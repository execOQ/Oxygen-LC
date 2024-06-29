using BepInEx.Logging;
using GameNetcodeStuff;
using Oxygen.Configuration;
using UnityEngine;
using static Oxygen.Extras.AudioController;
using static Oxygen.Configuration.OxygenConfig;
using Oxygen.Items;

namespace Oxygen.GameObjects
{
    internal class OxygenLogic : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenLogic");

        public const float low_OxygenAmount = 0.2f;
        public const float critical_OxygenAmount = 0.1f;
        public const float damage_OxygenAmount = 0f;

        private static float OxygenAmount {
            get => OxygenInit.Percent;
            set => OxygenInit.Percent = value;
        }

        #region Supports
        private static bool InfinityOxygenInModsPlaces => OxygenBase.OxygenConfig.infinityOxygenInModsPlaces.Value;

        private static bool ImmersiveVisorSupport => OxygenBase.OxygenConfig.immersiveVisorSupport.Value;
        private static float ImmersiveVisor_OxygenDecreasing => OxygenBase.OxygenConfig.immersiveVisor_OxygenDecreasing.Value;
        #endregion

        #region General
        private static AutoFillingOnShip AutoFillingOnShip => OxygenBase.OxygenConfig.autoFillingOnShip.Value;

        private static int PlayerDamage => OxygenBase.OxygenConfig.playerDamage.Value;

        private static bool IsgreenPlanet => MoonsDicts.GreenPlanetsValue;
        private static float IncreasingOxygen => OxygenBase.OxygenConfig.autoFillingOnShip_increasingOxygen.Value;
        private static float DecreasingOxygenOutside => MoonsDicts.DecreasingOxygenOutsideMoonsValue;
        private static float DecreasingOxygenInFactory => MoonsDicts.DecreasingOxygenInFactoryMoonsValue;
        private static float RunningMultiplier => MoonsDicts.RunningMultiplierMoonsValue;
        private static float OxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        private static float DecreasingInFear => OxygenBase.OxygenConfig.decreasingInFear.Value;
        private static float OxygenDeficiency => OxygenBase.OxygenConfig.oxygenDeficiency.Value;
        #endregion

        #region Timer
        private static float SecTimer => OxygenBase.OxygenConfig.secTimer.Value;
        private static float secTimerInFear = 2f;
        private static float secTimerForAudio = 5f;

        private static float timeSinceLastAction = 0f;
        private static float timeSinceLastFear = 0f;
        private static float timeSinceLastPlayedAudio = 0f;
        #endregion

        #region Audio
        private static bool EnableOxygenSFX => OxygenBase.OxygenConfig.enableOxygenSFX.Value;
        private static bool EnableInhaleSFXWhileWalking => OxygenBase.OxygenConfig.enableInhaleSFXWhileWalking.Value;
        private static bool EnableOxygenSFXInShip => OxygenBase.OxygenConfig.enableOxygenSFXInShip.Value;
        private static bool EnableOxygenSFXOnTheCompany => OxygenBase.OxygenConfig.enableOxygenSFXOnTheCompany.Value;

        private static void SFX_Logic(PlayerControllerB pc, StartOfRound sor)
        {
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
                                wasInFearOrExhaustedLastFrame = true;
                            }
                        }
                        else if (pc.isSprinting && !breathablePlace_Notification)
                        {
                            state = State.running;
                            volume = OxygenBase.OxygenConfig.runningSFX_volume.Value;
                        }
                        else if (pc.isExhausted && !breathablePlace_Notification)
                        {
                            state = State.exhausted;
                            volume = OxygenBase.OxygenConfig.exhaustedSFX_volume.Value;
                            wasInFearOrExhaustedLastFrame = true;
                        }
                        else if (pc.isWalking && EnableInhaleSFXWhileWalking && !breathablePlace_Notification)
                        {
                            state = State.walking;
                            volume = OxygenBase.OxygenConfig.walkingSFX_volume.Value;
                        }

                        bool isEndOfFearOrExhausted = false;
                        if (wasInFearOrExhaustedLastFrame)
                        {
                            if (sor.fearLevel <= 0 && state != State.scared && !pc.isExhausted && state != State.exhausted)
                            {
                                wasInFearOrExhaustedLastFrame = false;
                                isEndOfFearOrExhausted = true;

                                // just to bypass next if
                                state = State.scared;
                            }

                            //mls.LogDebug(state);
                            //mls.LogDebug($"isEndOfFearOrExhausted: {isEndOfFearOrExhausted}");
                        }

                        if (state != State.standing)
                        {
                            PlaySFX(state, volume, isEndOfFearOrExhausted);
                        }

                        // updates the wait before the next playing
                        secTimerForAudio = (int)state;
                    }
                    timeSinceLastPlayedAudio = 0f;
                }
                timeSinceLastPlayedAudio += Time.deltaTime;
            }
        }

        #endregion

        #region Notifications
        private static bool IsNotification => OxygenBase.OxygenConfig.notifications.Value;

        private static bool breathablePlace_Notification = false;
        private static bool immersiveVisor_Notification = false;
        private static bool lowLevel_Notification = false;
        private static bool criticalLevel_Notification = false;

        internal static void ShowNotifications(PlayerControllerB pc)
        {
            if (IsNotification)
            {
                // notification about low level of oxygen
                if (OxygenAmount < low_OxygenAmount)
                {
                    if (!lowLevel_Notification)
                    {
                        HUDManager.Instance.DisplayTip(
                            "System...", 
                            "The oxygen tanks are running low."
                        );
                        lowLevel_Notification = true;
                    }
                } else
                {
                    lowLevel_Notification = false;
                }

                // system warning
                if (OxygenAmount < critical_OxygenAmount)
                {
                    if (!criticalLevel_Notification)
                    {
                        HUDManager.Instance.DisplayTip(
                            "System...",
                            "Oxygen tanks have a critical level of oxygen, fill them up immediately!",
                            isWarning: true
                        );
                        criticalLevel_Notification = true;
                    }
                } else
                {
                    criticalLevel_Notification = false;
                }

                if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= -480f) // -480f is Y offset 
                {
                    if (!breathablePlace_Notification)
                    {
                        HUDManager.Instance.DisplayTip(
                            "System...", 
                            "Oxygen outside is breathable, oxygen supply through cylinders is turned off"
                        );
                        breathablePlace_Notification = true;
                    }
                } else
                {
                    breathablePlace_Notification = false;
                }

                if (OxygenBase.Instance.IsImmersiveVisorFound && ImmersiveVisorSupport)
                {
                    Notification_ImmersiveVisor();
                }
            }
        }

        internal static void ResetAllNotifications()
        {
            breathablePlace_Notification = false;
            immersiveVisor_Notification = false;
            lowLevel_Notification = false;
            criticalLevel_Notification = false;
        }

        private static void Notification_ImmersiveVisor()
        {
            if (Woecust.ImmersiveVisor.Visor.Instance.visorCrack.crackLevel.value == 2)
            {
                if (!immersiveVisor_Notification)
                {
                    HUDManager.Instance.DisplayTip(
                        "System...",
                        "The helmet is experiencing oxygen leakage due to substantial damage",
                        isWarning: true
                    );
                    immersiveVisor_Notification = true;
                }
            }
            else
            {
                immersiveVisor_Notification = false;
            }
        }

        #endregion

        #region Other
        private static bool wasInFearOrExhaustedLastFrame = false;
        private static bool wasRunningLastFrame = false;
        private static float runTime = 0;

        #endregion

        internal static void RunLogic()
        {
            if (!OxygenInit.IsOxygenHUDInitialized)
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

            float localDecValue = 0f;
            sor.drowningTimer = OxygenAmount;

            SFX_Logic(pc, sor);

            if (!pc.isInHangarShipRoom)
            {
                if (pc.isSprinting)
                {
                    wasRunningLastFrame = true;
                    runTime += Time.deltaTime;
                }
                else if (!pc.isSprinting && wasRunningLastFrame && runTime > 0.4)
                {
                    float currentStaminaFillAmount = OxygenInit.StaminaFillAmount;
                    float totalOxygenConsumption = (float)(RunningMultiplier * pc.movementSpeed * runTime * (1 - currentStaminaFillAmount) / 1000);

                    // if stamina has not dropped much, then we reduce oxygen consumption
                    if (currentStaminaFillAmount > 0.8f)
                    {
                        totalOxygenConsumption *= 0.5f; // reduce consumption by 50% if stamina is above 80%
                    }

                    mls.LogDebug($"player was running for {runTime}");
                    mls.LogDebug($"player's current stamina amount is {currentStaminaFillAmount}");
                    mls.LogDebug($"total oxygen consumption: {totalOxygenConsumption}");

                    localDecValue += totalOxygenConsumption;

                    wasRunningLastFrame = false;
                    runTime = 0f;
                }

                if (timeSinceLastFear >= secTimerInFear)
                {
                    if (sor.fearLevel > 0)
                    {
                        mls.LogDebug($"Oxygen consumption is increased by {DecreasingInFear}");
                        localDecValue += DecreasingInFear;

                        timeSinceLastFear = 0f;
                    }
                }
                timeSinceLastFear += Time.deltaTime;

                if (timeSinceLastAction >= SecTimer)
                {
                    // it has to be before oxygen consumption underwater 
                    if (IsgreenPlanet && !pc.isInsideFactory)
                    {
                        mls.LogDebug("It's a green planet and you're outside, oxygen consumption is omitted!");
                        localDecValue = 0f;
                    }
                    else if (!pc.isInsideFactory) // means outside
                    {
                        localDecValue += DecreasingOxygenOutside;
                    }
                    else if (pc.isInsideFactory)
                    {
                        localDecValue += DecreasingOxygenInFactory;
                    }

                    // support for Immersive visor
                    if (OxygenBase.Instance.IsImmersiveVisorFound && ImmersiveVisorSupport)
                    {
                        localDecValue += LogicForImmersiveVisor();
                    }

                    // increasing drunkness
                    if (OxygenAmount < critical_OxygenAmount)
                    {
                        pc.drunkness = Mathf.Clamp01(pc.drunkness + OxygenDeficiency);
                        mls.LogDebug($"current oxygen deficiency level: {pc.drunkness}");
                    }

                    if (pc.isUnderwater && pc.underwaterCollider != null && pc.underwaterCollider.bounds.Contains(pc.gameplayCamera.transform.position))
                    {
                        mls.LogDebug($"The player is underwater, oxygen consumption is increased by {OxygenDepletionInWater}");
                        localDecValue += OxygenDepletionInWater;

                        //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                    }

                    // 0.30 is the lowest value when we still see UI meter (without AccurateMeter enabled)
                    if (OxygenAmount <= damage_OxygenAmount)
                    {
                        pc.DamagePlayer(PlayerDamage);  
                    }

                    // if player was teleported and unable to refill oxygen
                    if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= -480f) // -480f is Y offset 
                    {
                        pc.drunkness = Mathf.Clamp01(pc.drunkness - OxygenDeficiency);
                    }
                    else
                    {
                        OxygenAmount -= localDecValue;
                        mls.LogDebug($"current oxygen level: {OxygenAmount}");
                    }

                    timeSinceLastAction = 0f;
                }
                timeSinceLastAction += Time.deltaTime;
            }
            else
            {
                if ((AutoFillingOnShip == AutoFillingOnShip.WhenDoorsClosed && sor.hangarDoorsClosed) || (AutoFillingOnShip == AutoFillingOnShip.WhenPlayerOnShip))
                {
                    if (OxygenAmount < 1)
                    {
                        OxyCharger.Instance.AutoRefillOxygen();
                        mls.LogDebug($"Oxygen is recovering: {OxygenAmount}");
                    }
                }

                pc.drunkness = Mathf.Clamp01(pc.drunkness - IncreasingOxygen);
            }
        }

        private static float LogicForImmersiveVisor()
        {
            if (Woecust.ImmersiveVisor.Visor.Instance.visorCrack.crackLevel.value == 2)
            {
                mls.LogDebug($"The helmet is fucked, oxygen consumption is increased by {ImmersiveVisor_OxygenDecreasing}");

                return ImmersiveVisor_OxygenDecreasing;
            }
            return 0;
        }
    }
}
