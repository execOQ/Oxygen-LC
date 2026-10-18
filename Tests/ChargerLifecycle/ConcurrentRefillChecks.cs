using System;
using System.Collections.Generic;
using System.Reflection;
using LethalLib.Modules;
using Oxygen;
using Oxygen.Configuration;
using Oxygen.General;
using Oxygen.Items;
using Unity.Netcode;
using UnityEngine;

// Each peer has its own charger, oxygen meter, config, session service and message handlers.
// Only packets cross peers; clients deliberately retain stale replicated reserve values.
static class ConcurrentRefillChecks
{
    private static readonly FieldInfo instanceField = typeof(OxyCharger).GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo reserveField = typeof(OxyCharger).GetField("_remainedOxygenAmount", BindingFlags.NonPublic | BindingFlags.Instance);
    private static int checks;
    private static readonly Dictionary<ulong, Peer> peers = new();
    private const int ChargerId = 45;

    private sealed class Peer
    {
        internal NetworkManager Network;
        internal readonly OxygenBase Plugin = new();
        internal readonly Config Config = new();
        internal readonly GameNetworkManager Game = new();
        internal readonly StartOfRound Round = new();
        internal GameObject Charger;
        internal float Oxygen = 0.5f;
        internal Peer(ulong id, float reserve)
        {
            Network = new(id) { IsServer = id == 0 };
            Game.localPlayerController.actualClientId = id;
            Round.ClientPlayerList[id] = (int)id + 2;
            Round.allPlayerScripts[(int)id + 2] = Game.localPlayerController;
            Config.eclipsed_LimitedOxygenAmount.Value = reserve;
            Round.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
            for (int i = 0; i < ChargerId; i++) Round.unlockablesList.unlockables.Add(new());
            Round.unlockablesList.unlockables.Add(Unlockables.Registered);
            Select();
            ChargerUnlockable.Prepare(Round);
            Charger = UnityEngine.Object.Instantiate(Unlockables.Registered.prefabObject, Vector3.zero, Quaternion.identity);
            Charger.GetComponent<NetworkObject>().IsSpawned = true;
            Charger.GetComponent<NetworkObject>().NetworkObjectId = 500;
            ((NetworkVariable<float>)reserveField.GetValue(Component)).Value = reserve;
            Round.SpawnedShipUnlockables.Add(ChargerId, Charger);
            Plugin.Refills.StartSession(Network);
            peers[id] = this;
        }
        internal OxyCharger Component => Charger?.GetComponent<OxyCharger>();
        private void Select()
        {
            NetworkManager.Singleton = Network;
            OxygenBase.Instance = Plugin;
            OxygenBase.OxygenConfig = Config;
            GameNetworkManager.Instance = Game;
            StartOfRound.Instance = Round;
            OxygenInit.Percent = Oxygen;
            instanceField.SetValue(null, Charger != null && Charger.GetComponent<NetworkObject>().IsSpawned ? Component : null);
            OxygenInit.oxyCharger = Charger;
        }
        internal void Run(Action action)
        {
            Select();
            try { action(); }
            finally { Oxygen = OxygenInit.Percent; }
        }
    }

    private static Peer Setup(float reserve = 2f, int clientCount = 2)
    {
        foreach (var peer in peers.Values) peer.Plugin.Refills.EndSession();
        peers.Clear(); CustomMessagingManager.Pending.Clear();
        ChargerUnlockable.Reset(); WeatherHandler.Reset();
        Time.realtimeSinceStartup = 0;
        var server = new Peer(0, reserve);
        for (ulong id = 1; id <= (ulong)clientCount; id++)
        {
            _ = new Peer(id, reserve);
            RegisterClient(server, id);
        }
        server.Run(() => { ChargerUnlockable.StartRound(server.Round); WeatherHandler.UpdateWeatherType(server.Round.currentLevel); });
        return server;
    }

    private static void RegisterClient(Peer host, ulong id)
    {
        host.Network.ConnectedClients[id] = new();
        // Keep server player state independent of each client's local copy. Slot != client ID.
        host.Round.ClientPlayerList[id] = (int)id + 2;
        host.Round.allPlayerScripts[(int)id + 2] = new() { actualClientId = id };
    }

    private static void Assert(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static void Near(float actual, float expected, string message) => Assert(Math.Abs(actual - expected) < 0.00001f, message);
    private static void Deliver(RefillPacket packet) => peers[packet.Target].Run(() => peers[packet.Target].Network.CustomMessagingManager.Deliver(packet));
    private static void Next() => Deliver(CustomMessagingManager.Pending.Dequeue());
    private static void Pump() { while (CustomMessagingManager.Pending.Count > 0) Next(); }

    private static Peer ManualOnlySetup()
    {
        var host = Setup();
        foreach (var peer in peers.Values) peer.Config.autoFillingOnShip.Value = OxygenConfig.AutoFillingOnShip.Off;
        return host;
    }

    private static void RetryAndPlayerStateRegressions()
    {
        var host = ManualOnlySetup();
        peers[1].Network.CustomMessagingManager.FailSends = 1;
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        Assert(CustomMessagingManager.Pending.Count == 0, "Initial manual request send did not fail.");
        Time.realtimeSinceStartup = 0.9f;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick());
        Assert(CustomMessagingManager.Pending.Count == 0, "Manual timer retried before its interval.");
        Time.realtimeSinceStartup = 1.1f;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick()); // No refill input or automatic calls.
        Assert(CustomMessagingManager.Pending.Count == 1, "Manual-only request was not retried by the timer.");
        var attempts = peers[1].Network.CustomMessagingManager.Attempts;
        Assert(attempts[0].Bytes.AsSpan().SequenceEqual(attempts[1].Bytes), "Timer changed the pending request ID or payload.");
        Pump();
        Near(peers[1].Oxygen, 1, "Manual-only failed request did not recover without input.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Manual retry spent reserve twice."));
        Time.realtimeSinceStartup = 5;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick());
        Assert(CustomMessagingManager.Pending.Count == 0, "Timer kept retrying a completed manual refill.");

        host = ManualOnlySetup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        var original = CustomMessagingManager.Pending.Dequeue();
        host.Network.CustomMessagingManager.FailSends = 1;
        Deliver(original); // Host has allocated, but cannot send its first reply.
        Near(peers[1].Oxygen, 0.5f, "Failed reply applied oxygen before receipt.");
        Time.realtimeSinceStartup = 1.1f;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick());
        Assert(CustomMessagingManager.Pending.Count == 1, "Lost manual reply did not trigger an independent retry.");
        var retry = CustomMessagingManager.Pending.Dequeue();
        Assert(original.Bytes.AsSpan().SequenceEqual(retry.Bytes), "Reply recovery used a new allocation request.");
        Deliver(retry); Deliver(retry); Pump();
        Near(peers[1].Oxygen, 1, "Manual-only lost reply was not recovered exactly once.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Lost reply recovery allocated oxygen twice."));

        host = ManualOnlySetup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        host.Network.CustomMessagingManager.FailSends = 1; Next();
        host.Run(() => host.Round.Store(ChargerId));
        peers[1].Run(() => peers[1].Charger.GetComponent<NetworkObject>().Despawn());
        Time.realtimeSinceStartup = 1.1f;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick()); Pump();
        Near(peers[1].Oxygen, 1, "Storage stopped recovery of an already allocated manual grant.");
        host.Run(() =>
        {
            host.Round.ReturnUnlockableFromStorageServerRpc(ChargerId);
            host.Charger = host.Round.SpawnedShipUnlockables[ChargerId];
            Near(host.Component.RemainedOxygenAmount, 1.5f, "Stored grant recovery deducted again or restored spent oxygen.");
        });

        host = ManualOnlySetup();
        peers[1].Network.CustomMessagingManager.FailSends = 1;
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        peers[1].Run(() => peers[1].Network.Disconnect(1));
        Time.realtimeSinceStartup = 10;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick());
        Assert(CustomMessagingManager.Pending.Count == 0, "Disconnected session kept retrying a manual request.");
        Assert(peers[1].Network.CustomMessagingManager.Handlers.Count == 0, "Retry session retained disconnected handlers.");

        foreach (bool automatic in new[] { false, true })
        {
            host = Setup();
            peers[1].Run(() =>
            {
                if (automatic) peers[1].Component.AutoRefillOxygen();
                else peers[1].Component.FillOxygen(false);
            });
            var request = CustomMessagingManager.Pending.Dequeue();
            var serverPlayer = host.Round.allPlayerScripts[host.Round.ClientPlayerList[1]];
            serverPlayer.isPlayerDead = true; // Client may not have received the death state yet.
            Deliver(request); Pump();
            Near(peers[1].Oxygen, 0.5f, "Host granted a queued refill to an already-dead player.");
            host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Death before processing consumed shared reserve."));
            Time.realtimeSinceStartup = 2;
            peers[1].Run(() => peers[1].Plugin.Refills.Tick());
            Assert(CustomMessagingManager.Pending.Count == 0, "Zero grant failed to acknowledge a rejected dead-player request.");
            serverPlayer.isPlayerDead = false;
            Deliver(request); Pump(); // An old rejected request stays rejected after revival.
            host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Retry allocated a previously rejected request after revival."));
            peers[1].Run(() => peers[1].Component.FillOxygen(false)); Pump();
            Near(peers[1].Oxygen, 1, "Fresh refill after revival was blocked by the old rejection.");
            host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Fresh refill after revival was not deducted exactly once."));
        }

        host = Setup();
        host.Game.localPlayerController.isPlayerDead = true;
        host.Run(() => host.Component.FillOxygen(false));
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Dead host bypassed the player-state validation."));

        foreach (int invalid in new[] { 0, 1, 2, 3, 4 })
        {
            host = Setup();
            peers[1].Run(() => peers[1].Component.FillOxygen(false));
            int slot = host.Round.ClientPlayerList[1];
            if (invalid == 0) host.Round.ClientPlayerList.Remove(1);
            else if (invalid == 1) host.Round.ClientPlayerList[1] = -1;
            else if (invalid == 2) host.Round.ClientPlayerList[1] = host.Round.allPlayerScripts.Length;
            else if (invalid == 3) host.Round.allPlayerScripts[slot] = null;
            else host.Round.allPlayerScripts[slot].actualClientId = 99;
            Pump();
            Near(peers[1].Oxygen, 0.5f, "Missing/stale scene player mapping received oxygen.");
            host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Missing/stale player mapping consumed shared reserve."));
            RegisterClient(host, 1);
            peers[1].Run(() => peers[1].Component.FillOxygen(false)); Pump();
            Near(peers[1].Oxygen, 1, "Valid mapped scene player could not refill after a rejected request.");
        }
    }

    internal static int Run()
    {
        var host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        peers[2].Run(() => peers[2].Component.FillOxygen(false));
        Assert(peers[1].Oxygen == 0.5f && peers[2].Oxygen == 0.5f, "Clients granted oxygen before host approval.");
        Assert(CustomMessagingManager.Pending.Count == 2, "Simultaneous clients did not send independent requests.");
        Next(); Next(); // Both requests reach the host before either reply reaches a client.
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1f, "Concurrent requests lost a reserve deduction."));
        Pump();
        Near(peers[1].Oxygen + peers[2].Oxygen - 1, 1, "Clients did not receive exactly the host's two grants.");
        peers[1].Run(() => Near(peers[1].Component.RemainedOxygenAmount, 2, "Test must retain a stale client reserve replica."));
        peers[1].Run(() =>
        {
            peers[1].Component.RemainedOxygenAmount = 100;
            Near(peers[1].Component.RemainedOxygenAmount, 2, "Client still controls the reserve setter.");
        });
        Assert(CustomMessagingManager.Pending.Count == 0, "Client reserve assignment sent an absolute-balance RPC.");

        host = Setup(0.6f);
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        peers[2].Run(() => peers[2].Component.FillOxygen(false));
        host.Run(() => host.Component.FillOxygen(false)); // Host competes through the same allocator.
        Pump();
        Near(host.Oxygen + peers[1].Oxygen + peers[2].Oxygen - 1.5f, 0.6f, "Host/client grants exceeded a nearly empty reserve.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 0, "Nearly empty reserve became negative."));

        host = Setup(0.1f, 4);
        host.Config.autoFillingOnShip_increasingOxygen.Value = 0.05f;
        foreach (var peer in peers.Values)
            if (peer.Network.LocalClientId != 0) peer.Run(() => peer.Component.AutoRefillOxygen());
        Pump();
        float sum = 0;
        foreach (var peer in peers.Values) if (peer.Network.LocalClientId != 0) sum += peer.Oxygen - 0.5f;
        Near(sum, 0.1f, "Four simultaneous automatic refills overspent the last reserve.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 0, "Automatic refills produced a negative reserve."));

        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        var request = CustomMessagingManager.Pending.Dequeue();
        Deliver(request); Deliver(request); // Retry while its first reply is still in flight.
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Retry allocated twice."));
        Pump();
        Near(peers[1].Oxygen, 1, "Duplicate replies applied a grant twice.");

        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        host.Network.CustomMessagingManager.FailSends = 1;
        Next(); // Deduct once, but lose the first attempt to send its reply.
        Assert(CustomMessagingManager.Pending.Count == 0, "Send-failure simulation did not drop the reply.");
        Time.realtimeSinceStartup = 1.1f;
        peers[1].Run(() => peers[1].Plugin.Refills.Tick());
        Pump();
        Near(peers[1].Oxygen, 1, "Retry did not recover a failed grant send.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Retry after reply failure deducted twice."));

        host = Setup();
        host.Config.autoFillingOnShip_increasingOxygen.Value = 0.2f;
        peers[1].Config.autoFillingOnShip_increasingOxygen.Value = 0.8f;
        peers[1].Run(() => peers[1].Component.AutoRefillOxygen());
        Assert(CustomMessagingManager.Pending.Count == 1, "Waiting client flooded automatic refill requests.");
        Pump();
        Near(peers[1].Oxygen, 0.7f, "Automatic grant ignored the host's increment.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.8f, "Host automatic deduction disagreed with grant."));
        peers[1].Run(() => peers[1].Component.AutoRefillOxygen());
        host.Config.autoFillingOnShip.Value = OxygenConfig.AutoFillingOnShip.Off;
        Pump();
        Near(peers[1].Oxygen, 0.7f, "Disabled automatic refill still received a grant.");
        host.Config.autoFillingOnShip.Value = OxygenConfig.AutoFillingOnShip.WhenDoorsClosed;
        host.Round.hangarDoorsClosed = false;
        peers[1].Run(() => peers[1].Component.AutoRefillOxygen()); Pump();
        Near(peers[1].Oxygen, 0.7f, "Open doors bypassed host automatic refill rules.");

        host = Setup();
        host.Config.autoFillingOnShip_increasingOxygen.Value = 0.01f;
        peers[1].Run(() => { for (int i = 0; i < 20; i++) peers[1].Component.AutoRefillOxygen(); });
        Assert(CustomMessagingManager.Pending.Count == 1, "Delayed automatic refills sent per-frame packets.");
        Pump();
        Near(peers[1].Oxygen, 0.7f, "Latency lost automatic refill frames instead of batching them.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.8f, "Batched refill deduction disagreed with granted oxygen."));

        host = Setup(0.15f);
        host.Config.autoFillingOnShip_increasingOxygen.Value = 0.01f;
        foreach (ulong id in new ulong[] { 1, 2 })
            peers[id].Run(() => { for (int i = 0; i < 20; i++) peers[id].Component.AutoRefillOxygen(); });
        Pump();
        Near(peers[1].Oxygen + peers[2].Oxygen - 1, 0.15f, "Concurrent batched refills exceeded the remaining reserve.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 0, "Concurrent batches made the reserve negative."));

        host = Setup(0.25f);
        peers[1].Config.eclipsed_LimitOxygen.Value = false;
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        Near(peers[1].Oxygen, 0.5f, "Stale client weather config bypassed host approval.");
        Pump();
        Near(peers[1].Oxygen, 0.75f, "Host weather limit was not authoritative.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 0, "Stale client config bypassed reserve deduction."));
        host.Config.eclipsed_LimitOxygen.Value = false;
        peers[1].Config.eclipsed_LimitOxygen.Value = true;
        peers[1].Run(() => peers[1].Component.FillOxygen(false)); Pump();
        Near(peers[1].Oxygen, 1, "Unlimited host could not grant oxygen to a stale limited client.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 0, "Unlimited grant changed the inactive reserve."));

        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        host.Config.oxyCharger_Enabled.Value = false;
        Pump();
        Near(peers[1].Oxygen, 0.5f, "Host disabled the charger but a queued request still granted oxygen.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Rejected disabled request consumed reserve."));

        host = Setup();
        peers[1].Run(() => { peers[1].Component.AutoRefillOxygen(); peers[1].Component.FillOxygen(false); });
        Pump();
        Near(peers[1].Oxygen, 1, "Manual interaction during a pending automatic refill was lost.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.5f, "Queued manual refill overspent its missing oxygen."));

        // Deduction accepted before storage; reply must survive despawning both replicas.
        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false)); Next();
        host.Run(() => host.Round.Store(ChargerId));
        peers[1].Run(() => peers[1].Charger.GetComponent<NetworkObject>().Despawn());
        Pump();
        Near(peers[1].Oxygen, 1, "Storage destroyed an already allocated grant.");
        host.Run(() =>
        {
            host.Round.ReturnUnlockableFromStorageServerRpc(ChargerId);
            host.Charger = host.Round.SpawnedShipUnlockables[ChargerId];
            Near(host.Component.RemainedOxygenAmount, 1.5f, "Storage restored oxygen already granted to a client.");
        });

        // Storage won the race: request for the old object must not debit its replacement.
        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        host.Run(() =>
        {
            host.Round.Store(ChargerId);
            host.Round.ReturnUnlockableFromStorageServerRpc(ChargerId);
            host.Charger = host.Round.SpawnedShipUnlockables[ChargerId];
        });
        Pump();
        Near(peers[1].Oxygen, 0.5f, "Stale charger request was granted against a replacement.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Stale request consumed the carried reserve."));

        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        host.Network.Disconnect(1);
        peers[1].Run(() => peers[1].Network.Disconnect(1));
        Pump();
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Disconnected sender still consumed reserve."));
        Assert(peers[1].Network.CustomMessagingManager.Handlers.Count == 0, "Disconnected client retained refill handlers.");

        // Reconnect with the same service: an old host's grant cannot match a new request ID.
        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false)); Next();
        var oldGrant = CustomMessagingManager.Pending.Dequeue();
        var oldHost = host;
        oldHost.Plugin.Refills.EndSession();
        host = new Peer(0, 2);
        RegisterClient(host, 1);
        host.Run(() => WeatherHandler.UpdateWeatherType(host.Round.currentLevel));
        peers[1].Run(() =>
        {
            peers[1].Plugin.Refills.EndSession();
            peers[1].Network = new(1);
            NetworkManager.Singleton = peers[1].Network;
            peers[1].Plugin.Refills.StartSession(peers[1].Network);
            OxygenInit.Percent = 0.25f;
            peers[1].Component.FillOxygen(false);
        });
        Deliver(oldGrant);
        Near(peers[1].Oxygen, 0.25f, "Previous session's grant matched a new pending refill.");
        Pump();
        Near(peers[1].Oxygen, 1, "Reconnected client could not refill with a fresh request.");
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 1.25f, "New host did not allocate from its own reserve."));

        host = Setup();
        peers[1].Run(() => peers[1].Component.FillOxygen(false));
        var forged = CustomMessagingManager.Pending.Dequeue();
        // A grant must come from the host and match an outstanding request.
        using (var writer = new FastBufferWriter(16, Unity.Collections.Allocator.Temp))
        {
            writer.WriteValueSafe(1UL); writer.WriteValueSafe(0.5f);
            Deliver(new(2, 1, OxygenBase.modGUID + ".Refill.Grant.v1", writer.Bytes));
        }
        Near(peers[1].Oxygen, 0.5f, "Another client forged a refill grant.");
        Deliver(forged);
        peers[1].Game.localPlayerController.isPlayerDead = true;
        Pump();
        Near(peers[1].Oxygen, 0.5f, "In-flight grant changed a dead player's oxygen.");

        host = Setup();
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
        {
            peers[1].Run(() => peers[1].Plugin.Refills.Request(invalid, false, false));
            Assert(CustomMessagingManager.Pending.Count == 0, "Invalid local request was sent.");
        }
        ulong requestId = 1;
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f, 0f })
        {
            using var writer = new FastBufferWriter(32, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(requestId++); writer.WriteValueSafe(500UL); writer.WriteValueSafe(invalid);
            writer.WriteValueSafe(false); writer.WriteValueSafe(false);
            writer.WriteValueSafe(1);
            Deliver(new(1, 0, OxygenBase.modGUID + ".Refill.Request.v1", writer.Bytes));
        }
        Pump();
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Malformed client amounts changed the reserve."));
        foreach (int ticks in new[] { 0, -1, RefillSync.MaxAutomaticTicks + 1 })
        {
            using var writer = new FastBufferWriter(32, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(requestId++); writer.WriteValueSafe(500UL); writer.WriteValueSafe(0.5f);
            writer.WriteValueSafe(true); writer.WriteValueSafe(false); writer.WriteValueSafe(ticks);
            Deliver(new(1, 0, OxygenBase.modGUID + ".Refill.Request.v1", writer.Bytes));
        }
        Pump();
        host.Run(() => Near(host.Component.RemainedOxygenAmount, 2, "Invalid automatic batch size changed the reserve."));

        RetryAndPlayerStateRegressions();
        foreach (var peer in peers.Values) peer.Plugin.Refills.EndSession();
        peers.Clear(); CustomMessagingManager.Pending.Clear();
        return checks;
    }
}
