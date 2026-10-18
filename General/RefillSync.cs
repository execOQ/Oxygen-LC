using System;
using System.Collections.Generic;
using Oxygen.Items;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.General
{
    // Grants use session messages so a storage despawn cannot destroy an in-flight reply.
    internal sealed class RefillSync
    {
        internal const int MaxAutomaticTicks = 256;
        private const string RequestMessage = OxygenBase.modGUID + ".Refill.Request.v1";
        private const string GrantMessage = OxygenBase.modGUID + ".Refill.Grant.v1";
        private NetworkManager manager;
        private ulong nextRequestId, pendingId, pendingCharger;
        private float pendingAmount, lastSent;
        private int pendingTicks, queuedTicks;
        private bool pending, pendingAutomatic, pendingSound, queuedManual, queuedSound;
        private readonly Dictionary<ulong, (ulong Id, float Amount)> replies = new();

        internal void StartSession(NetworkManager network)
        {
            EndSession();
            manager = network;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(RequestMessage, OnRequest);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(GrantMessage, OnGrant);
            manager.OnClientDisconnectCallback += OnDisconnect;
        }

        internal void EndSession()
        {
            if (manager != null)
            {
                manager.OnClientDisconnectCallback -= OnDisconnect;
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(RequestMessage);
                manager.CustomMessagingManager?.UnregisterNamedMessageHandler(GrantMessage);
            }
            manager = null;
            pending = queuedManual = queuedSound = false;
            queuedTicks = 0;
            replies.Clear();
            // Keep request IDs increasing across hosts to reject a previous session's reply.
        }

        private void OnDisconnect(ulong client)
        {
            if (manager == null) return;
            if (client == manager.LocalClientId) EndSession();
            else replies.Remove(client);
        }

        internal void Request(float amount, bool automatic, bool sound, int ticks = 1)
        {
            if (manager == null || !manager.IsListening) return;
            if (pending)
            {
                if (!automatic) { queuedManual = true; queuedSound |= sound; }
                else queuedTicks = Math.Min(MaxAutomaticTicks, queuedTicks + ticks);
                return;
            }
            var charger = OxyCharger.Instance;
            if (charger == null || !charger.IsSpawned || !Finite(amount) || amount <= 0) return;
            pending = true;
            pendingId = ++nextRequestId;
            pendingCharger = charger.NetworkObjectId;
            pendingAmount = Math.Min(1f, amount);
            pendingAutomatic = automatic;
            pendingSound = sound;
            pendingTicks = Math.Max(1, Math.Min(MaxAutomaticTicks, ticks));
            SendRequest();
        }

        // Driven by the plugin's Update, even when auto refill is off or the charger is stored.
        internal void Tick()
        {
            if (manager != null && manager.IsListening && pending &&
                Time.realtimeSinceStartup - lastSent >= 1f) SendRequest();
        }

        private void SendRequest()
        {
            lastSent = Time.realtimeSinceStartup;
            try
            {
                if (manager.IsServer)
                {
                    Allocate(manager.LocalClientId, pendingId, pendingCharger, pendingAmount, pendingAutomatic, pendingSound, pendingTicks);
                    return;
                }
                using var writer = new FastBufferWriter(32, Allocator.Temp);
                writer.WriteValueSafe(pendingId);
                writer.WriteValueSafe(pendingCharger);
                writer.WriteValueSafe(pendingAmount);
                writer.WriteValueSafe(pendingAutomatic);
                writer.WriteValueSafe(pendingSound);
                writer.WriteValueSafe(pendingTicks);
                manager.CustomMessagingManager.SendNamedMessage(RequestMessage, NetworkManager.ServerClientId,
                    writer, NetworkDelivery.ReliableSequenced);
            }
            catch (Exception ex) { Log(ex); } // A retry reuses the ID, never a second allocation.
        }

        private void OnRequest(ulong sender, FastBufferReader reader)
        {
            if (manager == null || !manager.IsServer) return;
            try
            {
                reader.ReadValueSafe(out ulong id);
                reader.ReadValueSafe(out ulong charger);
                reader.ReadValueSafe(out float amount);
                reader.ReadValueSafe(out bool automatic);
                reader.ReadValueSafe(out bool sound);
                reader.ReadValueSafe(out int ticks);
                if (reader.Position != reader.Length) return;
                Allocate(sender, id, charger, amount, automatic, sound, ticks);
            }
            catch (Exception ex) { Log(ex); }
        }

        private void Allocate(ulong sender, ulong id, ulong chargerId, float amount, bool automatic, bool sound, int ticks)
        {
            if (!manager.IsListening || !manager.ConnectedClients.ContainsKey(sender) || id == 0) return;
            if (replies.TryGetValue(sender, out var previous) && id <= previous.Id)
            {
                if (id == previous.Id) SendGrant(sender, previous.Id, previous.Amount);
                return;
            }
            var charger = OxyCharger.Instance;
            float granted = ticks >= 1 && ticks <= MaxAutomaticTicks &&
                SenderIsAlive(sender) && charger != null && charger.IsSpawned && charger.NetworkObjectId == chargerId
                ? charger.AllocateOxygen(amount, automatic, automatic ? ticks : 1) : 0f;
            replies[sender] = (id, granted);
            // Allocation and deduction complete on the host's main thread before replying.
            if (granted > 0 && sound) charger.PlayGrantedSFX();
            SendGrant(sender, id, granted);
        }

        private static bool SenderIsAlive(ulong sender)
        {
            // Lethal Company assigns pre-existing scene players; NGO PlayerObject can be null.
            var round = StartOfRound.Instance;
            if (round == null || round.ClientPlayerList == null || round.allPlayerScripts == null ||
                !round.ClientPlayerList.TryGetValue(sender, out int slot) ||
                slot < 0 || slot >= round.allPlayerScripts.Length) return false;
            var player = round.allPlayerScripts[slot];
            return player != null && player.actualClientId == sender && !player.isPlayerDead;
        }

        private void SendGrant(ulong client, ulong id, float amount)
        {
            if (client == manager.LocalClientId) { ApplyGrant(id, amount); return; }
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(id);
            writer.WriteValueSafe(amount);
            manager.CustomMessagingManager.SendNamedMessage(GrantMessage, client, writer, NetworkDelivery.ReliableSequenced);
        }

        private void OnGrant(ulong sender, FastBufferReader reader)
        {
            if (manager == null || manager.IsServer || sender != NetworkManager.ServerClientId) return;
            try
            {
                reader.ReadValueSafe(out ulong id);
                reader.ReadValueSafe(out float amount);
                if (reader.Position == reader.Length) ApplyGrant(id, amount);
            }
            catch (Exception ex) { Log(ex); }
        }

        private void ApplyGrant(ulong id, float amount)
        {
            if (!pending || id != pendingId || !Finite(amount) || amount < 0 || amount > pendingAmount) return;
            pending = false;
            bool manual = queuedManual, sound = queuedSound;
            int ticks = queuedTicks;
            queuedManual = queuedSound = false;
            queuedTicks = 0;
            var player = GameNetworkManager.Instance?.localPlayerController;
            if (player == null || player.isPlayerDead) return;
            OxygenInit.Percent = Math.Min(1f, OxygenInit.Percent + amount);
            if (manual) Request(1f - OxygenInit.Percent, false, sound);
            // Accumulate auto-refill frames while waiting, preserving refill speed under latency
            // without applying unconfirmed oxygen or sending one packet every frame.
            else if (ticks > 0) Request(1f - OxygenInit.Percent, true, false, ticks);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void Log(Exception ex) => Configuration.OxygenConfig.mls.LogError($"Oxygen refill message failed: {ex}");
    }
}
