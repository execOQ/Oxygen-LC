using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.Configuration
{
    internal sealed class ConfigSync
    {
        private const string RequestMessage = OxygenBase.modGUID + ".Config.Request.v1";
        private const string SnapshotMessage = OxygenBase.modGUID + ".Config.Snapshot.v1";
        private readonly OxygenConfig config;
        private readonly List<IHostConfigEntry> entries;
        private readonly ConfigSnapshot snapshot = new ConfigSnapshot();
        private NetworkManager manager;
        private bool receivedHost;
        private bool syncFailed;
        private Coroutine requestRoutine;
        private Dictionary<string, string> hostSettings;
        private float lastProgressTime;
        private bool receivingSnapshot;
        private int rejectedSnapshots;
        private bool restoringLocal;
        internal General.RefillSync Refills { get; } = new General.RefillSync();

        internal ConfigSync(OxygenConfig config, List<IHostConfigEntry> entries)
        {
            this.config = config;
            this.entries = entries;
        }

        internal void StartSession(NetworkManager network)
        {
            if (manager == network) return;
            EndSession();
            manager = network;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(RequestMessage, OnRequest);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage, OnSnapshot);
            Refills.StartSession(manager);
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnect;
            manager.OnClientStopped += OnStopped;
            manager.OnServerStopped += OnStopped;
            foreach (var entry in entries) entry.UseDefault();
            bool refreshed = false;
            if (manager.IsServer)
            {
                hostSettings = entries.ToDictionary(entry => entry.Key, entry => entry.SerializeEffective());
                refreshed = CommitHostSettings(false);
            }
            else
            {
                if (manager.IsConnectedClient) OnConnected(manager.LocalClientId);
            }
            if (!refreshed)
            {
                try { config.RefreshEffects(); }
                catch (Exception ex) { OxygenConfig.mls.LogError($"Could not refresh session config: {ex}"); }
            }
        }

        private void EndSession()
        {
            Refills.EndSession();
            if (requestRoutine != null && OxygenBase.Instance != null)
                OxygenBase.Instance.StopCoroutine(requestRoutine);
            requestRoutine = null;
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnConnected;
                manager.OnClientDisconnectCallback -= OnDisconnect;
                manager.OnClientStopped -= OnStopped;
                manager.OnServerStopped -= OnStopped;
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(RequestMessage);
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(SnapshotMessage);
            }
            manager = null;
            receivedHost = false;
            syncFailed = false;
            hostSettings = null;
            receivingSnapshot = false;
            rejectedSnapshots = 0;
            lastProgressTime = 0;
            snapshot.Reset();
            foreach (var entry in entries) entry.ClearOverride();
            // Never parse local settings while tearing down or preparing a different host.
            try { config.ResetSessionState(); }
            catch (Exception ex) { OxygenConfig.mls.LogError($"Could not reset config presentation: {ex}"); }
        }

        private void OnStopped(bool wasHost) => EndSession();

        private void OnDisconnect(ulong clientId)
        {
            if (manager != null && clientId == manager.LocalClientId) EndSession();
        }

        private void OnConnected(ulong clientId)
        {
            if (manager.IsServer || syncFailed || clientId != manager.LocalClientId || requestRoutine != null) return;
            lastProgressTime = Time.realtimeSinceStartup;
            requestRoutine = OxygenBase.Instance.StartCoroutine(RequestUntilSynced(manager));
        }

        private IEnumerator RequestUntilSynced(NetworkManager session)
        {
            while (manager == session && !receivedHost && rejectedSnapshots < 5 &&
                Time.realtimeSinceStartup - lastProgressTime < 10f)
            {
                // Reliable chunks already in flight need time to finish, not another full snapshot.
                if (!receivingSnapshot)
                {
                    try
                    {
                        using var writer = new FastBufferWriter(1, Allocator.Temp);
                        session.CustomMessagingManager.SendNamedMessage(RequestMessage, NetworkManager.ServerClientId,
                            writer, NetworkDelivery.ReliableSequenced);
                    }
                    catch (Exception ex) { OxygenConfig.mls.LogError($"Could not request host config: {ex}"); }
                }
                yield return new WaitForSecondsRealtime(2f);
            }
            requestRoutine = null;
            if (manager == session && !receivedHost) FailInitialSync(session);
        }

        private void FailInitialSync(NetworkManager session)
        {
            syncFailed = true;
            const string reason = "Oxygen could not apply the host's settings. Check the host config and use the same Oxygen build on all peers.";
            OxygenConfig.mls.LogError(reason);
            // Use the game's disconnect flow so lobby membership and the main menu also recover.
            try
            {
                if (GameNetworkManager.Instance != null && StartOfRound.Instance != null)
                {
                    GameNetworkManager.Instance.disconnectionReasonMessage = reason;
                    GameNetworkManager.Instance.Disconnect();
                }
                else session.Shutdown();
            }
            catch (Exception ex)
            {
                OxygenConfig.mls.LogError($"Could not leave unsynchronized lobby: {ex}");
                session.Shutdown();
            }
        }

        private void OnRequest(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer && sender != NetworkManager.ServerClientId &&
                manager.ConnectedClients.ContainsKey(sender)) SendSnapshot(sender);
        }

        internal void OnLocalSettingChanged()
        {
            if (restoringLocal) return;
            // Client edits are saved locally, but cannot change the active host overrides.
            if (manager != null && !manager.IsServer) return;
            if (manager == null)
            {
                try { config.RefreshEffects(); }
                catch (Exception ex) { OxygenConfig.mls.LogError($"Could not refresh local config: {ex}"); }
                return;
            }
            if (!CommitHostSettings() || !manager.IsListening) return;
            foreach (ulong clientId in manager.ConnectedClientsIds)
                if (clientId != NetworkManager.ServerClientId) SendSnapshot(clientId);
        }

        private bool CommitHostSettings(bool restoreLocalOnFailure = true)
        {
            try
            {
                var candidate = entries.ToDictionary(entry => entry.Key, entry => entry.SerializeLocal());
                // Enforce wire-size limits before changing gameplay, as well as types and moon lists.
                ConfigSnapshot.Encode(candidate);
                ApplySnapshot(entries, candidate, config.ValidateHostSettings, () => config.RefreshEffects());
                hostSettings = candidate;
                return true;
            }
            catch (Exception ex)
            {
                OxygenConfig.mls.LogError($"Rejected host config edit; keeping the last working settings: {ex}");
                if (restoreLocalOnFailure)
                {
                    restoringLocal = true;
                    try { foreach (var entry in entries) entry.RestoreLocal(hostSettings[entry.Key]); }
                    catch (Exception restoreError) { OxygenConfig.mls.LogError($"Could not restore the host config file: {restoreError}"); }
                    finally { restoringLocal = false; }
                }
                return false;
            }
        }

        private void SendSnapshot(ulong clientId)
        {
            try
            {
                byte[] data = ConfigSnapshot.Encode(hostSettings);
                for (int offset = 0; offset < data.Length; offset += ConfigSnapshot.ChunkSize)
                {
                    int length = Math.Min(ConfigSnapshot.ChunkSize, data.Length - offset);
                    using var writer = new FastBufferWriter(length + 8, Allocator.Temp);
                    writer.WriteValueSafe(data.Length);
                    writer.WriteValueSafe(offset);
                    writer.WriteBytesSafe(data, length, offset);
                    manager.CustomMessagingManager.SendNamedMessage(SnapshotMessage, clientId, writer,
                        NetworkDelivery.ReliableFragmentedSequenced);
                }
            }
            catch (Exception ex) { OxygenConfig.mls.LogError($"Could not send host config: {ex}"); }
        }

        private void OnSnapshot(ulong sender, FastBufferReader reader)
        {
            if (manager.IsServer || syncFailed || sender != NetworkManager.ServerClientId) return;
            try
            {
                reader.ReadValueSafe(out int totalBytes);
                reader.ReadValueSafe(out int offset);
                int length = reader.Length - reader.Position;
                if (length <= 0 || length > ConfigSnapshot.ChunkSize)
                    throw new InvalidDataException("Invalid host config message size.");
                var chunk = new byte[length];
                reader.ReadBytesSafe(ref chunk, length);
                var values = snapshot.Receive(totalBytes, offset, chunk);
                if (values == null)
                {
                    receivingSnapshot = true;
                    lastProgressTime = Time.realtimeSinceStartup;
                    return;
                }
                ApplySnapshot(entries, values, config.ValidateHostSettings, () => config.RefreshEffects());
                receivingSnapshot = false;
                receivedHost = true;
                OxygenConfig.mls.LogInfo("Received complete host config.");
            }
            catch (Exception ex)
            {
                snapshot.Reset();
                receivingSnapshot = false;
                if (!receivedHost) rejectedSnapshots++;
                OxygenConfig.mls.LogError($"Could not receive host config: {ex}");
            }
        }

        internal static void ApplySnapshot(List<IHostConfigEntry> entries, Dictionary<string, string> values,
            Action<IReadOnlyDictionary<IHostConfigEntry, object>> validate = null, Action refresh = null)
        {
            // Reject schema/type mismatches before changing any active value.
            if (values.Count != entries.Count || entries.Any(entry => !values.ContainsKey(entry.Key)))
                throw new InvalidDataException("Incompatible Oxygen host settings. All peers need the same build.");
            var parsed = entries.ToDictionary(entry => entry, entry => entry.Parse(values[entry.Key]));
            validate?.Invoke(parsed);
            Action[] restore = entries.Select(entry => entry.CaptureOverride()).ToArray();
            try
            {
                foreach (var entry in entries) entry.SetOverride(parsed[entry]);
                refresh?.Invoke();
            }
            catch
            {
                foreach (var rollback in restore) rollback();
                // Moon dictionaries are rebuilt atomically; restore them before retrying world effects.
                try { refresh?.Invoke(); }
                catch (Exception ex) { OxygenConfig.mls.LogError($"Could not restore config presentation: {ex}"); }
                throw;
            }
        }
    }
}
