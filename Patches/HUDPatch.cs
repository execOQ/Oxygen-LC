using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch : MonoBehaviour
    {
        public static Image oxygenUI;

        public static bool instantiating = true;

        public static AudioClip[] inhaleSFX = OxygenBase.Instance.inhaleSFX;

        //public static bool instantiating => StartOfRoundPatch.instantiating;
        //public static bool updatingConfig = StartOfRoundPatch.updatingConfig;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        private static readonly bool isBackroomsFound = OxygenBase.Instance.isBackroomsFound;
        private const float backroomsOffset = -500f;

        // syncing
        public static bool backroomsCompatibility => Config.Instance.InfinityOxygenInbackrooms.Value;

        public static int playerDamage => Config.Instance.playerDamage.Value;

        public static float increasingOxygen => Config.Instance.increasingOxygen.Value;
        public static float decreasingOxygen => Config.Instance.decreasingOxygen.Value;
        public static float multiplyDecreasingInFear => Config.Instance.multiplyDecreasingInFear.Value;

        public static float oxygenDepletionWhileRunning => Config.Instance.oxygenRunning.Value;
        public static float oxygenDepletionInWater => Config.Instance.oxygenDepletionInWater.Value;

        public static float oxygenDeficiency => Config.Instance.oxygenDeficiency.Value;

        public static float secTimer => Config.Instance.secTimer.Value;  // number of seconds the cool down timer lasts
        //

        public static bool enableOxygenSFX => Config.Instance.enableOxygenSFX.Value;
        public static bool enableOxygenSFXInShip => Config.Instance.enableOxygenSFXInShip.Value;
        public static float volume => Config.Instance.SFXvolume.Value;

        public static float secTimerInFear = 2f;

        private static float timeSinceLastAction = 0f;  //number of seconds since we did something
        private static float timeSinceLastFear = 0f;  //number of seconds since we were fear

        public static bool isNotification => Config.Instance.notifications.Value;

        internal static bool backroomsNotification = false;
        internal static bool firstNotification = false;
        internal static bool warningNotification = false;

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        public static void UnInstantiate()
        {
            instantiating = true;
        }

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        public static void Init_oxyHUD()
        {
            if (instantiating)
            {
                GameObject sprintMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/SprintMeter");
                GameObject topLeftCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");

                if (sprintMeter == null || topLeftCorner == null)
                {
                    mls.LogError("oxygenMeter or oxyUI is null");
                    return;
                }

                GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCorner.transform);

                oxygenMeter.name = "OxygenMeter";
                oxygenMeter.transform.localPosition = new Vector3(-317.386f, 125.961f, -13.0994f);
                oxygenMeter.transform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);
                oxygenMeter.transform.localScale = new Vector3(2.0164f, 2.0018f, 1f);

                oxygenUI = oxygenMeter.transform.GetComponent<Image>();
                oxygenUI.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

                mls.LogInfo("Oxygen UI instantiated");

                GameObject statusEffectHUD = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/StatusEffects");
                if (statusEffectHUD == null)
                {
                    mls.LogError("statusEffectHUD is null");
                    return;
                }

                statusEffectHUD.transform.localPosition = new Vector3(20.1763f, -4.0355f, 0.0046f);
                //HUDManager.Instance.DisplayStatusEffect("Oxygen critically low!");

                mls.LogInfo("statusEffectHUD is fixed");

                mls.LogWarning($"config synced: {Config.Synced}");

                instantiating = false;
            }
        }

        private static void PlaySFX(PlayerControllerB pc, AudioClip clip)
        {
            AudioSource audio = pc.waterBubblesAudio;
            if (audio.isPlaying) audio.Stop();

            audio.PlayOneShot(clip, Random.Range(volume - 0.18f, volume));
        }


        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void Update()
        {
            if (instantiating)
            {
                mls.LogError("HUDPatch is still instantiating");
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

            if (oxygenUI == null)
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

            float localDecValue = decreasingOxygen;

            // can cause a problems with other mods (●'◡'●)
            if (!pController.isInsideFactory)
            {
                sor.drowningTimer = oxygenUI.fillAmount;
            }

            // i think it could cause a troubles if other mods teleport player in the same offset
            if (isBackroomsFound && backroomsCompatibility)
            {
                if (pController.serverPlayerPosition.y == backroomsOffset)
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
                            PlaySFX(pController, inhaleSFX[0]);
                        }
                    }

                    // just unnecessary to decrease oxygen in ship ~_~
                    if (!pController.isInHangarShipRoom)
                    {
                        mls.LogError("playing sound cause fearLevelIncreasing. oxygen consumption is increased by 2");
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
                        PlaySFX(pController, inhaleSFX[index]);
                    }
                }

                if (!pController.isInsideFactory && pController.isUnderwater && pController.underwaterCollider != null &&
                    pController.underwaterCollider.bounds.Contains(pController.gameplayCamera.transform.position))
                {
                    localDecValue += oxygenDepletionInWater;

                    //mls.LogInfo($"oxyUI.fillAmount: {oxyUI.fillAmount}");
                    //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                    return;
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

                    oxygenUI.fillAmount -= localDecValue;
                    mls.LogInfo($"current oxygen level: {oxygenUI.fillAmount}");
                }

                // inside factory
                if (pController.isInsideFactory)
                {
                    oxygenUI.fillAmount -= localDecValue;
                    mls.LogInfo($"current oxygen level: {oxygenUI.fillAmount}");
                }

                // notification about low level of oxygen
                if (oxygenUI.fillAmount < 0.45)
                {
                    if (!firstNotification) { 
                        if (isNotification)
                        {
                            HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                        }
                        firstNotification = true;
                    }
                }

                // system warning
                if (oxygenUI.fillAmount < 0.35)
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
                if (oxygenUI.fillAmount < 0.33)
                {
                    pController.drunkness += oxygenDeficiency;
                    mls.LogInfo($"current oxygen deficiency level: {pController.drunkness}");
                }

                // 0.30 is the lowest value when we see UI meter
                if (oxygenUI.fillAmount < 0.30)
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
                if (oxygenUI.fillAmount != 1) {
                    oxygenUI.fillAmount += increasingOxygen;
                    mls.LogInfo($"Oxygen is recovering: {oxygenUI.fillAmount}");
                }

                if (pController.drunkness != 0) pController.drunkness -= increasingOxygen;
            }
        }
    }
}
