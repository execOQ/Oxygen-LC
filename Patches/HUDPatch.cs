using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using Oxygen.GameObjects;
using Discord;
using UnityEngine.Rendering;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > HUDPatch");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Awake")]
        public static void BuildHUD(HUDManager __instance)
        {
            __instance.StartCoroutine(AwaitPlayerController());

            OxygenHUD.Init();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void AddAudioSource(PlayerControllerB __instance)
        {
            OxygenHUD.Init_AudioSource(__instance.playersManager.thisClientPlayerId);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Update")]
        public static void UpdatePatch()
        {
            if (!OxygenHUD.initialized)
            {
                mls.LogWarning("OxygenHUD is still initializing");
                return;
            }

            PlayerControllerB pc = GameNetworkManager.Instance.localPlayerController;
            StartOfRound sor = StartOfRound.Instance;

            if (pc == null)
            {
                mls.LogError("PlayerControllerB is null");
                return;
            }

            if (sor == null)
            {
                mls.LogError("StartOfRound is null");
                return;
            }

            if (OxygenHUD.oxygenUI == null)
            {
                mls.LogError("oxygenUI is null");
                return;
            }

            if (pc.isPlayerDead) return;

            OxygenHUD.UpdateModsCompatibility();
            OxygenHUD.ShowNotifications();

            OxygenLogic.RunLogic(sor, pc);
        }

        private static IEnumerator AwaitPlayerController()
        {
            yield return new WaitUntil(() => (Object)(object)GameNetworkManager.Instance.localPlayerController != null);
        }
    }
}
