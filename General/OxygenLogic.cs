﻿using BepInEx.Logging;
using GameNetcodeStuff;
using Oxygen.Configuration;
using UnityEngine;
using static Oxygen.Extras.AudioController;

namespace Oxygen.GameObjects
{
    internal class OxygenLogic : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenLogic");

        private static float OxygenAmount {
            get => OxygenInit.Percent;
            set => OxygenInit.Percent = value;
        }

        // Compatibilitions and supports
        private static bool InfinityOxygenInModsPlaces => OxygenBase.OxygenConfig.infinityOxygenInModsPlaces.Value;

        private static bool ImmersiveVisorSupport = OxygenBase.OxygenConfig.immersiveVisorSupport.Value;
        private static float ImmersiveVisor_OxygenDecreasing => OxygenBase.OxygenConfig.immersiveVisor_OxygenDecreasing.Value;

        // General
        private static int OxygenFillOption => OxygenBase.OxygenConfig.oxygenFillOption.Value;

        private static int PlayerDamage => OxygenBase.OxygenConfig.playerDamage.Value;

        private static bool IsgreenPlanet => MoonsDicts.GreenPlanetsValue;
        private static float IncreasingOxygen => OxygenBase.OxygenConfig.increasingOxygen.Value;
        private static float DecreasingOxygenOutside => MoonsDicts.DecreasingOxygenOutsideMoonsValue;
        private static float DecreasingOxygenInFactory => MoonsDicts.DecreasingOxygenInFactoryMoonsValue;
        private static float OxygenDepletionWhileRunning => MoonsDicts.OxygenRunningMoonsValue;
        private static float OxygenDepletionInWater => MoonsDicts.OxygenDepletionInWaterMoonsValue;

        private static float DecreasingInFear => OxygenBase.OxygenConfig.decreasingInFear.Value;
        private static float OxygenDeficiency => OxygenBase.OxygenConfig.oxygenDeficiency.Value;

        // Timer
        private static float SecTimer => OxygenBase.OxygenConfig.secTimer.Value;
        private static float secTimerInFear = 2f;
        private static float secTimerForAudio = 5f;

        private static float timeSinceLastAction = 0f;
        private static float timeSinceLastFear = 0f;
        private static float timeSinceLastPlayedAudio = 0f;

        // Notifications
        private static bool IsNotification => OxygenBase.OxygenConfig.notifications.Value;
        private static readonly float low_OxygenAmount = 0.25f;
        private static readonly float critical_OxygenAmount = 0.1f;
        private static readonly float damage_OxygenAmount = 0f;

        internal static bool breathablePlace_Notification = false;
        internal static bool immersiveVisor_Notification = false;
        internal static bool lowLevel_Notification = false;
        internal static bool criticalLevel_Notification = false;

        // Sounds
        private static bool EnableOxygenSFX => OxygenBase.OxygenConfig.enableOxygenSFX.Value;
        private static bool EnableInhaleSFXWhileWalking => OxygenBase.OxygenConfig.enableInhaleSFXWhileWalking.Value;
        private static bool EnableOxygenSFXInShip => OxygenBase.OxygenConfig.enableOxygenSFXInShip.Value;
        private static bool EnableOxygenSFXOnTheCompany => OxygenBase.OxygenConfig.enableOxygenSFXOnTheCompany.Value;

        internal static void ShowNotifications()
        {
            if (IsNotification)
            {
                // notification about low level of oxygen
                if (OxygenAmount < low_OxygenAmount && !lowLevel_Notification)
                {
                    HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                    lowLevel_Notification = true;
                }

                // system warning
                if (OxygenAmount < critical_OxygenAmount && !criticalLevel_Notification)
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
            sor.drowningTimer = OxygenInit.Percent;

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
                        else if (pc.isSprinting && !breathablePlace_Notification)
                        {
                            state = State.running;
                            volume = OxygenBase.OxygenConfig.runningSFX_volume.Value;
                        }
                        else if (pc.isExhausted && !breathablePlace_Notification)
                        {
                            state = State.exhausted;
                            volume = OxygenBase.OxygenConfig.exhaustedSFX_volume.Value;
                        }
                        else if (pc.isWalking && EnableInhaleSFXWhileWalking && !breathablePlace_Notification)
                        {
                            state = State.walking;
                            volume = OxygenBase.OxygenConfig.walkingSFX_volume.Value;
                        }

                        if (state != State.standing)
                        {
                            // updates the wait before the next playing
                            secTimerForAudio = (int)state;

                            // TODO: add usage of isEndOfFearOrExhausted here
                            PlaySFX(state, volume);
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
                        mls.LogDebug($"Oxygen consumption is increased by {DecreasingInFear}");
                        localDecValue += DecreasingInFear;

                        timeSinceLastFear = 0f;
                    }
                }
                timeSinceLastFear += Time.deltaTime;

                if (timeSinceLastAction >= SecTimer)
                { 
                    // support for Immersive visor
                    if (OxygenBase.Instance.IsImmersiveVisorFound && ImmersiveVisorSupport)
                    {
                        localDecValue += LogicForImmersiveVisor();
                    }

                    // if player running the oxygen goes away faster
                    if (pc.isSprinting)
                    {
                        localDecValue += OxygenDepletionWhileRunning;
                        mls.LogDebug($"The player is running, oxygen consumption is increased by {OxygenDepletionWhileRunning}");
                    }

                    // increasing drunkness
                    if (OxygenAmount < critical_OxygenAmount)
                    {
                        pc.drunkness = Mathf.Clamp01(pc.drunkness + OxygenDeficiency);
                        mls.LogDebug($"current oxygen deficiency level: {pc.drunkness}");
                    }

                    // it has to be before oxygen consumption underwater 
                    if (IsgreenPlanet && !pc.isInsideFactory)
                    {
                        mls.LogDebug("It's a green planet and you're outside, oxygen consumption is omitted!");
                        localDecValue = 0f;
                    }

                    if (pc.isUnderwater && pc.underwaterCollider != null && pc.underwaterCollider.bounds.Contains(pc.gameplayCamera.transform.position))
                    {
                        mls.LogDebug($"The player is underwater, oxygen consumption is increased by {OxygenDepletionInWater}");
                        localDecValue += OxygenDepletionInWater;

                        //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                    }

                    // 0.30 is the lowest value when we still see UI meter
                    if (OxygenAmount <= damage_OxygenAmount)
                    {
                        pc.DamagePlayer(PlayerDamage);  
                    }

                    // just for simplification if player was teleported and unable to refill oxygen
                    if (InfinityOxygenInModsPlaces && pc.serverPlayerPosition.y <= -480f) // -480f is Y offset 
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
                        OxygenAmount = OxygenAmount - localDecValue;
                        mls.LogDebug($"current oxygen level: {OxygenAmount}");

                        breathablePlace_Notification = false;
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
                    if (OxygenAmount != 1)
                    {
                        OxygenAmount = OxygenAmount + IncreasingOxygen;
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
