using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Extras;
using Oxygen.General;
using UnityEngine;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > PlayerControllerBPatch");

        [HarmonyPostfix]
        [HarmonyPatch("ConnectClientToPlayerObject")]
        [HarmonyPriority(Priority.Last)]
        public static void ConnectClientToPlayerObject_Postfix(PlayerControllerB __instance)
        {
            AudioController.Init_AudioSource(__instance.playersManager.thisClientPlayerId);

            // TooManyEmotes mod copies GameObject with ControlTips and because of it DieEarly object is also copied, which causes it to show while dancing
            GameObject endGameEarlyUI = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/ThirdPersonEmotesControlTips/DieEarly/");
            if (endGameEarlyUI != null)
            {
                mls.LogDebug("DieEarly object was copied, destroying it.");
                Destroy(endGameEarlyUI);
            }
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
