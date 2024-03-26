using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class KillPlayerPatch
    {
        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > KillPlayerPatch");

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayer_patch(ref PlayerControllerB __instance)
        {
            if (__instance == null) return;
            
            if (__instance.isPlayerDead)
            {
                OxygenHUD.oxygenUI.fillAmount = 1;
                __instance.drunkness = 0;

                mls.LogInfo("Player is dead, oxygen recovered to 1");

                // resets notifications
                OxygenHUD.backroomsNotification = false;
                OxygenHUD.firstNotification = false;
                OxygenHUD.warningNotification = false;
            }
        }
    }
}
