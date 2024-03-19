using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Configuration;
using static System.Math;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Oxygen.Extras;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch
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
        public static float oxygenDepletionWhileRunning => MoonsDicts.OxygenRunningMoonsValue;
        public static float oxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        public static float oxygenDeficiency => OxygenConfig.Instance.oxygenDeficiency.Value;

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

            StartOfRound sor = StartOfRound.Instance;

            if (__instance.isWalking)
            {
                AudioController.Instance.CurrentStage = AudioController.Stage.walking;
            } 
            else if (__instance.isSprinting)
            {
                AudioController.Instance.CurrentStage = AudioController.Stage.running;
            }
            else if (__instance.isExhausted)
            {
                AudioController.Instance.CurrentStage = AudioController.Stage.exhausted;
            }
            else if (sor.fearLevel > 0)
            {
                AudioController.Instance.CurrentStage = AudioController.Stage.scared;
            } 
            else
            {
                AudioController.Instance.CurrentStage = AudioController.Stage.standing;
            }
        }

        public static void UpdateModsCompatibility()
        {
            if (EladsOxygenUIText != null)
            {
                float roundedValue = (float)Round(OxygenUI.fillAmount, 2);
                int oxygenInPercent = (int)(roundedValue * 100);

                OxygenHUD.EladsOxygenUIText.text = $"{oxygenInPercent}<size=75%><voffset=1>%</voffset></size>";
            }

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

        public static void ShowNotifications()
        {
            if (isNotification)
            {
                // notification about low level of oxygen
                if (OxygenUI.fillAmount < 0.45 && !firstNotification)
                {
                    HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                    firstNotification = true;
                }

                // system warning
                if (OxygenUI.fillAmount < 0.35 && !warningNotification)
                {
                    HUDManager.Instance.DisplayTip(
                        "System...",
                        "There is a critical level of oxygen in the oxygen tanks, fill it up immediately!",
                        isWarning: true
                    );
                    warningNotification = true;
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

            if (pc.isPlayerDead)
            {
                return;
            }

            // can cause a problems with other mods (●'◡'●)
            sor.drowningTimer = OxygenUI.fillAmount;

            UpdateModsCompatibility();
            ShowNotifications();

            float localDecValue = 0f;
            if (!pc.isInsideFactory && !pc.isInHangarShipRoom) localDecValue += decreasingOxygenOutside;
            else if (pc.isInsideFactory) localDecValue += decreasingOxygenInFactory;

            if (timeSinceLastFear >= secTimerInFear)
            {
                if (sor.fearLevel > 0)
                {
                    if (enableOxygenSFX)
                    {
                        if (!pc.isInHangarShipRoom || (pc.isInHangarShipRoom && enableOxygenSFXInShip))
                        {
                            //mls.LogInfo($"Playing sound cause fearLevelIncreasing");
                            AudioClip clip = AudioController.FindSFX(AudioController.Stage.scared);
                            AudioController.PlaySFX(pc, clip);
                        }
                    }

                    mls.LogInfo($"Oxygen consumption is increased by {decreasingInFear}");
                    localDecValue += decreasingInFear;

                    timeSinceLastFear = 0f;
                }
            }
            timeSinceLastFear += Time.deltaTime; //increment the cool down timer

            if (timeSinceLastAction >= secTimer)
            {
                if (enableOxygenSFX && sor.fearLevel <= 0)
                {
                    bool shouldPlaySFX = false;

                    if (enableOxygenSFXOnTheCompany && StartOfRound.Instance.currentLevel.levelID == 3 && !pc.isInHangarShipRoom)
                        shouldPlaySFX = true;
                    else if (pc.isInHangarShipRoom && enableOxygenSFXInShip)
                        shouldPlaySFX = true;
                    else if (!pc.isInHangarShipRoom && StartOfRound.Instance.currentLevel.levelID != 3)
                        shouldPlaySFX = true;
                    
                    if (shouldPlaySFX)
                    {
                        mls.LogInfo("playing oxygen SFX");
                        int index = Random.Range(0, inhaleSFX.Length);
                        OxygenHUD.PlaySFX(pc, inhaleSFX[index]);
                    }
                }

                if (!pc.isInsideFactory && pc.isUnderwater && pc.underwaterCollider != null &&
                    pc.underwaterCollider.bounds.Contains(pc.gameplayCamera.transform.position))
                {
                    mls.LogInfo($"The player is underwater, oxygen consumption is increased by {oxygenDepletionInWater}");
                    localDecValue += oxygenDepletionInWater;

                    //mls.LogInfo($"oxyUI.fillAmount: {oxyUI.fillAmount}");
                    //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                }
                
                // if player running the oxygen goes away faster
                if (pc.isSprinting)
                {
                    localDecValue += oxygenDepletionWhileRunning;
                    mls.LogInfo($"The player is running, oxygen consumption is increased by {oxygenDepletionWhileRunning}");
                }

                // increasing drunkness
                if (OxygenUI.fillAmount < 0.33)
                {
                    pc.drunkness += oxygenDeficiency;
                    mls.LogInfo($"current oxygen deficiency level: {pc.drunkness}");
                }

                // 0.30 is the lowest value when we see UI meter
                if (OxygenUI.fillAmount < 0.30)
                {              
                    pc.DamagePlayer(playerDamage);
                }


                if (IsgreenPlanet && !pc.isInHangarShipRoom && !pc.isInsideFactory)
                {
                    mls.LogInfo("It's a green planet. Oxygen consumption is omitted!");
                    localDecValue = 0f;
                }

                if (!!pc.isInHangarShipRoom)
                {
                    // just for simplification if player was teleported and unable to refill oxygen
                    if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= offset)
                    {
                        if (!backroomsNotification)
                        {
                            if (isNotification)
                            {
                                HUDManager.Instance.DisplayTip("System...", "Oxygen outside is breathable, oxygen supply through cylinders is turned off");
                            }
                            backroomsNotification = true;
                        }

                        pc.drunkness = Mathf.Clamp01(pc.drunkness - increasingOxygen);
                    } 
                    else
                    {
                        OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount - localDecValue);
                        mls.LogInfo($"current oxygen level: {OxygenUI.fillAmount}");
                    }
                }

                // timer resets
                timeSinceLastAction = 0f;
            }
            timeSinceLastAction += Time.deltaTime; //increment the cool down timer

            // in ship
            if (pc.isInHangarShipRoom)
            {
                if (oxygenFillOption == 2)
                {
                    if (OxygenUI.fillAmount != 1)
                    {
                        OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount + increasingOxygen);
                        mls.LogInfo($"Oxygen is recovering: {OxygenUI.fillAmount}");
                    }
                }

                pc.drunkness = Mathf.Clamp01(pc.drunkness - increasingOxygen);
            }
        }

        private static IEnumerator AwaitPlayerController()
        {
            yield return new WaitUntil(() => (Object)(object)GameNetworkManager.Instance.localPlayerController != null);
        }
    }
}
