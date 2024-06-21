using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Items;
using UnityEngine;
using LL = LethalLib.Modules;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class StartOfRoundPatch : MonoBehaviour
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > StartOfRoundPatch");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        private static void ShipHasLeft_Patch()
        {
            if (OxygenBase.OxygenConfig.recoverOxygenOnceShipLeft.Value)
            {
                OxygenInit.Percent = 1f;
                mls.LogInfo("Ship has left, oxygen was recovered.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        private static void Patch_RoundAwake()
        {
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

            OxyCharger.Init();
        }
    }
}
