using BepInEx.Logging;
using HarmonyLib;
using Oxygen.General;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > StartOfRoundPatch");

        [HarmonyPrefix]
        [HarmonyPatch("SpawnUnlockable")]
        private static bool BeforeSpawn(StartOfRound __instance, int unlockableIndex)
            => ChargerUnlockable.BeforeSpawn(__instance, unlockableIndex);

        [HarmonyPrefix]
        [HarmonyPatch("ReturnUnlockableFromStorageServerRpc")]
        private static bool BeforeReturnFromStorage(StartOfRound __instance, int unlockableID)
            => ChargerUnlockable.CanReturnFromStorage(__instance, unlockableID);

        [HarmonyPostfix]
        [HarmonyPatch("ResetShipFurniture")]
        private static void AfterFurnitureReset() => ChargerUnlockable.ResetFurniture();

        [HarmonyPostfix]
        [HarmonyPatch("ShipHasLeft")]
        private static void ShipHasLeft_Patch()
        {
            if (OxygenBase.OxygenConfig.recoverOxygen_ShipLeft.Value)
            {
                OxygenInit.Percent = 1f;
                DieEarly.DisplayDieEarlyMeter(false);
                mls.LogInfo("Ship has left, oxygen was recovered.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OpenShipDoors")]
        private static void OpenShipDoors_PostFix()
        {
            if (OxygenBase.OxygenConfig.recoverOxygen_StartOfRound.Value)
            {
                OxygenInit.Percent = 1f;
                mls.LogInfo("Round just started, oxygen was recovered.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Patch_RoundStart(StartOfRound __instance)
        {
            ChargerUnlockable.StartRound(__instance);
            OxygenBase.OxygenConfig.RefreshEffects();

            // for some reasons it's not working.
            // removing OxyCanister from the "Oops! All Flooded" mod cuz it's not working with the Oxygen mod whose overwriting the "drowningTimer" variable
            /* if (OxygenBase.Instance.IsOopsAllFloodedFound)
            {
                foreach (LL.Items.ShopItem item in LL.Items.shopItems)
                {
                    mls.LogDebug($"Mod name: {item.modName} | GameObject name: {item.item.name} | Item name: {item.item.itemName}");
                    if (item != null)
                    {
                        mls.LogInfo("1");
                        if (item.item.itemName == "Oxy-Canister")
                        {
                            mls.LogInfo("2");
                            LL.Items.RemoveShopItem(item.item);
                            mls.LogInfo("3");
                            if (item.wasRemoved)
                            {
                                mls.LogInfo("Deleted Oxy-canister from the 'Oops! All Flooded' mod.");
                            }
                            break;
                        }
                    }
                }
            } */

            /* 
            RoundManager roundManager = Object.FindFirstObjectByType<RoundManager>();

            if (roundManager != null)
            {
                string msg2 = "\nUse these dungeons names in the config:\n\n";
                foreach (IndoorMapType imp in roundManager.dungeonFlowTypes)
                {
                    msg2 += imp.dungeonFlow.name + "\n";
                }

                mls.LogInfo(msg2);
            }
            else mls.LogError("roundmanager is null");
            */
        }
    }
}
