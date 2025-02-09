using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Extras;
using Oxygen.General;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > PlayerControllerBPatch");

        [HarmonyPostfix]
        [HarmonyPatch("ConnectClientToPlayerObject")]
        public static void ConnectClientToPlayerObject_Postfix(PlayerControllerB __instance)
        {
            AudioController.Init_AudioSource(__instance.playersManager.thisClientPlayerId);
        }

        [HarmonyPostfix]
        [HarmonyPatch("KillPlayer")]
        private static void KillPlayer_Postfix(PlayerControllerB __instance)
        {
            if (__instance == null) return;

            if (__instance.isPlayerDead)
            {
                OxygenInit.Percent = 1;
                __instance.drunkness = 0;
                mls.LogInfo("Player is dead. Oxygen level was recovered");

                OxygenLogic.ResetAllNotifications();
                DieEarly.DisplayDieEarlyMeter(false);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        private static void LateUpdate_Prefix(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && (!__instance.IsServer || __instance.isHostPlayerObject))
            {
                if (!__instance.isPlayerDead)
                {
                    OxygenLogic.ShowNotifications(__instance);
                }
            }
        }
    }
}
