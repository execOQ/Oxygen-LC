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
using System.Net;
using System.Linq;

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
        public static bool InfinityOxygenInModsPlaces => OxygenConfig.Instance.InfinityOxygenInModsPlaces.Value;

        public static int oxygenFillOption => OxygenConfig.Instance.OxygenFillOption.Value;

        public static int playerDamage => OxygenConfig.Instance.playerDamage.Value;

        public static bool IsgreenPlanet => MoonsDicts.GreenPlanetsValue;
        public static float increasingOxygen => MoonsDicts.IncreasingOxygenMoonsValue;
        public static float decreasingOxygenOutside => MoonsDicts.DecreasingOxygenOutsideMoonsValue;
        public static float decreasingOxygenInFactory => MoonsDicts.DecreasingOxygenInFactoryMoonsValue;
        public static float decreasingInFear => OxygenConfig.Instance.decreasingInFear.Value;
        //public static float decreasingInFear => MoonsDicts.DecreasingInFearMoonsValue;
        public static float oxygenDepletionWhileRunning => MoonsDicts.OxygenRunningMoonsValue;
        public static float oxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        public static float oxygenDeficiency => OxygenConfig.Instance.oxygenDeficiency.Value;

        public static bool oxygenConsumptionOnTheCompany => OxygenConfig.Instance.oxygenConsumptionOnTheCompany.Value;

        public static float secTimer => OxygenConfig.Instance.secTimer.Value;  // number of seconds the cool down timer lasts
        //

        public static bool enableOxygenSFX => OxygenBase.OxygenConfig.enableOxygenSFX.Value;
        public static bool enableOxygenSFXInShip => OxygenBase.OxygenConfig.enableOxygenSFXInShip.Value;
        public static bool enableOxygenSFXOnTheCompany => OxygenBase.OxygenConfig.enableOxygenSFXOnTheCompany.Value;

        public static float secTimerInFear = 2f;

        private static float timeSinceLastAction = 0f;  //number of seconds since we did something
        private static float timeSinceLastFear = 0f;  //number of seconds since we were fear

        public static bool isNotification => OxygenBase.OxygenConfig.notifications.Value;

        internal static bool backroomsNotification = false;
        internal static bool firstNotification = false;
        internal static bool warningNotification = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HUDManager), "Awake")]
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

            if (OxygenBase.Instance.isShyHUDFound && OxygenConfig.Instance.ShyHUDSupport)
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

            float localDecValue = 0f;
            //if (!pController.isInsideFactory && !pController.isInHangarShipRoom) localDecValue += decreasingOxygenOutside;
            //if (pController.isInsideFactory) localDecValue += decreasingOxygenInFactory;

            // can cause a problems with other mods (●'◡'●)
            sor.drowningTimer = OxygenUI.fillAmount;

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
                if (sor.fearLevel > 0)
                {
                    if (enableOxygenSFX)
                    {
                        if (!pController.isInHangarShipRoom || (pController.isInHangarShipRoom && enableOxygenSFXInShip))
                        {
                            //mls.LogInfo($"Playing sound cause fearLevelIncreasing");
                            OxygenHUD.PlaySFX(pController, inhaleSFX[0]);
                        }
                    }

                    // just unnecessary to decrease oxygen in ship ~_~
                    if (!pController.isInHangarShipRoom)
                    {
                        mls.LogInfo($"Oxygen consumption is increased by {decreasingInFear}");
                        //mls.LogError($"fear level: {sor.fearLevel}");

                        localDecValue += decreasingInFear;
                    }

                    timeSinceLastFear = 0f;
                }
            }
            timeSinceLastFear += Time.deltaTime; //increment the cool down timer

            if (timeSinceLastAction >= secTimer)
            {
                //mls.LogInfo($"Synced: {OxygenConfig.Synced}");
                //mls.LogInfo($"increasingOxygen: {increasingOxygen}");
                //mls.LogInfo($"decreasingOxygenOutside: {decreasingOxygenOutside}");
                //mls.LogInfo($"decreasingOxygenInFactory: {decreasingOxygenInFactory}");
                //mls.LogInfo($"decreasingInFear: {decreasingInFear}");
                //mls.LogInfo($"oxygenDepletionWhileRunning: {oxygenDepletionWhileRunning}");
                //mls.LogInfo($"oxygenDepletionInWater: {oxygenDepletionInWater}");

                if (enableOxygenSFX && sor.fearLevel <= 0)
                {
                    bool shouldPlaySFX = false;

                    if (enableOxygenSFXOnTheCompany && StartOfRound.Instance.currentLevel.levelID == 3 && !pController.isInHangarShipRoom)
                        shouldPlaySFX = true;
                    else if (pController.isInHangarShipRoom && enableOxygenSFXInShip)
                        shouldPlaySFX = true;
                    else if (!pController.isInHangarShipRoom && StartOfRound.Instance.currentLevel.levelID != 3)
                        shouldPlaySFX = true;

                    if (shouldPlaySFX)
                    {
                        mls.LogInfo("playing oxygen SFX");
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

                // outside
                if (!pController.isInHangarShipRoom && !pController.isInsideFactory)
                {
                    if (!oxygenConsumptionOnTheCompany && StartOfRound.Instance.currentLevel.levelID == 3)
                    {
                        mls.LogInfo("Oxygen consumption on the company's planet is disabled, skipping... ~_~");
                    }
                    else
                    {
                        if (IsgreenPlanet)
                        {
                            mls.LogInfo("It's a green planet! Oxygen consumption is omitted");
                        }
                        else
                        {
                            OxygenUI.fillAmount -= localDecValue + decreasingOxygenOutside;
                            mls.LogInfo($"current oxygen level: {OxygenUI.fillAmount}");
                        }
                    }
                }

                // in the facility
                if (pController.isInsideFactory)
                {
                    OxygenUI.fillAmount -= localDecValue + decreasingOxygenInFactory;
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
                if (oxygenFillOption == 2)
                {
                    if (OxygenUI.fillAmount != 1)
                    {
                        OxygenUI.fillAmount += increasingOxygen;
                        mls.LogInfo($"Oxygen is recovering: {OxygenUI.fillAmount}");
                    }
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
