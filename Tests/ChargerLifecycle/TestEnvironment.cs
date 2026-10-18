// These doubles exercise the production registration/lifecycle helper without a Unity player.
using System.Collections.Generic;
using System.Linq;
using Oxygen.General;
using Oxygen.Items;

namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(GameObject obj) => obj.Destroyed = true;
        public static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var obj = new GameObject();
            obj.transform.position = position;
            obj.Components.Add(new Unity.Netcode.NetworkObject { gameObject = obj });
            obj.Components.Add(new PlaceableShipObject { unlockableID = prefab.GetComponent<PlaceableShipObject>().unlockableID, Owner = obj });
            var template = prefab.GetComponent<AutoParentToShip>();
            obj.Components.Add(new AutoParentToShip
            {
                unlockableID = template.unlockableID,
                positionOffset = template.positionOffset, startingPosition = template.positionOffset,
                rotationOffset = template.rotationOffset, startingRotation = template.rotationOffset,
                Owner = obj
            });
            obj.Components.Add(new AudioSource());
            var charger = new OxyCharger { gameObject = obj };
            obj.Components.Add(charger);
            typeof(OxyCharger).GetMethod("Awake", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(charger, null);
            return obj;
        }
    }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : new() => new();
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new();
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 right => new(1, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 forward => new(0, 0, 1);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 vector, float scale) => new(vector.x * scale, vector.y * scale, vector.z * scale);
        public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object other) => other is Vector3 vector && this == vector;
        public override int GetHashCode() => System.HashCode.Combine(x, y, z);
    }
    public struct Quaternion
    {
        private System.Numerics.Quaternion value;
        public static Quaternion identity => new() { value = System.Numerics.Quaternion.Identity };
        public static Quaternion Euler(Vector3 angles) => new()
        {
            value = System.Numerics.Quaternion.CreateFromYawPitchRoll(
                angles.y * System.MathF.PI / 180, angles.x * System.MathF.PI / 180, angles.z * System.MathF.PI / 180)
        };
        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            var result = System.Numerics.Vector3.Transform(new(point.x, point.y, point.z), rotation.value);
            return new(result.X, result.Y, result.Z);
        }
    }
    public class AudioClip { }
    public class AudioSource
    {
        public bool isPlaying;
        public void Stop() { }
        public void PlayOneShot(AudioClip clip, float volume) { }
    }
    public static class Random { public static int Range(int min, int max) => min; }
    public static class Time { public static float realtimeSinceStartup; }
    public static class Mathf
    {
        public static float Abs(float value) => System.Math.Abs(value);
        public static float Max(float a, float b) => System.Math.Max(a, b);
        public static float Min(float a, float b) => System.Math.Min(a, b);
    }
    public class Transform
    {
        public GameObject gameObject;
        public Transform parent;
        public Vector3 position, eulerAngles, localScale = Vector3.one;
        public Vector3 lossyScale => parent == null ? localScale : Vector3.Scale(parent.lossyScale, localScale);
        public Vector3 TransformPoint(Vector3 point) => position + Vector3.Scale(point, lossyScale);
        public Transform Find(string path) => GameObject.Cylinders?.transform;
        public Vector3 InverseTransformPoint(Vector3 value) => new() { x = value.x - position.x };
        public Vector3 InverseTransformDirection(Vector3 value) => value;
    }
    public class Collider { public Transform transform = new(); }
    public class BoxCollider : Collider { public Vector3 center, size; }
    public class GameObject : Object
    {
        public static GameObject SuitParts, Cylinders;
        public Transform transform;
        public List<object> Components = new();
        public bool Destroyed;
        public GameObject() => transform = new() { gameObject = this };
        public static GameObject Find(string path) => SuitParts;
        public T GetComponent<T>() where T : class => Components.OfType<T>().FirstOrDefault();
        public T GetComponentInChildren<T>() where T : class => GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Components.OfType<T>().ToArray();
    }
}
namespace Unity.Netcode
{
    public class NetworkManager
    {
        public static NetworkManager Singleton;
        public bool IsServer, IsListening = true;
        public const ulong ServerClientId = 0;
        public ulong LocalClientId;
        public Dictionary<ulong, object> ConnectedClients = new();
        public CustomMessagingManager CustomMessagingManager;
        public event System.Action<ulong> OnClientDisconnectCallback;
        public NetworkManager(ulong id = 0)
        {
            LocalClientId = id;
            ConnectedClients[id] = new();
            CustomMessagingManager = new(this);
        }
        public void Disconnect(ulong id)
        {
            ConnectedClients.Remove(id);
            if (id == LocalClientId) IsListening = false;
            OnClientDisconnectCallback?.Invoke(id);
        }
    }
    public class NetworkObject
    {
        public UnityEngine.GameObject gameObject;
        private static ulong nextId;
        public ulong NetworkObjectId = ++nextId;
        public bool IsSpawned;
        public int Despawns;
        public void Spawn()
        {
            IsSpawned = true;
            StartOfRound.Instance.Spawns++;
            StartOfRound.Instance.PositionAtSpawn = gameObject.transform.position;
            gameObject.GetComponent<OxyCharger>().OnNetworkSpawn();
        }
        public void Despawn()
        {
            IsSpawned = false; Despawns++;
            gameObject.GetComponent<OxyCharger>().OnNetworkDespawn();
        }
    }
    public class NetworkVariable<T> { public T Value; }
    public class NetworkBehaviour
    {
        public UnityEngine.GameObject gameObject;
        public bool IsServer => NetworkManager.Singleton.IsServer;
        public bool IsSpawned => gameObject.GetComponent<NetworkObject>().IsSpawned;
        public ulong NetworkObjectId => gameObject.GetComponent<NetworkObject>().NetworkObjectId;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }
    }
    public class ServerRpcAttribute : System.Attribute { public bool RequireOwnership; }
    public class ClientRpcAttribute : System.Attribute { }
}
namespace BepInEx.Logging
{
    public class ManualLogSource
    {
        public void LogDebug(object message) { }
        public void LogError(object message) { }
        public void LogInfo(object message) { }
    }
    public static class Logger { public static ManualLogSource CreateLogSource(string name) => new(); }
}
public class UnlockableItem
{
    public string unlockableName;
    public UnityEngine.GameObject prefabObject;
    public int unlockableType, maxNumber;
    public bool alreadyUnlocked, IsPlaceable, canBeStored, spawnPrefab;
    public bool inStorage, hasBeenMoved, hasBeenUnlockedByPlayer;
    public UnityEngine.Vector3 placedPosition, placedRotation;
}
public class UnlockablesList { public List<UnlockableItem> unlockables = new(); }
public class TerminalNode : UnityEngine.ScriptableObject
{
    public bool clearPreviousText, returnFromStorage;
    public string displayText;
    public int shipUnlockableID;
}
public class PlaceableShipObject
{
    public int unlockableID;
    public UnityEngine.GameObject Owner;
    public UnityEngine.Collider placeObjectCollider;
    public float yOffset;
}
public class AutoParentToShip
{
    public int unlockableID;
    public bool overrideOffset;
    public UnityEngine.Vector3 positionOffset, rotationOffset;
    public UnityEngine.Vector3 startingPosition, startingRotation;
    public UnityEngine.GameObject Owner;
    public void MoveToOffset() => Owner.transform.position = positionOffset;
}
public class StartOfRound
{
    public static StartOfRound Instance;
    public Dictionary<ulong, int> ClientPlayerList = new();
    public Player[] allPlayerScripts = new Player[16];
    public UnlockablesList unlockablesList = new();
    public UnityEngine.Transform elevatorTransform = new();
    public Dictionary<int, UnityEngine.GameObject> SpawnedShipUnlockables = new();
    public int Spawns;
    public float LastPlacedPosition;
    public UnityEngine.Vector3 LastPlacedRotation, PositionAtSpawn;
    public UnityEngine.Vector3? DiskPosition;
    public UnityEngine.Vector3 DiskRotation;
    public SelectableLevel currentLevel = new();
    public bool shipIsLeaving, inShipPhase, hangarDoorsClosed = true;
    public bool IsServer => Unity.Netcode.NetworkManager.Singleton.IsServer;
    public int ReturnBroadcasts, ReturnRequests;
    public void SpawnUnlockable(int id)
    {
        if (!ChargerUnlockable.BeforeSpawn(this, id)) return;
        var obj = UnityEngine.Object.Instantiate(unlockablesList.unlockables[id].prefabObject,
            elevatorTransform.position, UnityEngine.Quaternion.identity);
        // Native default spawning reads ES3, not the latest in-memory placement.
        if (DiskPosition.HasValue)
            ShipBuildModeManager.Instance.PlaceShipObject(DiskPosition.Value, DiskRotation,
                obj.GetComponent<PlaceableShipObject>(), false, obj);
        obj.GetComponent<Unity.Netcode.NetworkObject>().Spawn();
        SpawnedShipUnlockables.Add(id, obj);
    }
    public void ReturnUnlockableFromStorageServerRpc(int id)
    {
        if (!ChargerUnlockable.CanReturnFromStorage(this, id)) return;
        if (!IsServer) { ReturnRequests++; return; }
        var item = unlockablesList.unlockables[id];
        if (!item.inStorage || SpawnedShipUnlockables.ContainsKey(id)) return;
        SpawnUnlockable(id);
        item.inStorage = false;
        ReturnBroadcasts++;
    }
    public void Store(int id)
    {
        unlockablesList.unlockables[id].inStorage = true;
        SpawnedShipUnlockables[id].GetComponent<Unity.Netcode.NetworkObject>().Despawn();
        SpawnedShipUnlockables.Remove(id);
    }
}
public enum LevelWeatherType { None, Eclipsed }
public class SelectableLevel { public LevelWeatherType currentWeather; }
public class ShipBuildModeManager
{
    public static ShipBuildModeManager Instance = new();
    public void PlaceShipObject(UnityEngine.Vector3 position, UnityEngine.Vector3 rotation,
        PlaceableShipObject placeable, bool placementSFX = false, UnityEngine.GameObject owner = null)
    {
        var round = StartOfRound.Instance;
        var item = round.unlockablesList.unlockables[placeable.unlockableID];
        item.placedPosition = position; item.placedRotation = rotation; item.hasBeenMoved = true;
        round.LastPlacedPosition = position.x; round.LastPlacedRotation = rotation;
        // Native placement changes parent offsets; its transform updates at MoveToOffset/LateUpdate.
        owner ??= placeable.Owner;
        if (owner != null) owner.GetComponent<AutoParentToShip>().positionOffset = position;
    }
}
namespace LethalLib.Modules
{
    public enum StoreType { None }
    public static class Unlockables
    {
        public static UnlockableItem Registered;
        public static int Registrations;
        public static void RegisterUnlockable(UnlockableItem item, int price, StoreType store)
        { Registered = item; Registrations++; }
    }
    public static class TerminalUtils
    {
        public static TerminalNode Node;
        public static void CreateTerminalKeyword(string word, TerminalNode node) => Node = node;
    }
}
namespace Oxygen
{
    public class OxygenBase
    {
        public const string modName = "Oxygen";
        public const string modGUID = "test.Oxygen";
        public static OxygenBase Instance = new();
        public UnityEngine.AudioClip[] oxyChargerSFX = new UnityEngine.AudioClip[1];
        public static Config OxygenConfig = new();
        internal RefillSync Refills = new();
    }
    public class Config
    {
        public Entry oxyCharger_Enabled = new(), eclipsed_LimitOxygen = new();
        public NumberEntry eclipsed_LimitedOxygenAmount = new() { Value = 10 };
        public NumberEntry autoFillingOnShip_increasingOxygen = new() { Value = 0.05f };
        public NumberEntry oxyCharger_SFXVolume = new() { Value = 1 };
        public ModeEntry autoFillingOnShip = new();
    }
    public class Entry { public bool Value = true; }
    public class NumberEntry { public float Value; }
    public class ModeEntry
    {
        public Configuration.OxygenConfig.AutoFillingOnShip Value = Configuration.OxygenConfig.AutoFillingOnShip.WhenDoorsClosed;
    }
}
namespace Oxygen.Configuration
{
    public static class OxygenConfig
    {
        public enum AutoFillingOnShip { Off, WhenDoorsClosed, WhenPlayerOnShip }
        public static BepInEx.Logging.ManualLogSource mls = new();
    }
}
public class GameNetworkManager
{
    public static GameNetworkManager Instance = new();
    public Player localPlayerController = new();
}
public class Player { public bool isPlayerDead; public ulong actualClientId; }
namespace Oxygen.General
{
    public static class OxygenInit { public static UnityEngine.GameObject oxyCharger; public static float Percent; }
    public static class VanillaCanisters
    {
        public static bool Visible;
        public static void SetVisible(bool visible) => Visible = visible;
    }
}
