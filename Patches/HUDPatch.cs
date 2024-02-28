using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Configuration;
using Oxygen.Utils;
using static System.Math;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch : MonoBehaviour
    {
        public static AudioClip[] inhaleSFX = OxygenBase.Instance.inhaleSFX;

        public static Image OxygenUI => OxygenHUD.oxygenUI;

        public static TextMeshProUGUI EladsOxygenUIText => OxygenHUD.EladsOxygenUIText;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        private const float offset = -400f;

        // syncing with host
        public static bool InfinityOxygenInModsPlaces => Config.Instance.InfinityOxygenInModsPlaces.Value;

        public static int playerDamage => Config.Instance.playerDamage.Value;

        public static float increasingOxygen => Config.Instance.increasingOxygen.Value;
        public static float decreasingOxygen => Config.Instance.decreasingOxygen.Value;
        public static float multiplyDecreasingInFear => Config.Instance.multiplyDecreasingInFear.Value;

        public static float oxygenDepletionWhileRunning => Config.Instance.oxygenRunning.Value;
        public static float oxygenDepletionInWater => Config.Instance.oxygenDepletionInWater.Value;

        public static float oxygenDeficiency => Config.Instance.oxygenDeficiency.Value;

        public static float secTimer => Config.Instance.secTimer.Value;  // number of seconds the cool down timer lasts
        //

        public static bool enableOxygenSFX => OxygenBase.Config.enableOxygenSFX.Value;
        public static bool enableOxygenSFXInShip => OxygenBase.Config.enableOxygenSFXInShip.Value;

        public static float secTimerInFear = 2f;

        private static float timeSinceLastAction = 0f;  //number of seconds since we did something
        private static float timeSinceLastFear = 0f;  //number of seconds since we were fear

        public static bool isNotification => OxygenBase.Config.notifications.Value;

        internal static bool backroomsNotification = false;
        internal static bool firstNotification = false;
        internal static bool warningNotification = false;

        [HarmonyPostfix]
        //[HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        public static void BuildHUD(HUDManager __instance)
        {
            __instance.StartCoroutine(AwaitPlayerController());

            OxygenHUD.Init();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        public static void LateUpdatePatch(ref PlayerControllerB __instance)
        {
            if (!OxygenHUD.initialized) return;

            if (OxygenBase.Instance.isShyHUDFound && Config.Instance.ShyHUDSupport)
            {
                if (OxygenUI.fillAmount >= 0.55f)
                {
                    OxygenUI.CrossFadeAlpha(0f, 5f, ignoreTimeScale: false);
                }
                else
                {
                    OxygenUI.CrossFadeAlpha(1f, 0.5f, ignoreTimeScale: false);
                }
            }
        }

        [HarmonyPostfix]
        //[HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPatch(typeof(HUDManager), "Update")]
        public static void UpdatePatch()
        {
            if (!OxygenHUD.initialized)
            {
                mls.LogError("OxygenHUD is still instantiating");
                return;
            }

            PlayerControllerB pController = GameNetworkManager.Instance.localPlayerController;
            StartOfRound sor = StartOfRound.Instance;

            if (pController == null)
            {
                mls.LogError("PlayerControllerB is null");
                return;
            }

            if (sor == null)
            {
                mls.LogError("StartOfRound is null");
                return;
            }

            if (OxygenUI == null)
            {
                mls.LogError("oxygenUI is null");
                return;
            }

            if (inhaleSFX == null)
            {
                mls.LogError("inhalerSFX is null");
                return;
            }

            if (pController.isPlayerDead)
            {
                return;
            }

            if (EladsOxygenUIText != null)
            {
                float roundedValue = (float)Round(OxygenUI.fillAmount, 2);
                int oxygenInPercent = (int)(roundedValue * 100);

                OxygenHUD.EladsOxygenUIText.text = $"{oxygenInPercent}<size=75%><voffset=1>%</voffset></size>";
            }

            float localDecValue = decreasingOxygen;

            // can cause a problems with other mods (●'◡'●)
            if (!pController.isInsideFactory)
            {
                sor.drowningTimer = OxygenUI.fillAmount;
            }

            // just for simplification if player was teleported and unable to refill oxygen
            if (InfinityOxygenInModsPlaces && pController.serverPlayerPosition.y <= offset)
            {
                if (!backroomsNotification)
                {
                    if (isNotification)
                    {
                        HUDManager.Instance.DisplayTip("System...", "Oxygen outside is breathable, oxygen supply through cylinders is turned off");
                    }
                    backroomsNotification = true;
                }

                if (pController.drunkness != 0) pController.drunkness -= increasingOxygen;

                return;
            }

            if (timeSinceLastFear >= secTimerInFear)
            {
                //mls.LogError($"fear level: {sor.fearLevel}");
                //mls.LogError($"fearLevelIncreasing: {sor.fearLevelIncreasing}");

                if (sor.fearLevel > 0)
                {
                    if (enableOxygenSFX)
                    {
                        if (!pController.isInHangarShipRoom || (pController.isInHangarShipRoom && enableOxygenSFXInShip))
                        {
                            OxygenHUD.PlaySFX(pController, inhaleSFX[0]);
                        }
                    }

                    // just unnecessary to decrease oxygen in ship ~_~
                    if (!pController.isInHangarShipRoom)
                    {
                        mls.LogInfo("playing sound cause fearLevelIncreasing. oxygen consumption is increased by 2");
                        //mls.LogError($"fear level: {sor.fearLevel}");

                        localDecValue += multiplyDecreasingInFear;
                    }

                    timeSinceLastFear = 0f;
                }
            }
            timeSinceLastFear += Time.deltaTime; //increment the cool down timer

            if (timeSinceLastAction >= secTimer)
            {
                if (enableOxygenSFX && !sor.fearLevelIncreasing)
                {
                    if (!pController.isInHangarShipRoom || (pController.isInHangarShipRoom && enableOxygenSFXInShip))
                    {
                        int index = Random.Range(0, inhaleSFX.Length);
                        OxygenHUD.PlaySFX(pController, inhaleSFX[index]);
                    }
                }

                if (!pController.isInsideFactory && pController.isUnderwater && pController.underwaterCollider != null &&
                    pController.underwaterCollider.bounds.Contains(pController.gameplayCamera.transform.position))
                {
                    mls.LogInfo($"The player is underwater, oxygen consumption is increased by {oxygenDepletionInWater}");
                    localDecValue += oxygenDepletionInWater;

                    //mls.LogInfo($"oxyUI.fillAmount: {oxyUI.fillAmount}");
                    //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                }

                // if player running the oxygen goes away faster
                if (pController.isSprinting)
                {
                    localDecValue += oxygenDepletionWhileRunning;
                    mls.LogInfo($"The player is running, oxygen consumption is increased by {oxygenDepletionWhileRunning}");
                }

                // outside and not in ship
                if (!pController.isInsideFactory && !pController.isInHangarShipRoom)
                {
                    //isRecovering = false; // just to prevent creating a lot logs about recovering oxygen when player in ship

                    OxygenUI.fillAmount -= localDecValue;
                    mls.LogInfo($"current oxygen level: {OxygenUI.fillAmount}");
                }

                // inside factory
                if (pController.isInsideFactory)
                {
                    OxygenUI.fillAmount -= localDecValue;
                    mls.LogInfo($"current oxygen level: {OxygenUI.fillAmount}");
                }

                // notification about low level of oxygen
                if (OxygenUI.fillAmount < 0.45)
                {
                    if (!firstNotification)
                    {
                        if (isNotification)
                        {
                            HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                        }
                        firstNotification = true;
                    }
                }

                // system warning
                if (OxygenUI.fillAmount < 0.35)
                {
                    if (!warningNotification)
                    {
                        if (isNotification)
                        {
                            HUDManager.Instance.DisplayTip("System...", "There is a critical level of oxygen in the oxygen tanks, fill it up immediately!", isWarning: true);
                        }
                        warningNotification = true;
                    }
                }

                // increasing drunkness
                if (OxygenUI.fillAmount < 0.33)
                {
                    pController.drunkness += oxygenDeficiency;
                    mls.LogInfo($"current oxygen deficiency level: {pController.drunkness}");
                }

                // 0.30 is the lowest value when we see UI meter
                if (OxygenUI.fillAmount < 0.30)
                {              
                    pController.DamagePlayer(playerDamage);
                }

                // timer resets
                timeSinceLastAction = 0f;
            }
            timeSinceLastAction += Time.deltaTime; //increment the cool down timer

            // in ship
            if (pController.isInHangarShipRoom)
            {
                if (OxygenUI.fillAmount != 1)
                {
                    OxygenUI.fillAmount += increasingOxygen;
                    mls.LogInfo($"Oxygen is recovering: {OxygenUI.fillAmount}");
                }

                if (pController.drunkness != 0) pController.drunkness -= increasingOxygen;
            }
        }

        private static IEnumerator AwaitPlayerController()
        {
            yield return new WaitUntil(() => (Object)(object)GameNetworkManager.Instance.localPlayerController != null);
        }
    }
}
