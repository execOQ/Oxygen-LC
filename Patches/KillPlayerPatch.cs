using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class KillPlayerPatch : NetworkBehaviour
    {
        private static PlayerControllerB pcB = GameNetworkManager.Instance.localPlayerController;

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayer_patch()
        {
            if (pcB == null)
            {
                return;
            }

            if (pcB.isPlayerDead)
            {
                HUDPatch.oxygenUI.fillAmount = 1;
                pcB.drunkness = 0;
                HUDPatch.mls.LogInfo("Player is dead, oxygen recovered to 1");

                // resets notifications
                HUDPatch.backroomsNotification = false;
                HUDPatch.firstNotification = false;
                HUDPatch.warningNotification = false;
            }
        }
    }
}
