using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using Oxygen.GameObjects;
using Oxygen.Extras;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > HUDPatch");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Start")]
        public static void BuildHUD()
        {
            mls.LogInfo("Initializing HUD");
            OxygenInit.Init();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void AddAudioSource(PlayerControllerB __instance)
        {
            AudioController.Init_AudioSource(__instance.playersManager.thisClientPlayerId);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Update")]
        public static void UpdatePatch()
        {
            OxygenLogic.RunLogic();
        }

        /* private static IEnumerator AwaitPlayerController()
        {
            yield return new WaitUntil(() => (Object)(object)GameNetworkManager.Instance.localPlayerController != null);
        } */
    }
}
