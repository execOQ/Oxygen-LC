using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.GameObjects;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void LateUpdate_Prefix(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && (!__instance.IsServer || __instance.isHostPlayerObject))
            {
                if (!__instance.isPlayerDead)
                {
                    OxygenInit.UpdateModsCompatibility();
                    OxygenLogic.ShowNotifications();
                }
            }
        }
    }
}
