using BepInEx.Configuration;
using Oxygen.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Netcode;

static class Program
{
    private enum Mode { DoorsClosed, OnShip, Off }
    private static int checks;
    private static void Assert(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is FormatException)
        { checks++; return; }
        throw new Exception("Invalid snapshot was accepted.");
    }
    private sealed class Peer
    {
        internal readonly OxygenConfig Config = new();
        internal readonly HostConfigEntry<string> Moons;
        internal readonly HostConfigEntry<int> Price;
        internal readonly HostConfigEntry<bool> Enabled;
        internal readonly HostConfigEntry<float> Drain;
        internal readonly HostConfigEntry<Mode> FillMode;
        internal readonly ConfigEntry<float> Volume = new("Volume", 0.25f, 1f);
        internal readonly List<IHostConfigEntry> Entries;
        internal readonly ConfigSync Sync;
        internal Peer(string moons, int price, bool enabled, float drain, Mode mode)
        {
            Moons = new(new("Moons", moons, ""));
            Price = new(new("Price", price, 70));
            Enabled = new(new("Enabled", enabled, true));
            Drain = new(new("Drain", drain, 0.0075f));
            FillMode = new(new("FillMode", mode, Mode.DoorsClosed));
            Entries = new() { Moons, Price, Enabled, Drain, FillMode };
            Config.decreasingOxygenOutsideMoons = Moons;
            Config.decreasingOxygenOutside = Drain;
            Config.greenPlanets = new(new("greenPlanets", "", ""));
            Config.decreasingOxygenInFactoryMoons = new(new("insideMoons", "", ""));
            Config.runningMultiplierMoons = new(new("runningMoons", "", ""));
            Config.oxygenDepletionInWaterMoons = new(new("waterMoons", "", ""));
            Config.decreasingOxygenInFactory = new(new("insideBase", 0.007f, 0.007f));
            Config.runningMultiplier = new(new("runningBase", 1f, 1f));
            Config.oxygenDepletionInWater = new(new("waterBase", 0.018f, 0.018f));
            Entries.AddRange(new IHostConfigEntry[] { Config.greenPlanets, Config.decreasingOxygenInFactoryMoons,
                Config.runningMultiplierMoons, Config.oxygenDepletionInWaterMoons, Config.decreasingOxygenInFactory,
                Config.runningMultiplier, Config.oxygenDepletionInWater });
            Sync = new(Config, Entries);
            Moons.Entry.SettingChanged += (_, _) => Sync.OnLocalSettingChanged();
            Price.Entry.SettingChanged += (_, _) => Sync.OnLocalSettingChanged();
            Enabled.Entry.SettingChanged += (_, _) => Sync.OnLocalSettingChanged();
            Drain.Entry.SettingChanged += (_, _) => Sync.OnLocalSettingChanged();
            FillMode.Entry.SettingChanged += (_, _) => Sync.OnLocalSettingChanged();
        }
        internal Dictionary<string, string> Values() => Entries.ToDictionary(e => e.Key, e => e.SerializeLocal());
    }

    private static void RoundTrip(Dictionary<string, string> expected)
    {
        byte[] bytes = ConfigSnapshot.Encode(expected);
        var receiver = new ConfigSnapshot();
        Dictionary<string, string> actual = null;
        for (int offset = 0; offset < bytes.Length; offset += ConfigSnapshot.ChunkSize)
        {
            int length = Math.Min(ConfigSnapshot.ChunkSize, bytes.Length - offset);
            actual = receiver.Receive(bytes.Length, offset, bytes.Skip(offset).Take(length).ToArray());
            if (offset + length < bytes.Length) Assert(actual == null, "Partial snapshot applied.");
        }
        Assert(actual != null && actual.Count == expected.Count && expected.All(e => actual[e.Key] == e.Value), "Snapshot truncated or reordered.");
    }

    private static void CodecChecks(string longList)
    {
        RoundTrip(new() { ["Empty"] = "", ["Bool"] = "true", ["Int"] = "70", ["Float"] = "0.0125", ["Enum"] = "Off" });
        RoundTrip(new() { ["Moons"] = longList, ["Unicode"] = string.Concat(Enumerable.Repeat("Луна🌕@1.25;", 3000)) });
        for (int length = 8170; length <= 8195; length++) RoundTrip(new() { ["Boundary"] = new string('x', length) + "🌕" });
        var receiver = new ConfigSnapshot();
        Reject(() => receiver.Receive(ConfigSnapshot.MaxBytes + 1, 0, new byte[1]));
        Reject(() => receiver.Receive(8, -1, new byte[1]));
        Reject(() => receiver.Receive(8, 8, new byte[1]));
        Reject(() => receiver.Receive(8, 0, Array.Empty<byte>()));
        Reject(() => receiver.Receive(10000, 0, new byte[ConfigSnapshot.ChunkSize + 1]));
        Reject(() => receiver.Receive(8, 1, new byte[1]));
        Assert(receiver.Receive(8, 0, new byte[2]) == null, "Partial snapshot applied.");
        Reject(() => receiver.Receive(8, 4, new byte[2]));
        receiver.Reset();
        Reject(() => receiver.Receive(8, 2, new byte[2]));
        byte[] valid = ConfigSnapshot.Encode(new Dictionary<string, string> { ["Empty"] = "" });
        Assert(receiver.Receive(valid.Length, 0, valid)["Empty"] == "", "Interrupted snapshot was not replaced.");
        byte[] incompatible = (byte[])valid.Clone(); incompatible[0]++;
        Reject(() => receiver.Receive(incompatible.Length, 0, incompatible));
        byte[] invalidCount = (byte[])valid.Clone(); Array.Clear(invalidCount, 4, 4);
        Reject(() => receiver.Receive(invalidCount.Length, 0, invalidCount));
        byte[] extra = valid.Concat(new byte[] { 0 }).ToArray();
        Reject(() => receiver.Receive(extra.Length, 0, extra));
        Reject(() => receiver.Receive(1, 0, new byte[] { 0xff }));
        Reject(() => ConfigSnapshot.Encode(new Dictionary<string, string>()));
        Reject(() => ConfigSnapshot.Encode(Enumerable.Range(0, 257).ToDictionary(i => i.ToString(), i => "")));
        Reject(() => ConfigSnapshot.Encode(new Dictionary<string, string> { ["TooBig"] = new string('x', ConfigSnapshot.MaxBytes) }));
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(ConfigSnapshot.ProtocolVersion); writer.Write(2);
        writer.Write("Key"); writer.Write("first"); writer.Write("Key"); writer.Write("last");
        byte[] duplicate = stream.ToArray();
        Reject(() => receiver.Receive(duplicate.Length, 0, duplicate));
    }

    private static void OverrideChecks()
    {
        var peer = new Peer("LocalMoon@1", 42, false, 0.1f, Mode.OnShip);
        var values = peer.Values();
        values[peer.Price.Key] = "500";
        values[peer.FillMode.Key] = "99";
        Reject(() => ConfigSync.ApplySnapshot(peer.Entries, values));
        Assert(peer.Price.Value == 42, "Invalid enum caused partial override.");
        values[peer.FillMode.Key] = "Off"; values[peer.Drain.Key] = "NaN";
        Reject(() => ConfigSync.ApplySnapshot(peer.Entries, values));
        values[peer.Drain.Key] = "Infinity";
        Reject(() => ConfigSync.ApplySnapshot(peer.Entries, values));
        values[peer.Drain.Key] = "0.125";
        var missing = new Dictionary<string, string>(values); missing.Remove(peer.Enabled.Key);
        Reject(() => ConfigSync.ApplySnapshot(peer.Entries, missing));
        var unknown = new Dictionary<string, string>(values); unknown.Remove(peer.Enabled.Key); unknown["Unexpected"] = "true";
        Reject(() => ConfigSync.ApplySnapshot(peer.Entries, unknown));
        CultureInfo original = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU"); ConfigSync.ApplySnapshot(peer.Entries, values); }
        finally { CultureInfo.CurrentCulture = original; }
        Assert(peer.Drain.Value == 0.125f && peer.FillMode.Value == Mode.Off && peer.Price.Value == 500, "Typed conversion failed.");
        Assert(peer.Price.LocalValue == 42 && peer.FillMode.LocalValue == Mode.OnShip, "Host overwrote local config.");
        foreach (var entry in peer.Entries) entry.ClearOverride();
        Assert(peer.Price.Value == 42 && peer.FillMode.Value == Mode.OnShip, "Local values were not restored.");
    }

    private static void SessionChecks(string longList)
    {
        var host = new Peer(longList, 101, false, 0.012f, Mode.Off);
        var client = new Peer("ClientMoon@99", 999, true, 0.9f, Mode.OnShip);
        var server = new NetworkManager(0, true);
        var network = new NetworkManager(1, false) { IsConnectedClient = false };
        server.ConnectedClients.Add(1, new());
        host.Sync.StartSession(server); client.Sync.StartSession(network);
        Assert(host.Sync.Refills.Session == server && client.Sync.Refills.Session == network,
            "Network startup did not initialize refill sessions.");
        Assert(client.Price.Value == 70 && client.Price.LocalValue == 999, "Pending client used local gameplay values.");
        network.Connect();
        client.Sync.StartSession(network); // Repeated startup hooks must not reset/register twice.
        CustomMessagingManager.Deliver(CustomMessagingManager.Pending.Dequeue());
        var firstPacket = CustomMessagingManager.Pending.Peek();
        int refreshes = client.Config.RefreshCount;
        while (CustomMessagingManager.Pending.Count > 1)
        {
            CustomMessagingManager.Deliver(CustomMessagingManager.Pending.Dequeue());
            Assert(client.Price.Value == 70 && client.Config.RefreshCount == refreshes, "Partial config applied to gameplay.");
        }
        CustomMessagingManager.Pump();
        Assert(client.Moons.Value == longList && client.Price.Value == 101 && !client.Enabled.Value && client.FillMode.Value == Mode.Off, "Initial host sync failed.");
        Assert(client.Config.RefreshCount == refreshes + 1, "Effects did not refresh once after complete sync.");
        Assert(client.Price.LocalValue == 999 && client.Volume.Value == 0.25f, "Local values/preferences were modified.");
        refreshes = client.Config.RefreshCount;
        CustomMessagingManager.Deliver(firstPacket with { Sender = 2 });
        Assert(client.Config.RefreshCount == refreshes, "Client accepted a non-host snapshot.");
        CustomMessagingManager.Deliver(firstPacket with { Sender = 1, Target = 0 });
        Assert(host.Price.Value == 101, "Host accepted client config overrides.");
        host.Price.LocalValue = 202;
        CustomMessagingManager.Pump();
        Assert(client.Price.Value == 202, "Host edit did not sync.");
        client.Price.LocalValue = 777; client.Volume.Value = 0.8f;
        Assert(client.Price.Value == 202 && CustomMessagingManager.Pending.Count == 0 && client.Volume.Value == 0.8f, "Client edits affected host gameplay or local preferences were locked.");
        var late = new Peer("Late@1", 55, true, 1, Mode.OnShip);
        var lateNetwork = new NetworkManager(2, false); server.ConnectedClients.Add(2, new());
        late.Sync.StartSession(lateNetwork); CustomMessagingManager.Pump();
        Assert(late.Price.Value == 202 && late.Moons.Value == longList, "Late join did not receive current config.");
        network.DisconnectRemote(2);
        Assert(client.Price.Value == 202, "Another client disconnect cleared our overrides.");
        network.Stop();
        Assert(client.Sync.Refills.Session == null, "Disconnect retained the previous refill session.");
        Assert(client.Price.Value == 777 && network.CustomMessagingManager.Handlers.Count == 0, "Disconnect did not restore local settings/remove handlers.");
        host.Price.LocalValue = 303;
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network); CustomMessagingManager.Pump();
        Assert(client.Sync.Refills.Session == network, "Reconnect did not initialize a fresh refill session.");
        Assert(client.Price.Value == 303 && client.Price.LocalValue == 777, "Reconnect retained an old override.");
        network.Stop(); network.IsListening = true; network.IsServer = true;
        client.Sync.StartSession(network);
        Assert(client.Price.Value == 777, "Hosting after joining retained previous host config.");
        network.Stop(); lateNetwork.Stop(); server.Stop();
        CustomMessagingManager.Pending.Clear();
        // Missing host support times out with a clear diagnostic and clears its pending coroutine on shutdown.
        var isolated = new NetworkManager(3, false);
        client.Sync.StartSession(isolated);
        int errors = OxygenConfig.mls.Errors.Count;
        for (int i = 0; i < 5; i++) Oxygen.OxygenBase.Instance.Tick();
        Assert(OxygenConfig.mls.Errors.Count == errors + 1, "Missing host sync was silent.");
        Assert(isolated.ShutdownCount == 1 && !isolated.IsListening, "Unsynchronized client was left in the lobby.");
        isolated.Stop(); CustomMessagingManager.Pending.Clear();
        Assert(client.Price.Value == 777, "Failed sync leaked defaults after disconnect.");
    }

    private static void DeliverValues(NetworkManager client, Dictionary<string, string> values)
    {
        byte[] data = ConfigSnapshot.Encode(values);
        for (int offset = 0; offset < data.Length; offset += ConfigSnapshot.ChunkSize)
        {
            int length = Math.Min(ConfigSnapshot.ChunkSize, data.Length - offset);
            using var writer = new FastBufferWriter(length + 8, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(data.Length); writer.WriteValueSafe(offset);
            writer.WriteBytesSafe(data, length, offset);
            CustomMessagingManager.Deliver(new(0, client.LocalClientId,
                "OxygenTest.Config.Snapshot.v1", writer.Bytes));
        }
    }

    private static void SessionRegressionChecks(string longList)
    {
        CustomMessagingManager.Pending.Clear(); CustomMessagingManager.Peers.Clear();
        // A late join must use the host's lists even if the local file cannot be parsed.
        var hostA = new Peer(longList, 101, false, 0.012f, Mode.Off);
        var serverA = new NetworkManager(0, true); serverA.ConnectedClients.Add(1, new());
        hostA.Sync.StartSession(serverA);
        var client = new Peer("Duplicate@1;Duplicate@2", 777, true, 0.9f, Mode.OnShip);
        var network = new NetworkManager(1, false);
        client.Config.ResetFailuresRemaining = 1; // A presentation cleanup failure must not abort setup.
        client.Sync.StartSession(network);
        Assert(network.CustomMessagingManager.Handlers.Count == 2, "Local parsing/presentation failure aborted handler registration.");
        CustomMessagingManager.Pump();
        Assert(client.Moons.Value == longList && client.Config.OutsideMoons["lastmoon"] == 0.123f && client.Price.Value == 101,
            "Late join with an invalid local file did not apply all host values.");
        Assert(client.Moons.LocalValue == "Duplicate@1;Duplicate@2", "Sync changed the invalid local file.");

        // Leave an incomplete host-A update in the assembler, then reuse the client NetworkManager for host B.
        hostA.Moons.LocalValue = longList + ";AnotherMoon@0.456";
        CustomMessagingManager.Deliver(CustomMessagingManager.Pending.Dequeue());
        network.Stop(); serverA.Stop(); CustomMessagingManager.Pending.Clear();
        Assert(client.Config.OutsideMoons == null && MoonsDicts.decreasingOxygenOutsideMoons == null,
            "Disconnect retained host A's derived dictionaries.");
        Assert(client.Price.Value == 777 && network.CustomMessagingManager.Handlers.Count == 0,
            "Disconnect failed to restore local values/remove handlers.");
        var hostB = new Peer("DifferentMoon@0.456", 303, true, 0.02f, Mode.OnShip);
        var serverB = new NetworkManager(0, true); serverB.ConnectedClients.Add(1, new());
        hostB.Sync.StartSession(serverB);
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network);
        Assert(client.Price.Value == 70 && !client.Config.OutsideMoons.ContainsKey("lastmoon"),
            "Host A values leaked into the pending host B session.");
        CustomMessagingManager.Pump();
        Assert(client.Price.Value == 303 && client.Enabled.Value && client.FillMode.Value == Mode.OnShip &&
            client.Config.OutsideMoons["differentmoon"] == 0.456f && !client.Config.OutsideMoons.ContainsKey("lastmoon"),
            "Reconnecting to a different host kept old overrides or partial data.");
        Assert(client.Price.LocalValue == 777 && client.Moons.LocalValue == "Duplicate@1;Duplicate@2" && client.Volume.Value == 0.25f,
            "Switching hosts modified local config/preferences.");
        network.Stop(); serverB.Stop(); CustomMessagingManager.Pending.Clear();

        // Reject a complete but invalid initial snapshot before committing ANY scalar or derived list.
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network); CustomMessagingManager.Pending.Clear();
        var oldOutside = client.Config.OutsideMoons;
        var invalid = hostB.Values();
        invalid[hostB.Config.greenPlanets.Key] = "March@0";
        invalid[hostB.Config.decreasingOxygenInFactoryMoons.Key] = "Repeated@1;Repeated@2";
        DeliverValues(network, invalid);
        Assert(client.Price.Value == 70 && ReferenceEquals(client.Config.OutsideMoons, oldOutside),
            "Invalid late-join snapshot partially changed scalar/moon settings.");
        Oxygen.OxygenBase.Instance.Tick();
        Assert(CustomMessagingManager.Pending.Count == 1, "Rejected snapshot stopped initial-sync retries.");
        CustomMessagingManager.Pending.Clear();
        DeliverValues(network, hostB.Values());
        Oxygen.OxygenBase.Instance.Tick();
        Assert(client.Price.Value == 303 && CustomMessagingManager.Pending.Count == 0,
            "A corrected host snapshot did not recover initial sync.");

        // A rejected live update must retain the last working snapshot and its dictionary references.
        oldOutside = client.Config.OutsideMoons;
        invalid[hostB.Price.Key] = "999";
        DeliverValues(network, invalid);
        Assert(client.Price.Value == 303 && ReferenceEquals(client.Config.OutsideMoons, oldOutside),
            "Invalid host edit replaced the previous valid snapshot.");
        network.Stop(); CustomMessagingManager.Pending.Clear();

        // Failure while applying game-side effects must roll back and keep the initial request alive.
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network); CustomMessagingManager.Pending.Clear();
        client.Config.RefreshFailuresRemaining = 1;
        DeliverValues(network, hostB.Values());
        Assert(client.Price.Value == 70 && !client.Config.OutsideMoons.ContainsKey("differentmoon"),
            "World-effect failure retained partially committed overrides/dictionaries.");
        Oxygen.OxygenBase.Instance.Tick();
        Assert(CustomMessagingManager.Pending.Count == 1, "Failed world refresh was marked synced.");
        CustomMessagingManager.Pending.Clear(); DeliverValues(network, hostB.Values());
        Assert(client.Price.Value == 303 && client.Config.OutsideMoons.ContainsKey("differentmoon"),
            "Retry after world-effect failure did not recover.");
        network.Stop(); CustomMessagingManager.Pending.Clear();

        // Repeatedly invalid settings fail the connection through the game's full disconnect flow.
        network.IsListening = true; network.IsConnectedClient = true;
        GameNetworkManager.Instance = new() { Target = network };
        client.Sync.StartSession(network); CustomMessagingManager.Pending.Clear();
        DeliverValues(network, invalid);
        for (int attempt = 0; attempt < 5; attempt++) Oxygen.OxygenBase.Instance.Tick();
        Assert(GameNetworkManager.Instance.DisconnectCount == 1 && !network.IsListening &&
            GameNetworkManager.Instance.disconnectionReasonMessage.Contains("Oxygen"),
            "Failed initial sync did not leave the game with a visible reason.");
        Assert(client.Price.Value == 777 && network.CustomMessagingManager.Handlers.Count == 0 && client.Config.OutsideMoons == null,
            "Failed connection retained overrides, handlers, or derived host state.");
        GameNetworkManager.Instance = null; CustomMessagingManager.Pending.Clear();
        // The same client can still connect successfully after that failure.
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network); CustomMessagingManager.Pending.Clear(); DeliverValues(network, hostB.Values());
        Assert(client.Price.Value == 303, "Sync failure poisoned the next connection.");
        network.Stop(); CustomMessagingManager.Pending.Clear();

        // Transport exceptions must not kill the request coroutine before its failure path runs.
        network.IsListening = true; network.IsConnectedClient = true;
        network.CustomMessagingManager.SendFailuresRemaining = 5;
        client.Sync.StartSession(network);
        for (int attempt = 0; attempt < 5; attempt++) Oxygen.OxygenBase.Instance.Tick();
        Assert(network.ShutdownCount == 1 && !network.IsListening && client.Price.Value == 777,
            "Request-send exceptions left the client indefinitely unsynchronized.");
        CustomMessagingManager.Pending.Clear();
    }

    private static void HostEditRegressionChecks()
    {
        CustomMessagingManager.Pending.Clear(); CustomMessagingManager.Peers.Clear();
        var host = new Peer("Titan@0.01", 100, true, 0.01f, Mode.OnShip);
        var client = new Peer("Local@1", 999, false, 0.9f, Mode.Off);
        var server = new NetworkManager(0, true); server.ConnectedClients.Add(1, new());
        var network = new NetworkManager(1, false);
        host.Sync.StartSession(server); client.Sync.StartSession(network); CustomMessagingManager.Pump();
        host.Drain.LocalValue = float.NaN;
        Assert(host.Drain.Value == 0.01f && host.Drain.LocalValue == 0.01f && client.Drain.Value == 0.01f,
            "Rejected NaN host edit left players on different drain values.");
        host.Drain.LocalValue = float.PositiveInfinity;
        host.FillMode.LocalValue = (Mode)99;
        Assert(host.Drain.Value == client.Drain.Value && host.FillMode.Value == client.FillMode.Value &&
            host.FillMode.LocalValue == Mode.OnShip && CustomMessagingManager.Pending.Count == 0,
            "Non-finite/invalid enum host edit changed active settings or broadcast invalid data.");
        host.Moons.LocalValue = "Titan@1;Titan@2";
        Assert(host.Moons.Value == "Titan@0.01" && host.Moons.LocalValue == "Titan@0.01" &&
            host.Config.OutsideMoons["titan"] == 0.01f && CustomMessagingManager.Pending.Count == 0,
            "Rejected duplicate moon edit was not rolled back on the host.");
        host.Price.LocalValue = 500; CustomMessagingManager.Pump();
        Assert(host.Price.Value == 500 && client.Price.Value == 500,
            "Rejected moon edit blocked the next valid price update.");
        host.Moons.LocalValue = "Titan@NaN";
        Assert(host.Moons.LocalValue == "Titan@0.01", "Non-finite moon value was accepted.");
        host.Moons.LocalValue = new string('x', ConfigSnapshot.MaxBytes);
        Assert(host.Moons.LocalValue == "Titan@0.01" && CustomMessagingManager.Pending.Count == 0,
            "Oversized host edit changed gameplay before enforcing the wire limit.");
        host.Config.RefreshFailuresRemaining = 1;
        host.Enabled.LocalValue = false;
        Assert(host.Enabled.Value && host.Enabled.LocalValue && client.Enabled.Value && CustomMessagingManager.Pending.Count == 0,
            "Failed host world-effect refresh did not restore active and local settings.");
        host.Moons.LocalValue = "Titan@0.25;Vow@0.5"; CustomMessagingManager.Pump();
        Assert(host.Moons.Value == client.Moons.Value && client.Config.OutsideMoons["titan"] == 0.25f,
            "Valid host moon edit did not reach the client after rejected edits.");
        // A later join receives the accepted snapshot, rather than an unvalidated config-file value.
        host.Drain.LocalValue = float.NaN;
        var late = new Peer("Late@9", 9, false, 9, Mode.Off);
        var lateNetwork = new NetworkManager(2, false); server.ConnectedClients.Add(2, new());
        late.Sync.StartSession(lateNetwork); CustomMessagingManager.Pump();
        Assert(late.Price.Value == 500 && late.Drain.Value == host.Drain.Value && late.Moons.Value == host.Moons.Value,
            "Late join received rejected host-file settings.");
        lateNetwork.Stop(); network.Stop(); server.Stop(); CustomMessagingManager.Pending.Clear();
    }

    private static void SlowTransferChecks()
    {
        CustomMessagingManager.Pending.Clear(); CustomMessagingManager.Peers.Clear();
        string moons = string.Join(";", Enumerable.Range(0, 10000).Select(i => $"Moon{i}@0.0123"));
        var host = new Peer(moons, 400, true, 0.01f, Mode.OnShip);
        var client = new Peer("Local@1", 999, false, 0.9f, Mode.Off);
        var server = new NetworkManager(0, true); server.ConnectedClients.Add(1, new());
        var network = new NetworkManager(1, false);
        host.Sync.StartSession(server); client.Sync.StartSession(network);
        CustomMessagingManager.Deliver(CustomMessagingManager.Pending.Dequeue());
        var chunks = CustomMessagingManager.Pending.ToArray(); CustomMessagingManager.Pending.Clear();
        Assert(chunks.Length > 5, "Slow-transfer fixture did not exceed the old retry deadline.");
        float startTime = UnityEngine.Time.realtimeSinceStartup;
        for (int i = 0; i < chunks.Length; i++)
        {
            CustomMessagingManager.Deliver(chunks[i]); Oxygen.OxygenBase.Instance.Tick();
            Assert(network.IsListening && network.ShutdownCount == 0, "Progressing slow transfer was disconnected.");
            Assert(CustomMessagingManager.Pending.Count == 0, "Progressing transfer requested a redundant full snapshot.");
            if (i + 1 < chunks.Length) Assert(client.Price.Value == 70, "Slow partial transfer changed active gameplay.");
        }
        Assert(UnityEngine.Time.realtimeSinceStartup - startTime > 10 && client.Price.Value == 400 &&
            client.Moons.Value == moons && client.Config.OutsideMoons["moon9999"] == 0.0123f,
            "Slow transfer failed to apply the complete snapshot beyond ten seconds.");
        network.Stop(); CustomMessagingManager.Pending.Clear();
        // Genuine stalls still expire ten seconds after the last valid chunk.
        network.IsListening = true; network.IsConnectedClient = true;
        client.Sync.StartSession(network);
        CustomMessagingManager.Deliver(CustomMessagingManager.Pending.Dequeue());
        var first = CustomMessagingManager.Pending.Dequeue(); CustomMessagingManager.Pending.Clear();
        CustomMessagingManager.Deliver(first);
        for (int i = 0; i < 4; i++) Oxygen.OxygenBase.Instance.Tick();
        Assert(network.IsListening, "Stalled transfer disconnected before the inactivity limit.");
        Oxygen.OxygenBase.Instance.Tick();
        Assert(!network.IsListening && network.ShutdownCount == 1 && client.Price.Value == 999,
            "Stalled transfer did not disconnect and clean up after ten seconds without progress.");
        server.Stop(); CustomMessagingManager.Pending.Clear();
    }

    private static void CanisterChecks()
    {
        const string parentPath = "Environment/HangarShip/ScavengerModelSuitParts";
        const string canisterPath = parentPath + "/Circle.002";
        UnityEngine.GameObject.Scene.Clear(); Oxygen.General.VanillaCanisters.Reset();
        var parent = new UnityEngine.GameObject(parentPath);
        var canisters = new UnityEngine.GameObject(canisterPath, parent);
        Oxygen.General.VanillaCanisters.SetVisible(false);
        Assert(!canisters.activeSelf && UnityEngine.GameObject.Find(canisterPath) == null,
            "Canister fixture did not reproduce Unity's active-only GameObject.Find behavior.");
        Oxygen.General.VanillaCanisters.SetVisible(true);
        Assert(canisters.activeSelf, "Disabling/despawning the charger failed to restore hidden canisters.");
        Oxygen.General.VanillaCanisters.SetVisible(false); Oxygen.General.VanillaCanisters.Reset();
        Oxygen.General.VanillaCanisters.SetVisible(true);
        Assert(canisters.activeSelf, "Inactive-child lookup failed without a cached canister reference.");
        for (int i = 0; i < 3; i++)
        {
            Oxygen.General.VanillaCanisters.SetVisible(false);
            Assert(!canisters.activeSelf, "Re-enabling charger failed to hide vanilla canisters.");
            Oxygen.General.VanillaCanisters.SetVisible(true);
            Assert(canisters.activeSelf, "Repeated charger disable failed to restore vanilla canisters.");
        }
        // Unity treats destroyed cached objects as null; a different lobby must resolve a fresh object.
        parent.Destroy();
        var nextParent = new UnityEngine.GameObject(parentPath);
        var nextCanisters = new UnityEngine.GameObject(canisterPath, nextParent); nextCanisters.SetActive(false);
        Oxygen.General.VanillaCanisters.SetVisible(true);
        Assert(nextCanisters.activeSelf, "Destroyed scene cache prevented restoration in another lobby.");
        UnityEngine.GameObject.Scene.Clear(); Oxygen.General.VanillaCanisters.Reset();
        Oxygen.General.VanillaCanisters.SetVisible(true); // Missing/tearing-down scene is harmless.
        Assert(true, "Missing canisters threw during teardown.");
    }

    private static void WeatherAndRefillChecks()
    {
        var config = new OxygenConfig
        {
            eclipsed_LimitOxygen = new(new("LimitWeather", true, true)),
            eclipsed_LimitedOxygenAmount = new(new("Capacity", 10f, 10f)),
            autoFillingOnShip_increasingOxygen = new(new("Refill", 0.05f, 0.05f))
        };
        Oxygen.OxygenBase.OxygenConfig = config;
        StartOfRound.Instance = new();
        var server = new NetworkManager(0, true);
        Oxygen.Items.OxyCharger.Instance = null;
        Oxygen.General.WeatherHandler.Reset();
        StartOfRound.Instance.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
        Oxygen.General.WeatherHandler.UpdateWeatherType(StartOfRound.Instance.currentLevel);
        Assert(Oxygen.General.WeatherHandler.IsOxygenOnShipLimited, "Missing/disabled charger broke weather-limit evaluation.");
        var charger = new Oxygen.Items.OxyCharger { IsSpawned = true };
        Oxygen.Items.OxyCharger.Instance = charger;
        Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.RemainedOxygenAmount == 10, "Charger spawned after weather initialization without a reserve.");
        charger.RemainedOxygenAmount = 3; int writes = charger.Writes;
        Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.RemainedOxygenAmount == 3 && charger.Writes == writes, "Unrelated config refresh refilled a consumed reserve.");
        config.eclipsed_LimitOxygen.LocalValue = false; Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(!Oxygen.General.WeatherHandler.IsOxygenOnShipLimited, "Live weather-disable edit did not apply.");
        config.eclipsed_LimitOxygen.LocalValue = true;
        config.eclipsed_LimitedOxygenAmount.LocalValue = 20;
        Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.RemainedOxygenAmount == 20 && Oxygen.General.WeatherHandler.IsOxygenOnShipLimited,
            "Live weather re-enable did not initialize the configured reserve.");
        config.eclipsed_LimitedOxygenAmount.LocalValue = 25; Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.RemainedOxygenAmount == 25, "Changed reserve capacity remained cached.");
        var client = new NetworkManager(1, false);
        charger.RemainedOxygenAmount = 4; writes = charger.Writes;
        Oxygen.General.WeatherHandler.UpdateWeatherType(StartOfRound.Instance.currentLevel);
        Assert(charger.RemainedOxygenAmount == 4 && charger.Writes == writes,
            "Late-join client weather initialization wrote/refilled the host reserve.");
        NetworkManager.Singleton = server;
        Oxygen.General.WeatherHandler.RefreshLimit();
        charger.RemainedOxygenAmount = 4; writes = charger.Writes;
        Oxygen.General.WeatherHandler.ChargerDespawned(4); charger.IsSpawned = false;
        Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.Writes == writes, "Weather config wrote into an unspawned charger.");
        charger = new Oxygen.Items.OxyCharger { IsSpawned = true };
        Oxygen.Items.OxyCharger.Instance = charger; Oxygen.General.WeatherHandler.RefreshLimit();
        Assert(charger.RemainedOxygenAmount == 4, "Recreated charger did not preserve its remaining reserve.");
        StartOfRound.Instance.shipIsLeaving = true;
        Assert(!Oxygen.General.WeatherHandler.IsOxygenOnShipLimited, "Departing ship remained oxygen-limited.");
        StartOfRound.Instance.shipIsLeaving = false; StartOfRound.Instance.inShipPhase = true;
        Assert(!Oxygen.General.WeatherHandler.IsOxygenOnShipLimited, "Orbit retained planet oxygen limits.");
        StartOfRound.Instance.inShipPhase = false;
        Oxygen.General.WeatherHandler.Reset();
        Assert(!Oxygen.General.WeatherHandler.IsOxygenOnShipLimited, "Session reset retained the previous host's weather flag.");
        StartOfRound.Instance.currentLevel.currentWeather = LevelWeatherType.None;
        Oxygen.General.WeatherHandler.RefreshLimit();
        Oxygen.Items.OxyCharger.Instance = null; Oxygen.General.OxygenInit.Percent = 0.5f;
        Oxygen.General.ShipRefill.Refill();
        Assert(Oxygen.General.OxygenInit.Percent == 0.55f, "Automatic ship refill required a physical charger.");
        Oxygen.General.OxygenInit.Percent = 0.99f; Oxygen.General.ShipRefill.Refill();
        Assert(Oxygen.General.OxygenInit.Percent == 1f, "Charger-free auto refill exceeded full oxygen.");
        StartOfRound.Instance.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
        Oxygen.General.WeatherHandler.RefreshLimit(); Oxygen.General.OxygenInit.Percent = 0.5f;
        Oxygen.General.ShipRefill.Refill();
        Assert(Oxygen.General.OxygenInit.Percent == 0.5f, "Missing charger bypassed the finite oxygen reserve.");
        Oxygen.Items.OxyCharger.Instance = charger;
        Oxygen.General.ShipRefill.Refill();
        Assert(charger.RefillCalls == 1, "Spawned charger refill no longer used its shared reserve.");
        Oxygen.Items.OxyCharger.Instance = null; Oxygen.General.WeatherHandler.Reset();
        StartOfRound.Instance = new();
    }

    private static void ShopChecks()
    {
        var item = new Item { name = "oxyboost" };
        Oxygen.OxygenBase.Instance.oxyBoost = item;
        var shop = new LethalLib.Modules.Items.ShopItem { item = item };
        LethalLib.Modules.Items.shopItems.Add(shop);
        var boostKeyword = new TerminalKeyword { word = item.name };
        var boostNoun = new CompatibleNoun { noun = boostKeyword };
        var buyKeyword = new TerminalKeyword { word = "buy", compatibleNouns = new[] { boostNoun } };
        var nodes = new TerminalNodesList { allKeywords = new[] { buyKeyword, boostKeyword } };
        var terminal = new Terminal { terminalNodes = nodes };
        UnityEngine.Object.ActiveTerminal = terminal;
        OxyBoostShop.Update(item, false, 100);
        Assert(shop.wasRemoved && !nodes.allKeywords.Contains(boostKeyword) && !buyKeyword.compatibleNouns.Contains(boostNoun), "Disabled OxyBoost remained buyable.");
        Assert(shop.price == 100, "Disabled shop price was not stored.");
        var otherKeyword = new TerminalKeyword { word = "othermod" };
        var otherNoun = new CompatibleNoun { noun = otherKeyword };
        nodes.allKeywords = nodes.allKeywords.Append(otherKeyword).ToArray();
        buyKeyword.compatibleNouns = buyKeyword.compatibleNouns.Append(otherNoun).ToArray();
        OxyBoostShop.Update(item, true, 200);
        Assert(!shop.wasRemoved && nodes.allKeywords.Contains(boostKeyword) && buyKeyword.compatibleNouns.Contains(boostNoun), "Re-enabling did not restore OxyBoost.");
        Assert(nodes.allKeywords.Contains(otherKeyword) && buyKeyword.compatibleNouns.Contains(otherNoun), "Restoring OxyBoost discarded another mod's shop changes.");
        Assert(shop.price == 200, "Enabled price did not update.");
        OxyBoostShop.Update(item, true, 300);
        Assert(nodes.allKeywords.Count(k => k == boostKeyword) == 1 && buyKeyword.compatibleNouns.Count(n => n == boostNoun) == 1, "Repeated refresh duplicated shop entries.");
        OxyBoostShop.Update(item, false, 400);
        OxyBoostShop.Reset();
        Assert(nodes.allKeywords.Contains(boostKeyword) && buyKeyword.compatibleNouns.Contains(boostNoun) && !shop.wasRemoved, "Shared terminal assets retained a previous lobby's hidden item.");
        UnityEngine.Object.ActiveTerminal = new Terminal { terminalNodes = nodes };
        OxyBoostShop.Update(item, false, 500);
        Assert(shop.wasRemoved, "New lobby did not apply its disabled setting.");
        OxyBoostShop.Update(item, true, 600);
        Assert(!shop.wasRemoved && shop.price == 600, "New lobby did not restore its enabled setting.");
        OxyBoostShop.Reset();
    }

    static void Main()
    {
        string longList = string.Join(";", Enumerable.Range(0, 3000).Select(i => $"Moon{i}@0.002")) + ";LastMoon@0.123";
        CodecChecks(longList); OverrideChecks(); SessionChecks(longList); SessionRegressionChecks(longList);
        HostEditRegressionChecks(); SlowTransferChecks(); CanisterChecks(); ShopChecks(); WeatherAndRefillChecks();
        Console.WriteLine($"Passed {checks} config sync checks (production codec, overrides, and transport with engine doubles).");
    }
}
