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
        public static bool EnableOxygenSFX => OxygenBase.OxygenConfig.enableOxygenSFX.Value;
        public static bool EnableInhaleSFXWhileWalking => OxygenBase.OxygenConfig.enableInhaleSFXWhileWalking.Value;
        public static bool EnableOxygenSFXInShip => OxygenBase.OxygenConfig.enableOxygenSFXInShip.Value;
        public static bool EnableOxygenSFXOnTheCompany => OxygenBase.OxygenConfig.enableOxygenSFXOnTheCompany.Value;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenLogic");
        public static Image OxygenUI => OxygenInit.oxygenUI;

        public static bool InfinityOxygenInModsPlaces => OxygenBase.OxygenConfig.InfinityOxygenInModsPlaces.Value;

        public static int OxygenFillOption => OxygenBase.OxygenConfig.OxygenFillOption.Value;

        public static int PlayerDamage => OxygenBase.OxygenConfig.playerDamage.Value;

        public static bool IsgreenPlanet => MoonsDicts.GreenPlanetsValue;
        public static float IncreasingOxygen => OxygenBase.OxygenConfig.increasingOxygen.Value;
        public static float DecreasingOxygenOutside => MoonsDicts.DecreasingOxygenOutsideMoonsValue;
        public static float DecreasingOxygenInFactory => MoonsDicts.DecreasingOxygenInFactoryMoonsValue;
        public static float OxygenDepletionWhileRunning => MoonsDicts.OxygenRunningMoonsValue;
        public static float OxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        public static float ImmersiveVisor_OxygenDecreasing => OxygenBase.OxygenConfig.ImmersiveVisor_OxygenDecreasing.Value;

        public static float DecreasingInFear => OxygenBase.OxygenConfig.decreasingInFear.Value;
        public static float OxygenDeficiency => OxygenBase.OxygenConfig.oxygenDeficiency.Value;

        public static float SecTimer => OxygenBase.OxygenConfig.secTimer.Value;
        public static float secTimerInFear = 2f;
        public static float secTimerForAudio = 5f;

        private static float timeSinceLastAction = 0f;
        private static float timeSinceLastFear = 0f;
        private static float timeSinceLastPlayedAudio = 0f;

        internal static float low_OxygenAmount = OxygenBase.OxygenConfig.EladsHUD_QuickFix.Value && OxygenBase.Instance.IsEladsHUDFound ? 0.25f : 0.45f;
        internal static float critical_OxygenAmount = OxygenBase.OxygenConfig.EladsHUD_QuickFix.Value && OxygenBase.Instance.IsEladsHUDFound ? 0.15f : 0.35f;
        internal static float damage_OxygenAmount = OxygenBase.OxygenConfig.EladsHUD_QuickFix.Value && OxygenBase.Instance.IsEladsHUDFound ? 0.1f : 0.3f;

        public static bool IsNotification => OxygenBase.OxygenConfig.notifications.Value;
        internal static bool breathablePlace_Notification = false;
        internal static bool immersiveVisor_Notification = false;
        internal static bool lowLevel_Notification = false;
        internal static bool criticalLevel_Notification = false;

        internal static void ShowNotifications()
        {
            if (IsNotification)
            {
                // notification about low level of oxygen
                if (OxygenUI.fillAmount < low_OxygenAmount && !lowLevel_Notification)
                {
                    HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                    lowLevel_Notification = true;
                }

                // system warning
                if (OxygenUI.fillAmount < critical_OxygenAmount && !criticalLevel_Notification)
                {
                    HUDManager.Instance.DisplayTip(
                        "System...",
                        "Oxygen tanks have a critical level of oxygen, fill them up immediately!",
                        isWarning: true
                    );
                    criticalLevel_Notification = true;
                }
            }
        }

        public static void RunLogic()
        {
            if (OxygenInit.oxygenUI == null)
            {
                mls.LogError("oxygenUI is null, lol...");
                return;
            }

            StartOfRound sor = StartOfRound.Instance;
            if (sor == null)
            {
                mls.LogError("StartOfRound is null");
                return;
            }

            PlayerControllerB pc = GameNetworkManager.Instance.localPlayerController;
            if (pc == null)
            {
                mls.LogError("PlayerControllerB is null");
                return;
            }
            if (pc.isPlayerDead) return;

            //ShowNotifications();

            float localDecValue = 0f;
            sor.drowningTimer = OxygenUI.fillAmount;

            if (!pc.isInsideFactory && !pc.isInHangarShipRoom) localDecValue += DecreasingOxygenOutside;
            else if (pc.isInsideFactory) localDecValue += DecreasingOxygenInFactory;

            if (EnableOxygenSFX)
            {
                if (timeSinceLastPlayedAudio >= secTimerForAudio)
                {
                    bool shouldPlaySFX = false;

                    if (EnableOxygenSFXOnTheCompany && StartOfRound.Instance.currentLevel.levelID == 3 && !pc.isInHangarShipRoom)
                    {
                        shouldPlaySFX = true;
                    }
                    else if (IsgreenPlanet && pc.isInsideFactory)
                    {
                        shouldPlaySFX = true;
                    }
                    else if (pc.isInHangarShipRoom && EnableOxygenSFXInShip)
                    {
                        shouldPlaySFX = true;
                    }
                    else if (!pc.isInHangarShipRoom && StartOfRound.Instance.currentLevel.levelID != 3 && !IsgreenPlanet)
                    {
                        shouldPlaySFX = true;
                    }

                    if (shouldPlaySFX)
                    {
                        State state = State.standing;
                        float volume = 1f;

                        if (sor.fearLevel > 0)
                        {
                            if (!pc.isInHangarShipRoom || (pc.isInHangarShipRoom && EnableOxygenSFXInShip))
                            {
                                state = State.scared;
                                volume = OxygenBase.OxygenConfig.scaredSFX_volume.Value;
                            }
                        }
                        else if (pc.isSprinting)
                        {
                            state = State.running;
                            volume = OxygenBase.OxygenConfig.runningSFX_volume.Value;
                        }
                        else if (pc.isExhausted)
                        {
                            state = State.exhausted;
                            volume = OxygenBase.OxygenConfig.exhaustedSFX_volume.Value;
                        }
                        else if (pc.isWalking && EnableInhaleSFXWhileWalking)
                        {
                            state = State.walking;
                            volume = OxygenBase.OxygenConfig.walkingSFX_volume.Value;
                        }

                        if (state != State.standing)
                        {
                            AudioClip clip = FindSFX(state);

                            // updates the wait before the next playing
                            secTimerForAudio = (int)state;

                            PlaySFX(clip, volume);
                        }
                    }
                    timeSinceLastPlayedAudio = 0f;
                }
                timeSinceLastPlayedAudio += Time.deltaTime;
            }

            if (!pc.isInHangarShipRoom)
            {
                if (timeSinceLastFear >= secTimerInFear)
                {
                    if (sor.fearLevel > 0)
                    {
                        mls.LogInfo($"Oxygen consumption is increased by {DecreasingInFear}");
                        localDecValue += DecreasingInFear;

                        timeSinceLastFear = 0f;
                    }
                }
                timeSinceLastFear += Time.deltaTime;

                if (timeSinceLastAction >= SecTimer)
                { 
                    // support Immersive visor
                    if (OxygenBase.Instance.IsImmersiveVisorFound)
                    {
                        localDecValue += LogicForImmersiveVisor();
                    }

                    // if player running the oxygen goes away faster
                    if (pc.isSprinting)
                    {
                        localDecValue += OxygenDepletionWhileRunning;
                        mls.LogInfo($"The player is running, oxygen consumption is increased by {OxygenDepletionWhileRunning}");
                    }

                    // increasing drunkness
                    if (OxygenUI.fillAmount < critical_OxygenAmount)
                    {
                        pc.drunkness += OxygenDeficiency;
                        mls.LogInfo($"current oxygen deficiency level: {pc.drunkness}");
                    }

                    // it have to be before oxygen consumption underwater 
                    if (IsgreenPlanet && !pc.isInsideFactory)
                    {
                        mls.LogInfo("It's a green planet and you're outside, oxygen consumption is omitted!");
                        localDecValue = 0f;
                    }

                    if (!pc.isInsideFactory && pc.isUnderwater && pc.underwaterCollider != null &&
                        pc.underwaterCollider.bounds.Contains(pc.gameplayCamera.transform.position))
                    {
                        mls.LogInfo($"The player is underwater, oxygen consumption is increased by {OxygenDepletionInWater}");
                        localDecValue += OxygenDepletionInWater;

                        //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                    }

                    // 0.30 is the lowest value when we see UI meter
                    if (OxygenUI.fillAmount < damage_OxygenAmount)
                    {
                        pc.DamagePlayer(PlayerDamage);
                    }

                    // just for simplification if player was teleported and unable to refill oxygen
                    if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= -400f) // -400f is Y offset 
                    {
                        if (IsNotification && !breathablePlace_Notification)
                        {
                            HUDManager.Instance.DisplayTip("System...", "Oxygen outside is breathable, oxygen supply through cylinders is turned off");
                            breathablePlace_Notification = true;
                        }

                        pc.drunkness = Mathf.Clamp01(pc.drunkness - OxygenDeficiency);
                    }
                    else
                    {
                        OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount - localDecValue);
                        mls.LogInfo($"current oxygen level: {OxygenUI.fillAmount}");
                    }

                    // timer resets
                    timeSinceLastAction = 0f;
                }
                timeSinceLastAction += Time.deltaTime; //increment the cool down timer
            }
            else
            {
                if (OxygenFillOption == 2)
                {
                    if (OxygenUI.fillAmount != 1)
                    {
                        OxygenUI.fillAmount = Mathf.Clamp01(OxygenUI.fillAmount + IncreasingOxygen);
                        mls.LogDebug($"Oxygen is recovering: {OxygenUI.fillAmount}");
                    }
                }

                pc.drunkness = Mathf.Clamp01(pc.drunkness - IncreasingOxygen);
            }
        }

        internal static float LogicForImmersiveVisor()
        {
            if (Woecust.ImmersiveVisor.Visor.Instance.visorCrack.crackLevel.value == 2)
            {
                if (IsNotification && !immersiveVisor_Notification)
                {
                    HUDManager.Instance.DisplayTip(
                        "System...",
                        "The helmet is experiencing oxygen leakage due to substantial damage",
                        isWarning: true
                    );
                    immersiveVisor_Notification = true;
                }

                mls.LogInfo($"The helmet is fucked, oxygen consumption is increased by {ImmersiveVisor_OxygenDecreasing}");

                return ImmersiveVisor_OxygenDecreasing;
            }
            return 0;
        }
    }
}
