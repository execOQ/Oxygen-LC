using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.GameObjects;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class KillPlayerPatch
    {
        private readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > KillPlayerPatch");

        [HarmonyPostfix]
        [HarmonyPatch("KillPlayer")]
        public static void KillPlayer_patch(PlayerControllerB __instance)
        {
            if (__instance == null) return;

            if (__instance.isPlayerDead)
            {
                OxygenInit.Percent = 1;
                __instance.drunkness = 0;
                mls.LogInfo("Player probably is dead (lol), oxygen was recovered to 1");

                // resets notifications
                OxygenLogic.breathablePlace_Notification = false;
                OxygenLogic.lowLevel_Notification = false;
                OxygenLogic.criticalLevel_Notification = false;
                OxygenLogic.immersiveVisor_Notification = false;
            }
        }
    }
}
