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
        public static void KillPlayer_patch(ref PlayerControllerB __instance)
        {
            if (__instance == null) return;

            if (__instance.isPlayerDead)
            {
                OxygenHUD.oxygenUI.fillAmount = 1;
                __instance.drunkness = 0;
                OxygenHUD.mls.LogInfo("Player is dead, oxygen recovered to 1");

                // resets notifications
                HUDPatch.backroomsNotification = false;
                HUDPatch.firstNotification = false;
                HUDPatch.warningNotification = false;
            }
        }
    }
}
