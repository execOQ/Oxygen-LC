using System;
using LethalLib.Modules;
using Oxygen;
using Oxygen.General;
using Oxygen.Items;
using Unity.Netcode;
using UnityEngine;

static class Program
{
    static int checks;
    static void Assert(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }

    static void BuildModeGeometryRegressions()
    {
        // Values from the shipped charger prefab. Native build mode checks size
        // without multiplying by lossyScale and centers its ghost on the transform.
        var root = new Transform { localScale = Vector3.one * 0.2077057f };
        var box = new BoxCollider
        {
            transform = new Transform { parent = root, position = new(6, 0.66f, -11) },
            size = new(3.555636f, 1.856283f, 4.471804f),
            center = new(0.009632652f, 0.9384232f, 0.02547197f)
        };
        var placeable = new PlaceableShipObject { placeObjectCollider = box, yOffset = 0.86f };
        var previousCenter = box.transform.TransformPoint(box.center);
        var previousSize = Vector3.Scale(box.size, box.transform.lossyScale);
        ChargerUnlockable.NormalizePlacement(placeable);
        Assert(Math.Abs(box.size.x - 0.7385259f) < 0.00001f &&
            Math.Abs(box.size.y - 0.385559f) < 0.00001f &&
            Math.Abs(box.size.z - 0.928819f) < 0.00001f,
            "Native build-mode dimensions must match the small charger, not the unscaled source mesh.");
        Assert(box.center == Vector3.zero && box.transform.position == previousCenter,
            "The ghost origin must coincide with the physical selection-box center.");
        Assert(Vector3.Scale(box.size, box.transform.lossyScale) == previousSize,
            "Normalizing build mode must preserve the physical selection box.");
        Assert(Math.Abs(placeable.yOffset - 0.2027795f) < 0.00001f,
            "Floor placement must use half the actual charger height plus clearance.");
        ChargerUnlockable.SetPlacementHeight(placeable, Quaternion.Euler(new(90, 0, 0)));
        Assert(Math.Abs(placeable.yOffset - 0.4744095f) < 0.00001f,
            "Standing canisters must clear the floor along rotated local Z, not local Y.");
        Assert(placeable.yOffset - previousSize.z * 0.5f >= 0.00999f,
            "The full rotated charger must rest above the floor instead of intersecting it.");
        ChargerUnlockable.SetPlacementHeight(placeable, Quaternion.Euler(new(90, 135, 0)));
        Assert(Math.Abs(placeable.yOffset - 0.4744095f) < 0.00001f,
            "Rotating a standing charger around the vertical axis must preserve floor clearance.");
        // Actual default orientation captured from the live game's red preview.
        var liveRotation = Quaternion.Euler(new(302.086f, 351.514f, 187.202f));
        ChargerUnlockable.SetPlacementHeight(placeable, liveRotation);
        Assert(Math.Abs(placeable.yOffset - 0.5296577f) < 0.00002f,
            "The live canisters' oblique rotation must clear ShipInside's floor mesh.");
        foreach (int x in new[] { -1, 1 })
            foreach (int y in new[] { -1, 1 })
                foreach (int z in new[] { -1, 1 })
                {
                    var corner = liveRotation * new Vector3(box.size.x * x * 0.5f,
                        box.size.y * y * 0.5f, box.size.z * z * 0.5f);
                    Assert(corner.y + placeable.yOffset >= 0.00999f,
                        "Every corner of the tilted placement box must stay above the floor.");
                }
        // Native CheckBox uses size * 0.5 * 0.57: a charger must fit a 1m gap.
        Assert(box.size.x * 0.57f < 1f && box.size.z * 0.57f < 1f,
            "Placement in a one-metre ship gap must not be blocked by a four-metre source collider.");
        ChargerUnlockable.NormalizePlacement(placeable);
        Assert(box.transform.position == previousCenter &&
            Math.Abs(box.size.z - previousSize.z) < 0.00001f,
            "Repeated host edits and session preparation must not shrink or shift the selection volume.");
        ChargerUnlockable.NormalizePlacement(new PlaceableShipObject());
        Assert(true, "Missing placement collider must not prevent lifecycle cleanup.");
    }

    static StartOfRound Round(int originalCount)
    {
        var round = new StartOfRound();
        for (int i = 0; i < originalCount; i++) round.unlockablesList.unlockables.Add(new());
        // Simulate LethalLib's unconditional Terminal.Awake registration on each peer.
        round.unlockablesList.unlockables.Add(Unlockables.Registered);
        StartOfRound.Instance = round;
        ChargerUnlockable.Prepare(round);
        return round;
    }

    static void PlacementStorageRegressions()
    {
        ChargerUnlockable.Reset();
        WeatherHandler.Reset();
        NetworkManager.Singleton.IsServer = true;
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        OxygenBase.OxygenConfig.eclipsed_LimitOxygen.Value = true;
        OxygenBase.OxygenConfig.eclipsed_LimitedOxygenAmount.Value = 10;
        var host = Round(40);
        host.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
        host.DiskPosition = new() { x = 11, y = 2, z = -5 };
        host.DiskRotation = new() { x = 45 };
        ChargerUnlockable.StartRound(host);
        ChargerUnlockable.Refresh();
        Assert(host.LastPlacedPosition == 11, "Initial spawn must still load disk placement.");
        var item = Unlockables.Registered;
        var live = host.SpawnedShipUnlockables[40];
        var unsaved = new Vector3 { x = 31, y = 4, z = -2 };
        ShipBuildModeManager.Instance.PlaceShipObject(unsaved, Vector3.zero, live.GetComponent<PlaceableShipObject>());
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 10, "Initial reserve was not configured.");
        OxyCharger.Instance.RemainedOxygenAmount = 3.25f;
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.Refresh();
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        ChargerUnlockable.Refresh();
        Assert(host.PositionAtSpawn == unsaved && host.LastPlacedRotation == Vector3.zero,
            "Re-enable must place the unsaved position and zero rotation before network spawn.");
        Assert(item.placedPosition == unsaved && item.placedRotation == Vector3.zero && item.hasBeenMoved,
            "Re-enable overwrote current placement with stale disk data.");
        Assert(host.DiskPosition.Value.x == 11 && host.DiskRotation.x == 45,
            "Recreating the charger must not rewrite the save file.");
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 3.25f, "Config toggle replenished the remaining reserve.");

        // Store through the same callbacks as the game, with IsSpawned already false on despawn.
        host.Store(40);
        Assert(item.inStorage && OxyCharger.Instance == null, "Native storage did not remove the physical charger.");
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.Refresh();
        int spawns = host.Spawns, broadcasts = host.ReturnBroadcasts;
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(item.inStorage && host.Spawns == spawns && host.ReturnBroadcasts == broadcasts,
            "Disabled host return must preserve storage and send no success broadcast.");
        Assert(ChargerUnlockable.CanReturnFromStorage(host, 0), "Disabled charger must not block other furniture returns.");
        NetworkManager.Singleton.IsServer = false;
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(host.ReturnRequests == 1 && item.inStorage, "Client must defer storage return authority to the host.");
        NetworkManager.Singleton.IsServer = true;
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(item.inStorage && host.ReturnBroadcasts == broadcasts, "Host accepted a disabled client's return request.");
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == spawns && item.inStorage, "Re-enable must leave a rejected return in storage.");
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(!item.inStorage && host.ReturnBroadcasts == broadcasts + 1, "Enabled native storage return failed.");
        Assert(host.PositionAtSpawn == unsaved && host.LastPlacedRotation == Vector3.zero,
            "Storage return lost an unsaved placement or zero rotation.");
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 3.25f, "Storage return replenished the consumed reserve.");
        for (int cycle = 0; cycle < 3; cycle++)
        {
            OxyCharger.Instance.RemainedOxygenAmount -= 0.25f;
            host.Store(40);
            ChargerUnlockable.Refresh();
            host.ReturnUnlockableFromStorageServerRpc(40);
            Assert(OxyCharger.Instance.RemainedOxygenAmount == 3 - cycle * 0.25f,
                "Repeated storage/return replenished the reserve.");
        }
        OxyCharger.Instance.RemainedOxygenAmount = 0;
        host.Store(40);
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 0, "Depleted reserve must remain empty through storage.");

        host.Store(40);
        WeatherHandler.UpdateWeatherType(host.currentLevel);
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 10, "New round while stored must reset the reserve.");
        OxyCharger.Instance.RemainedOxygenAmount = 2;
        host.Store(40);
        OxygenBase.OxygenConfig.eclipsed_LimitedOxygenAmount.Value = 20;
        WeatherHandler.RefreshLimit();
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 20, "Capacity edit while stored must apply on return.");

        OxyCharger.Instance.RemainedOxygenAmount = 4.5f;
        host.Store(40);
        NetworkManager.Singleton.IsServer = false;
        var clientReplica = UnityEngine.Object.Instantiate(item.prefabObject, Vector3.zero, Quaternion.identity);
        clientReplica.GetComponent<NetworkObject>().Spawn();
        clientReplica.GetComponent<NetworkObject>().Despawn();
        NetworkManager.Singleton.IsServer = true;
        host.ReturnUnlockableFromStorageServerRpc(40);
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 4.5f,
            "Client spawn/despawn callbacks must not overwrite the server's carried reserve.");

        host.Store(40);
        WeatherHandler.Reset();
        ChargerUnlockable.Reset();
        var next = Round(45);
        next.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
        ChargerUnlockable.StartRound(next);
        ChargerUnlockable.Refresh();
        Assert(OxyCharger.Instance.RemainedOxygenAmount == 20, "New host inherited a previous host's carried reserve.");
    }

    static void Main()
    {
        BuildModeGeometryRegressions();
        NetworkManager.Singleton = new() { IsServer = true };
        var prefab = new GameObject();
        var placeable = new PlaceableShipObject { unlockableID = 34 };
        var parent = new AutoParentToShip { unlockableID = 34 };
        prefab.Components.Add(placeable);
        prefab.Components.Add(parent);
        GameObject.SuitParts = new();
        GameObject.Cylinders = new();
        GameObject.Cylinders.transform.position = new() { x = 12 };
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.Register(prefab);
        var item = Unlockables.Registered;
        Assert(Unlockables.Registrations == 1, "Disabled charger must still register on every peer.");
        Assert(item.alreadyUnlocked && item.IsPlaceable && item.spawnPrefab && item.canBeStored,
            "Charger must use the game's furniture save/storage path.");
        Assert(item.prefabObject == prefab && item.maxNumber == 1, "Register the correct single furniture prefab.");

        var host = Round(40);
        Assert(placeable.unlockableID == 40 && parent.unlockableID == 40 && TerminalUtils.Node.shipUnlockableID == 40,
            "Both components and terminal must share the registered ID.");
        Assert(parent.overrideOffset && parent.positionOffset.x == 12 &&
            Math.Abs(parent.positionOffset.z + 0.3f) < 0.00001f,
            "Default placement must follow ship coordinates with clearance from the rear wall.");
        Assert(Math.Abs(prefab.transform.localScale.x - 0.2077057f) < 0.00001f, "Preserve charger scale.");
        Assert(TerminalUtils.Node.returnFromStorage, "Terminal keyword must return stored furniture.");
        Assert(ChargerUnlockable.CanSpawn(host, 0), "Do not affect other furniture.");
        Assert(!ChargerUnlockable.CanSpawn(host, 40), "Block native charger spawns while disabled.");

        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 0, "Wait for native save loading before initial spawn.");
        item.inStorage = true; // Loaded by the native save routine before Start postfix.
        ChargerUnlockable.StartRound(host);
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 0, "Initial refresh must respect a saved stored charger.");
        Assert(ChargerUnlockable.CanSpawn(host, 40), "Native return-from-storage spawn must be allowed.");
        item.inStorage = false;
        item.placedPosition = new() { x = 25 };
        item.hasBeenMoved = true;
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 1 && host.LastPlacedPosition == 25, "Spawn through native saved placement.");
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 1 && !ChargerUnlockable.CanSpawn(host, 40), "Prevent duplicate spawns.");

        var firstObject = host.SpawnedShipUnlockables[40];
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.Refresh();
        Assert(firstObject.GetComponent<NetworkObject>().Despawns == 1 && host.SpawnedShipUnlockables.Count == 0,
            "Disabling must remove native tracking and network-despawn once.");
        Assert(VanillaCanisters.Visible, "Disabling must restore vanilla canisters.");
        ChargerUnlockable.Refresh();
        Assert(firstObject.GetComponent<NetworkObject>().Despawns == 1, "Repeated disable must be harmless.");
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 2 && host.LastPlacedPosition == 25, "Re-enable must restore moved placement.");

        // Simulate native storage: mark stored, despawn and remove the dictionary entry.
        item.inStorage = true;
        host.SpawnedShipUnlockables.Remove(40);
        ChargerUnlockable.Refresh();
        Assert(host.Spawns == 2, "Unrelated host edits must not respawn stored furniture.");
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.Refresh();
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        ChargerUnlockable.Refresh();
        Assert(item.inStorage && host.Spawns == 2, "Disable/re-enable must preserve storage state.");

        NetworkManager.Singleton.IsServer = false;
        var lateClient = Round(40);
        ChargerUnlockable.StartRound(lateClient);
        ChargerUnlockable.Refresh();
        Assert(placeable.unlockableID == 40 && parent.unlockableID == 40, "Late client prefab IDs must match host.");
        Assert(lateClient.Spawns == 0, "Clients must wait for replicated furniture.");
        ChargerUnlockable.Reset();
        Assert(!item.inStorage && !item.hasBeenMoved && item.placedPosition.x == 0 && item.placedRotation.x == 0,
            "A different host must not inherit storage or placement.");
        var nextHost = Round(45);
        Assert(placeable.unlockableID == 45 && parent.unlockableID == 45 && TerminalUtils.Node.shipUnlockableID == 45,
            "Resolve current list IDs again when reconnecting.");
        NetworkManager.Singleton.IsServer = true;
        ChargerUnlockable.Refresh();
        Assert(nextHost.Spawns == 0, "Session reset must clear readiness.");
        ChargerUnlockable.StartRound(nextHost);
        ChargerUnlockable.Refresh();
        Assert(nextHost.Spawns == 1 && nextHost.LastPlacedPosition == 0, "New host must use its own default placement.");
        var liveParent = new AutoParentToShip
        {
            startingPosition = new() { x = 12 }, startingRotation = new() { x = 90 },
            positionOffset = new() { x = 25 }, rotationOffset = new() { x = 180 }
        };
        OxygenInit.oxyCharger = nextHost.SpawnedShipUnlockables[45];
        OxygenInit.oxyCharger.Components.Add(liveParent);
        ChargerUnlockable.ResetFurniture();
        Assert(liveParent.positionOffset.x == 12 && liveParent.rotationOffset.x == 90,
            "Native furniture reset must also reposition the default charger.");
        nextHost.SpawnedShipUnlockables.Clear();
        OxygenInit.oxyCharger = null;
        item.inStorage = false; // Native reset clears storage before our postfix.
        ChargerUnlockable.ResetFurniture();
        Assert(nextHost.Spawns == 2, "Native furniture reset must restore a previously stored default charger.");
        PlacementStorageRegressions();
        checks += ConcurrentRefillChecks.Run();
        Console.WriteLine($"Passed {checks} charger lifecycle checks.");
    }
}
