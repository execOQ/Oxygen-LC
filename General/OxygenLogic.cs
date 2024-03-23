using BepInEx.Logging;
using GameNetcodeStuff;
using Oxygen.Configuration;
using UnityEngine;
using UnityEngine.UI;
using static Oxygen.Extras.AudioController;

namespace Oxygen.GameObjects
{
    internal class OxygenLogic : MonoBehaviour
    {
        public static float DecreasingInFear => OxygenConfig.Instance.decreasingInFear.Value;

        public static bool EnableOxygenSFX => OxygenBase.OxygenConfig.enableOxygenSFX.Value;
        public static bool EnableOxygenSFXInShip => OxygenBase.OxygenConfig.enableOxygenSFXInShip.Value;
        public static bool EnableOxygenSFXOnTheCompany => OxygenBase.OxygenConfig.enableOxygenSFXOnTheCompany.Value;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenLogic");
        public static Image OxygenUI => OxygenHUD.oxygenUI;

        public static bool InfinityOxygenInModsPlaces => OxygenConfig.Instance.InfinityOxygenInModsPlaces.Value;

        public static int OxygenFillOption => OxygenConfig.Instance.OxygenFillOption.Value;

        public static int PlayerDamage => OxygenConfig.Instance.playerDamage.Value;

        public static bool IsgreenPlanet => MoonsDicts.GreenPlanetsValue;
        public static float IncreasingOxygen => MoonsDicts.IncreasingOxygenMoonsValue;
        public static float DecreasingOxygenOutside => MoonsDicts.DecreasingOxygenOutsideMoonsValue;
        public static float DecreasingOxygenInFactory => MoonsDicts.DecreasingOxygenInFactoryMoonsValue;
        public static float OxygenDepletionWhileRunning => MoonsDicts.OxygenRunningMoonsValue;
        public static float OxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        public static float OxygenDeficiency => OxygenConfig.Instance.oxygenDeficiency.Value;

        public static float SecTimer => OxygenConfig.Instance.secTimer.Value;
        public static float secTimerInFear = 2f;
        public static float secTimerForAudio = 5f;

        private static float timeSinceLastAction = 0f;
        private static float timeSinceLastFear = 0f;
        private static float timeSinceLastPlayedAudio = 0f;

        public static void RunLogic(StartOfRound sor, PlayerControllerB pc)
        {
            float localDecValue = 0f;
            sor.drowningTimer = OxygenUI.fillAmount;

            if (!pc.isInsideFactory && !pc.isInHangarShipRoom) localDecValue += DecreasingOxygenOutside;
            else if (pc.isInsideFactory) localDecValue += DecreasingOxygenInFactory;

            if (timeSinceLastFear >= secTimerInFear)
            {
                if (sor.fearLevel > 0)
                {
                    if (EnableOxygenSFX)
                    {
                        if (!pc.isInHangarShipRoom || (pc.isInHangarShipRoom && EnableOxygenSFXInShip))
                        {
                            mls.LogInfo($"Playing sound cause fearLevelIncreasing");
                            AudioClip clip = FindSFX(Stage.scared);
                            PlaySFX(pc, clip);
                        }
                    }

                    mls.LogInfo($"Oxygen consumption is increased by {DecreasingInFear}");
                    localDecValue += DecreasingInFear;

                    timeSinceLastFear = 0f;
                }
            }
            timeSinceLastFear += Time.deltaTime;

            if (EnableOxygenSFX)
            {
                if (timeSinceLastPlayedAudio >= secTimerForAudio)
                {
                    bool shouldPlaySFX = false;

                    if (EnableOxygenSFXOnTheCompany && StartOfRound.Instance.currentLevel.levelID == 3 && !pc.isInHangarShipRoom)
                        shouldPlaySFX = true;
                    else if (pc.isInHangarShipRoom && EnableOxygenSFXInShip)
                        shouldPlaySFX = true;
                    else if (!pc.isInHangarShipRoom && StartOfRound.Instance.currentLevel.levelID != 3)
                        shouldPlaySFX = true;

                    if (shouldPlaySFX)
                    {
                        Stage stage = Stage.standing;

                        // for support Immersive visor
                        /* if ()
                        {
                            stage = Stage.oxygenLeak;
                        }
                        else 
                        */
                        if (OxygenUI.fillAmount < 0.27)
                        {
                            stage = Stage.outOfOxygen;
                        }
                        else if (sor.fearLevel > 0)
                        {
                            if (!pc.isInHangarShipRoom || (pc.isInHangarShipRoom && EnableOxygenSFXInShip))
                            {
                                stage = Stage.scared;
                            }
                        }
                        else if (pc.isSprinting)
                        {
                            stage = Stage.running;
                        }
                        else if (pc.isExhausted)
                        {
                            stage = Stage.exhausted;
                        }
                        else if (pc.isWalking)
                        {
                            stage = Stage.walking;
                        }

                        if (stage != Stage.standing)
                        {
                            AudioClip clip = FindSFX(stage);

                            // updates the wait before the next playing
                            secTimerForAudio = (int)stage;

                            PlaySFX(pc, clip);
                        }
                    }
                    timeSinceLastPlayedAudio = 0f;
                }
                timeSinceLastPlayedAudio += Time.deltaTime;
            }

            if (timeSinceLastAction >= SecTimer)
            {
                // if player running the oxygen goes away faster
                if (pc.isSprinting)
                {
                    localDecValue += OxygenDepletionWhileRunning;
                    mls.LogInfo($"The player is running, oxygen consumption is increased by {OxygenDepletionWhileRunning}");
                }
                
                // increasing drunkness
                if (OxygenUI.fillAmount < 0.33)
                {
                    pc.drunkness += OxygenDeficiency;
                    mls.LogInfo($"current oxygen deficiency level: {pc.drunkness}");
                }

                if (!pc.isInsideFactory && pc.isUnderwater && pc.underwaterCollider != null &&
                    pc.underwaterCollider.bounds.Contains(pc.gameplayCamera.transform.position))
                {
                    mls.LogInfo($"The player is underwater, oxygen consumption is increased by {OxygenDepletionInWater}");
                    OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount - OxygenDepletionInWater);

                    //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                }

                // 0.30 is the lowest value when we see UI meter
                if (OxygenUI.fillAmount < 0.30)
                {
                    pc.DamagePlayer(PlayerDamage);
                }

                if (IsgreenPlanet && !pc.isInHangarShipRoom && !pc.isInsideFactory)
                {
                    mls.LogInfo("It's a green planet and you're outside, oxygen consumption is omitted!");
                    localDecValue = 0f;
                }

                if (!pc.isInHangarShipRoom)
                {
                    // just for simplification if player was teleported and unable to refill oxygen
                    if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= -400f) // -400f is Y offset 
                    {
                        OxygenHUD.ShowAnotherNotification();

                        pc.drunkness = Mathf.Clamp01(pc.drunkness - IncreasingOxygen);
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

            if (pc.isInHangarShipRoom)
            {
                if (OxygenFillOption == 2)
                {
                    if (OxygenUI.fillAmount != 1)
                    {
                        OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount + IncreasingOxygen);
                        mls.LogInfo($"Oxygen is recovering: {OxygenUI.fillAmount}");
                    }
                }

                pc.drunkness = Mathf.Clamp01(pc.drunkness - IncreasingOxygen);
            }
        }
    }
}
