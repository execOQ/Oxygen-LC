// Minimal engine/config doubles let the production sync code run without a Unity player.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BepInEx.Configuration
{
    public sealed class ConfigDefinition
    {
        public string Section { get; set; } = "Gameplay";
        public string Key { get; set; }
    }
    public sealed class ConfigDescription { public AcceptableValueBase AcceptableValues { get; set; } }
    public abstract class AcceptableValueBase { public abstract bool IsValid(object value); }
    public sealed class ConfigEntry<T>
    {
        public ConfigDefinition Definition { get; }
        public ConfigDescription Description { get; } = new();
        public object DefaultValue { get; }
        private T value;
        public event EventHandler SettingChanged;
        public T Value { get => value; set { this.value = value; SettingChanged?.Invoke(this, EventArgs.Empty); } }
        public ConfigEntry(string key, T local, T fallback)
        { Definition = new() { Key = key }; value = local; DefaultValue = fallback; }
        public string GetSerializedValue() => Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    public static class TomlTypeConverter
    {
        public static string ConvertToString(object value, Type type) => Convert.ToString(value, CultureInfo.InvariantCulture);
        public static object ConvertToValue(string text, Type type) => type.IsEnum
            ? Enum.Parse(type, text) : Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
    }
}

namespace Unity.Collections { public enum Allocator { Temp } }
namespace UnityEngine
{
    public static class Time { public static float realtimeSinceStartup; }
    public class Object
    {
        internal static Terminal ActiveTerminal;
        public static T FindObjectOfType<T>() where T : class => ActiveTerminal as T;
    }
    public sealed class GameObject : Object
    {
        internal static readonly List<GameObject> Scene = new();
        internal readonly string Path;
        public Transform transform { get; }
        public bool activeSelf { get; private set; } = true;
        internal bool Destroyed;
        internal bool ActiveInHierarchy => !Destroyed && activeSelf && (transform.Parent == null || transform.Parent.gameObject.ActiveInHierarchy);
        internal GameObject(string path, GameObject parent = null)
        {
            Path = path; transform = new(this, parent?.transform);
            parent?.transform.Children.Add(transform); Scene.Add(this);
        }
        public static GameObject Find(string path) => Scene.FirstOrDefault(obj => obj.Path == path && obj.ActiveInHierarchy);
        public void SetActive(bool active) => activeSelf = active;
        internal void Destroy()
        { Destroyed = true; foreach (var child in transform.Children) child.gameObject.Destroy(); }
        public static bool operator ==(GameObject left, GameObject right)
        {
            bool leftNull = ReferenceEquals(left, null) || left.Destroyed;
            bool rightNull = ReferenceEquals(right, null) || right.Destroyed;
            return leftNull || rightNull ? leftNull == rightNull : ReferenceEquals(left, right);
        }
        public static bool operator !=(GameObject left, GameObject right) => !(left == right);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => base.GetHashCode();
    }
    public sealed class Transform
    {
        public GameObject gameObject { get; }
        internal Transform Parent;
        internal readonly List<Transform> Children = new();
        internal Transform(GameObject obj, Transform parent) { gameObject = obj; Parent = parent; }
        // Like Unity's Transform.Find, this includes inactive children.
        public Transform Find(string name) => Children.FirstOrDefault(child => !child.gameObject.Destroyed && child.gameObject.Path.Split('/').Last() == name);
    }
    public sealed class WaitForSecondsRealtime { public WaitForSecondsRealtime(float seconds) { } }
    public sealed class Coroutine { internal IEnumerator Routine; }
}
namespace Oxygen
{
    public sealed class OxygenBase
    {
        public const string modGUID = "OxygenTest";
        public const string modName = "OxygenTest";
        internal static Configuration.OxygenConfig OxygenConfig;
        public Item oxyBoost;
        public static OxygenBase Instance { get; } = new();
        private readonly List<UnityEngine.Coroutine> routines = new();
        public UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            var result = new UnityEngine.Coroutine { Routine = routine };
            if (routine.MoveNext()) routines.Add(result);
            return result;
        }
        public void StopCoroutine(UnityEngine.Coroutine routine) => routines.Remove(routine);
        internal void Tick()
        {
            UnityEngine.Time.realtimeSinceStartup += 2f;
            foreach (var routine in routines.ToArray()) if (!routine.Routine.MoveNext()) routines.Remove(routine);
        }
    }
}

public sealed class Item { public string name; }
public sealed class CompatibleNoun { public TerminalKeyword noun; }
public sealed class TerminalKeyword : UnityEngine.Object
{
    public string word;
    public CompatibleNoun[] compatibleNouns = Array.Empty<CompatibleNoun>();
}
public sealed class TerminalNodesList : UnityEngine.Object { public TerminalKeyword[] allKeywords; }
public sealed class Terminal : UnityEngine.Object { public TerminalNodesList terminalNodes; }
public enum LevelWeatherType { None, Eclipsed }
public class SelectableLevel { public string PlanetName = "Titan"; public LevelWeatherType currentWeather; }
public sealed class TestLevel : SelectableLevel { }
public sealed class StartOfRound
{
    public static StartOfRound Instance = new();
    public TestLevel currentLevel = new();
    public bool shipIsLeaving, inShipPhase;
}
public sealed class GameNetworkManager
{
    public static GameNetworkManager Instance;
    public string disconnectionReasonMessage;
    internal Unity.Netcode.NetworkManager Target;
    internal int DisconnectCount;
    public void Disconnect() { DisconnectCount++; Target.Stop(); }
}
namespace BepInEx.Logging
{
    public sealed class ManualLogSource
    {
        internal readonly List<string> Warnings = new();
        public void LogInfo(object message) { }
        public void LogWarning(object message) => Warnings.Add(message.ToString());
        public void LogError(object message) { }
    }
    public static class Logger { public static ManualLogSource CreateLogSource(string name) => new(); }
}
namespace Oxygen.Extras
{
    public static class Utilities { public static string GetNumberlessPlanetName(string name) => name; }
}
namespace LethalLib.Modules
{
    public static class Items
    {
        public sealed class ShopItem { public Item item; public bool wasRemoved; public int price; }
        public static List<ShopItem> shopItems = new();
        public static void RemoveShopItem(Item item)
        {
            shopItems.Single(entry => entry.item == item).wasRemoved = true;
            var nodes = UnityEngine.Object.ActiveTerminal.terminalNodes;
            foreach (var keyword in nodes.allKeywords)
                keyword.compatibleNouns = keyword.compatibleNouns.Where(noun => noun.noun.word != item.name).ToArray();
            nodes.allKeywords = nodes.allKeywords.Where(keyword => keyword.word != item.name).ToArray();
        }
        public static void UpdateShopItemPrice(Item item, int price) => shopItems.Single(entry => entry.item == item).price = price;
    }
}
namespace Oxygen.Configuration
{
    internal sealed class TestLog
    {
        internal readonly List<string> Errors = new();
        public void LogError(object error) => Errors.Add(error.ToString());
        public void LogInfo(object message) { }
    }
    internal sealed class OxygenConfig
    {
        internal static TestLog mls { get; } = new();
        internal int RefreshCount;
        internal int RefreshFailuresRemaining, ResetFailuresRemaining;
        internal Dictionary<string, float> OutsideMoons;
        internal HostConfigEntry<string> greenPlanets, decreasingOxygenOutsideMoons,
            decreasingOxygenInFactoryMoons, runningMultiplierMoons, oxygenDepletionInWaterMoons;
        internal HostConfigEntry<float> decreasingOxygenOutside, decreasingOxygenInFactory,
            runningMultiplier, oxygenDepletionInWater;
        internal HostConfigEntry<bool> eclipsed_LimitOxygen;
        internal HostConfigEntry<float> eclipsed_LimitedOxygenAmount, autoFillingOnShip_increasingOxygen;
        internal void ValidateHostSettings(IReadOnlyDictionary<IHostConfigEntry, object> values) => MoonsDicts.Prepare(this, values);
        internal void RefreshEffects(bool updateCharger = true)
        {
            Oxygen.OxygenBase.OxygenConfig = this;
            MoonsDicts.UpdateMoonsValues();
            OutsideMoons = MoonsDicts.decreasingOxygenOutsideMoons;
            if (RefreshFailuresRemaining > 0) { RefreshFailuresRemaining--; throw new InvalidOperationException("World effect failed."); }
            RefreshCount++;
        }
        internal void ResetSessionState()
        {
            MoonsDicts.Clear();
            Oxygen.General.VanillaCanisters.Reset();
            OutsideMoons = null;
            if (ResetFailuresRemaining > 0) { ResetFailuresRemaining--; throw new InvalidOperationException("Presentation reset failed."); }
        }
    }
}
namespace Unity.Netcode
{
    public enum NetworkDelivery { ReliableSequenced, ReliableFragmentedSequenced }
    public sealed class FastBufferWriter : IDisposable
    {
        private readonly MemoryStream stream = new();
        private readonly BinaryWriter writer;
        public FastBufferWriter(int capacity, Unity.Collections.Allocator allocator) => writer = new(stream);
        public void WriteValueSafe(int value) => writer.Write(value);
        public void WriteBytesSafe(byte[] data, int length, int offset) => writer.Write(data, offset, length);
        internal byte[] Bytes => stream.ToArray();
        public void Dispose() => writer.Dispose();
    }
    public sealed class FastBufferReader
    {
        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        internal FastBufferReader(byte[] data) { stream = new(data); reader = new(stream); }
        public int Length => (int)stream.Length;
        public int Position => (int)stream.Position;
        public void ReadValueSafe(out int value) => value = reader.ReadInt32();
        public void ReadBytesSafe(ref byte[] data, int length) => reader.ReadBytes(length).CopyTo(data, 0);
    }
    internal record Packet(ulong Sender, ulong Target, string Name, byte[] Bytes);
    public sealed class CustomMessagingManager
    {
        internal static readonly Queue<Packet> Pending = new();
        internal static readonly Dictionary<ulong, NetworkManager> Peers = new();
        internal readonly Dictionary<string, Action<ulong, FastBufferReader>> Handlers = new();
        internal int SendFailuresRemaining;
        private readonly NetworkManager owner;
        internal CustomMessagingManager(NetworkManager owner) => this.owner = owner;
        public void RegisterNamedMessageHandler(string name, Action<ulong, FastBufferReader> handler) => Handlers.Add(name, handler);
        public void UnregisterNamedMessageHandler(string name) => Handlers.Remove(name);
        public void SendNamedMessage(string name, ulong target, FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (SendFailuresRemaining > 0) { SendFailuresRemaining--; throw new IOException("Transport send failed."); }
            Pending.Enqueue(new(owner.LocalClientId, target, name, writer.Bytes));
        }
        internal static void Deliver(Packet packet)
        {
            if (Peers.TryGetValue(packet.Target, out var peer) && peer.CustomMessagingManager.Handlers.TryGetValue(packet.Name, out var handler))
                handler(packet.Sender, new(packet.Bytes));
        }
        internal static void Pump() { while (Pending.Count > 0) Deliver(Pending.Dequeue()); }
    }
    public sealed class NetworkManager
    {
        public static NetworkManager Singleton { get; set; }
        public const ulong ServerClientId = 0;
        public bool IsServer { get; set; }
        public bool IsListening { get; set; } = true;
        public bool IsConnectedClient { get; set; } = true;
        internal int ShutdownCount;
        public ulong LocalClientId { get; }
        public Dictionary<ulong, object> ConnectedClients { get; } = new();
        public IReadOnlyList<ulong> ConnectedClientsIds => ConnectedClients.Keys.ToArray();
        public CustomMessagingManager CustomMessagingManager { get; }
        public event Action<ulong> OnClientConnectedCallback;
        public event Action<ulong> OnClientDisconnectCallback;
        public event Action<bool> OnClientStopped;
        public event Action<bool> OnServerStopped;
        internal NetworkManager(ulong id, bool server)
        { LocalClientId = id; IsServer = server; CustomMessagingManager = new(this); CustomMessagingManager.Peers[id] = this; Singleton = this; }
        internal void Connect() { IsConnectedClient = true; OnClientConnectedCallback?.Invoke(LocalClientId); }
        internal void DisconnectRemote(ulong id) => OnClientDisconnectCallback?.Invoke(id);
        public void Shutdown() { ShutdownCount++; Stop(); }
        internal void Stop()
        {
            IsListening = false;
            OnClientDisconnectCallback?.Invoke(LocalClientId);
            OnClientStopped?.Invoke(IsServer);
            if (IsServer) OnServerStopped?.Invoke(true);
        }
    }
}
namespace Oxygen.General
{
    // Session integration is covered with the real RefillSync in ChargerLifecycle.
    internal sealed class RefillSync
    {
        internal Unity.Netcode.NetworkManager Session;
        public void StartSession(Unity.Netcode.NetworkManager manager) => Session = manager;
        public void EndSession() => Session = null;
    }
    public static class OxygenInit { public static float Percent; }
}
namespace Oxygen.Items
{
    public sealed class OxyCharger
    {
        public static OxyCharger Instance;
        public bool IsSpawned;
        private float reserve;
        public int Writes, RefillCalls;
        public float RemainedOxygenAmount { get => reserve; set { reserve = value; Writes++; } }
        public void AutoRefillOxygen() => RefillCalls++;
    }
}
