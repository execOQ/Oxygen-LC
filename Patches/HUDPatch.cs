using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using Oxygen.GameObjects;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch
    {
        public static AudioClip[] inhaleSFX = OxygenBase.Instance.inhaleSFX;

        public static TextMeshProUGUI EladsOxygenUIText => OxygenHUD.EladsOxygenUIText;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Awake")]
        public static void BuildHUD(HUDManager __instance)
        {
            __instance.StartCoroutine(AwaitPlayerController());

            OxygenHUD.Init();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Update")]
        public static void UpdatePatch()
        {
            if (!OxygenHUD.initialized)
            {
                mls.LogError("OxygenHUD is still instantiating");
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

            if (inhaleSFX == null)
            {
                mls.LogError("inhalerSFX is null");
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
