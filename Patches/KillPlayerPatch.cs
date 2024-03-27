using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.GameObjects;
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
                OxygenLogic.breathablePlace_Notification = false;
                OxygenLogic.lowLevel_Notification = false;
                OxygenLogic.criticalLevel_Notification = false;
                OxygenLogic.immersiveVisor_Notification = false;
            }
        }
    }
}
