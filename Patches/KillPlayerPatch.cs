using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class KillPlayerPatch
    {
        //private static PlayerControllerB pcB = GameNetworkManager.Instance.localPlayerController;

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayer_patch(PlayerControllerB __instance)
        {
            if (__instance == null) return;

            if (__instance.isPlayerDead)
            {
                OxygenInit.oxygenUI.fillAmount = 1;
                __instance.drunkness = 0;
                OxygenInit.mls.LogInfo("Player is dead, oxygen recovered to 1");

                // resets notifications
                OxygenInit.backroomsNotification = false;
                OxygenInit.firstNotification = false;
                OxygenInit.warningNotification = false;
                OxygenInit.ImmersiveVisorNotification = false;
            }
        }
    }
}
