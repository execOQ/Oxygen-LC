using BepInEx.Logging;
using HarmonyLib;
using Oxygen.General;
using System.Reflection;
using UnityEngine;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(ShipBuildModeManager))]
    internal static class ShipBuildModeManagerPatch
    {
        private static readonly ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > ShipBuildModeManagerPatch");
        private static readonly FieldInfo placingObjectField = AccessTools.Field(typeof(ShipBuildModeManager), "placingObject");

        [HarmonyPostfix]
        [HarmonyPatch("CreateGhostObjectAndHighlight")]
        private static void AfterCreateGhost(ShipBuildModeManager __instance)
        {
            var placeable = placingObjectField?.GetValue(__instance) as PlaceableShipObject;
            if (placeable == null || placeable.parentObject == null ||
                placeable.parentObject.gameObject != OxygenInit.oxyCharger) return;
            ChargerUnlockable.SetPlacementHeight(placeable, __instance.ghostObject.rotation);
        }

        [HarmonyPrefix]
        [HarmonyPatch("PlayerMeetsConditionsToBuild")]
        private static bool PlayerMeetsConditionsToBuild_Prefix(ShipBuildModeManager __instance, ref bool __result, bool log = true)
        {
            if (!__instance.InBuildMode)
            {
                return true;
            }

            PlaceableShipObject placingObject = placingObjectField?.GetValue(__instance) as PlaceableShipObject;
            if (placingObject == null)
            {
                return true;
            }

            int unlockableId = placingObject.unlockableID;
            int unlockablesCount = StartOfRound.Instance?.unlockablesList?.unlockables?.Count ?? -1;

            if (unlockableId < 0 || unlockablesCount < 0 || unlockableId >= unlockablesCount)
            {
                mls.LogWarning($"Cancelling build mode for invalid unlockableID. Object='{placingObject.name}', unlockableID={unlockableId}, unlockablesCount={unlockablesCount}");
                __instance.CancelBuildMode();
                __result = false;
                return false;
            }

            return true;
        }
    }
}
