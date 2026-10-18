using LethalLib.Modules;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.General
{
    internal static class ChargerUnlockable
    {
        private static UnlockableItem item;
        private static TerminalNode storageNode;
        private static bool ready;

        internal static void Register(GameObject prefab)
        {
            if (prefab == null) return;
            item = new UnlockableItem
            {
                unlockableName = "OxyCharger",
                prefabObject = prefab,
                unlockableType = 1,
                alreadyUnlocked = true,
                IsPlaceable = true,
                canBeStored = true,
                maxNumber = 1,
                spawnPrefab = true
            };
            // Register unconditionally so host and clients have the same list and prefab IDs.
            Unlockables.RegisterUnlockable(item, 0, StoreType.None);
            storageNode = ScriptableObject.CreateInstance<TerminalNode>();
            storageNode.clearPreviousText = true;
            storageNode.displayText = "OxyCharger. Use STORAGE to view stored ship furniture.\n";
            storageNode.returnFromStorage = true;
            storageNode.shipUnlockableID = -1;
            TerminalUtils.CreateTerminalKeyword("oxycharger", storageNode);
        }

        internal static void Reset()
        {
            ready = false;
            if (item == null) return;
            item.inStorage = false;
            item.hasBeenMoved = false;
            item.hasBeenUnlockedByPlayer = false;
            item.placedPosition = Vector3.zero;
            item.placedRotation = Vector3.zero;
        }

        internal static void StartRound(StartOfRound round)
        {
            Prepare(round);
            ready = true;
        }

        internal static void ResetFurniture()
        {
            // Native resets clear this unlockable's state, but only reposition built-in scene props.
            var charger = OxygenInit.oxyCharger;
            if (charger != null)
                foreach (var parent in charger.GetComponentsInChildren<AutoParentToShip>(true))
                {
                    parent.positionOffset = parent.startingPosition;
                    parent.rotationOffset = parent.startingRotation;
                }
            Refresh();
        }

        internal static int Prepare(StartOfRound round)
        {
            if (item == null || round == null || round.unlockablesList == null) return -1;
            int id = round.unlockablesList.unlockables.IndexOf(item);
            if (id < 0) return -1;
            storageNode.shipUnlockableID = id;
            var prefab = item.prefabObject;
            prefab.transform.localScale = Vector3.one * 0.2077057f;
            foreach (var placeable in prefab.GetComponentsInChildren<PlaceableShipObject>(true))
            {
                placeable.unlockableID = id;
                NormalizePlacement(placeable);
            }
            foreach (var parent in prefab.GetComponentsInChildren<AutoParentToShip>(true))
            {
                parent.unlockableID = id;
                parent.overrideOffset = true;
                var suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
                var cylinders = suitParts != null ? suitParts.transform.Find("Circle.002") : null;
                if (cylinders != null)
                {
                    // Pull the default charger 30 cm away from the rear wall into the ship.
                    parent.positionOffset = round.elevatorTransform.InverseTransformPoint(cylinders.position)
                        + new Vector3(0f, 0f, -0.3f);
                    parent.rotationOffset = round.elevatorTransform.InverseTransformDirection(cylinders.eulerAngles);
                }
            }
            return id;
        }

        internal static void NormalizePlacement(PlaceableShipObject placeable)
        {
            if (!(placeable.placeObjectCollider is BoxCollider box)) return;
            // Build mode uses box.size directly for its world-space overlap check and
            // ignores both collider.center and transform scale. Keep the physical box
            // unchanged, but express it as a unit-scale, centered selection volume.
            var transform = box.transform;
            var scale = transform.lossyScale;
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) return;
            var center = transform.TransformPoint(box.center);
            var size = Vector3.Scale(box.size, scale);
            transform.localScale = Vector3.Scale(transform.localScale,
                new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z));
            transform.position = center;
            box.center = Vector3.zero;
            box.size = size;
            SetPlacementHeight(placeable, Quaternion.identity);
        }

        internal static void SetPlacementHeight(PlaceableShipObject placeable, Quaternion rotation)
        {
            if (!(placeable.placeObjectCollider is BoxCollider box)) return;
            // The charger inherits the canisters' tilt. Local Y is not necessarily
            // vertical, so lift the ghost by the rotated box's world-space half-height.
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;
            placeable.yOffset = (Mathf.Abs(right.y) * box.size.x +
                Mathf.Abs(up.y) * box.size.y + Mathf.Abs(forward.y) * box.size.z) * 0.5f + 0.01f;
        }

        internal static bool CanSpawn(StartOfRound round, int id)
        {
            if (item == null || round.unlockablesList.unlockables[id] != item) return true;
            Prepare(round);
            return OxygenBase.OxygenConfig.oxyCharger_Enabled.Value &&
                (!round.SpawnedShipUnlockables.TryGetValue(id, out var existing) || existing == null);
        }

        internal static bool CanReturnFromStorage(StartOfRound round, int id)
        {
            // Clients still send the request; the host decides before the native RPC clears inStorage.
            return !round.IsServer || round.unlockablesList.unlockables[id] != item ||
                OxygenBase.OxygenConfig.oxyCharger_Enabled.Value;
        }

        internal static bool BeforeSpawn(StartOfRound round, int id)
        {
            if (!CanSpawn(round, id)) return false;
            if (round.unlockablesList.unlockables[id] != item || !ready ||
                (!item.hasBeenMoved && item.placedPosition == Vector3.zero)) return true;

            // Native SpawnUnlockable normally reloads placement from disk. Recreated furniture
            // must use its current placement, including unsaved moves and zero Euler rotation.
            var position = item.placedPosition;
            var rotation = item.placedRotation;
            var spawned = Object.Instantiate(item.prefabObject, round.elevatorTransform.position, Quaternion.identity);
            var networkObject = spawned.GetComponent<NetworkObject>();
            try
            {
                ShipBuildModeManager.Instance.PlaceShipObject(position, rotation,
                    spawned.GetComponentInChildren<PlaceableShipObject>(), placementSFX: false);
                spawned.GetComponentInChildren<AutoParentToShip>().MoveToOffset();
                networkObject.Spawn();
                round.SpawnedShipUnlockables.Add(id, spawned);
            }
            catch
            {
                if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
                else Object.Destroy(spawned);
                throw;
            }
            return false;
        }

        internal static void Refresh()
        {
            var round = StartOfRound.Instance;
            var manager = NetworkManager.Singleton;
            if (!ready || round == null || manager == null || !manager.IsServer || !manager.IsListening) return;
            int id = Prepare(round);
            if (id < 0) return;
            if (!OxygenBase.OxygenConfig.oxyCharger_Enabled.Value)
            {
                if (round.SpawnedShipUnlockables.TryGetValue(id, out var existing) && existing != null)
                {
                    round.SpawnedShipUnlockables.Remove(id);
                    var networkObject = existing.GetComponent<NetworkObject>();
                    if (networkObject != null && networkObject.IsSpawned) networkObject.Despawn();
                    else Object.Destroy(existing);
                }
                VanillaCanisters.SetVisible(true);
                return;
            }
            // Stored furniture is returned through the game's terminal RPC, not a config refresh.
            if (!item.inStorage &&
                (!round.SpawnedShipUnlockables.TryGetValue(id, out var charger) || charger == null))
            {
                round.SpawnedShipUnlockables.Remove(id);
                round.SpawnUnlockable(id);
            }
        }
    }
}
