using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class HUDPatch : MonoBehaviour
    {

        public static ManualLogSource mls = OxygenBase.mls;
        public static bool instantiating = true;

        private static readonly bool isBackroomsFound = OxygenBase.isBackroomsFound;
        public static bool backroomsCompatibility = OxygenBase.Config.InfinityOxygenInbackrooms.Value;
        private const float backroomsOffset = -500f;

        public static AudioClip[] inhaleSFX = OxygenBase.inhaleSFX;

        public static float volume = OxygenBase.Config.SFXvolume.Value;
        public static bool enableOxygenSFX = OxygenBase.Config.enableOxygenSFX.Value;
        public static bool enableOxygenSFXInShip = OxygenBase.Config.enableOxygenSFXInShip.Value;

        public static int playerDamage = OxygenBase.Config.playerDamage.Value;

        public static float increasingOxygen = OxygenBase.Config.increasingOxygen.Value; 
        public static float decreasingOxygen = OxygenBase.Config.decreasingOxygen.Value;
        public static float multiplyDecreasingInFear = OxygenBase.Config.multiplyDecreasingInFear.Value;

        public static float oxygenRunning = OxygenBase.Config.oxygenRunning.Value;

        public static float oxygenDeficiency = OxygenBase.Config.oxygenDeficiency.Value;

        public static float secTimer = OxygenBase.Config.secTimer.Value;  // number of seconds the cool down timer lasts
        public static float secTimerInFear = 2f;

        private static float timeSinceLastAction = 0f;  //number of seconds since we did something
        private static float timeSinceLastFear = 0f;  //number of seconds since we were fear

        public static bool isNotification = OxygenBase.Config.notifications.Value;
         
        //private static bool isRecovering = false; // just to prevent creating a lot logs about recovering oxygen when player in ship

        private static bool deadNotification = false;
        private static bool backroomsNotification = false;
        private static bool fisrtNotification = false;
        private static bool warningNotification = false;

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
               
                Image omImage = oxygenMeter.transform.GetComponent<Image>();
                omImage.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

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

                instantiating = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        public static void UnInstantiate()
        {
            instantiating = true;
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

            if (instantiating)
            {
                mls.LogError("HUDPatch is still instantiating");
                return;
            }

            GameObject oxygenMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/OxygenMeter");
            Image oxyUI = oxygenMeter.transform.GetComponent<Image>();

            if (oxygenMeter == null || oxyUI == null)
            {
                mls.LogError("oxygenMeter or oxyUI is null");
                return;
            }

            if (!pController.isPlayerDead)
            {
                float localDecValue = decreasingOxygen;

                if (deadNotification) deadNotification = false;

                if (pController.isUnderwater && pController.underwaterCollider != null && 
                    pController.underwaterCollider.bounds.Contains(pController.gameplayCamera.transform.position))
                {
                    // can cause a problems with other mods (●'◡'●)
                    oxyUI.fillAmount -= Mathf.Clamp(Time.deltaTime / 10f, 0f, 1f);

                    sor.drowningTimer = oxyUI.fillAmount;

                    //mls.LogInfo($"oxyUI.fillAmount: {oxyUI.fillAmount}");
                    //mls.LogInfo($"sor.drowningTimer: {sor.drowningTimer}");
                    return;
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
                                mls.LogInfo($"player in backrooms, oxygen is recovered.");
                                HUDManager.Instance.DisplayTip("System...", "Oxygen outside is breathable, oxygen supply through cylinders is turned off");
                            }
                            backroomsNotification = true;
                        }

                        if (oxyUI.fillAmount != 1)
                        {
                            oxyUI.fillAmount += increasingOxygen;
                            mls.LogInfo($"Oxygen is recovering: {oxyUI.fillAmount}");
                        }

                        if (pController.drunkness != 0) pController.drunkness -= increasingOxygen;

                        //oxyUI.fillAmount = 1f;
                        //mls.LogInfo($"player in backrooms, oxygen is recovered.");
                        //mls.LogInfo($"Contains: {Backrooms.Backrooms.Instance.playerInBackrooms.Contains(pController)}");
                        //mls.LogInfo($"isInHangarShipRoom: {pController.isInHangarShipRoom}");
                        //mls.LogInfo($"isInsideFactory: {pController.isInsideFactory}");
                        //mls.LogInfo($"isInElevator: {pController.isInElevator}");

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

                            localDecValue += decreasingOxygen * 2f;
                        }

                        timeSinceLastFear = 0f;
                    }
                }
                timeSinceLastFear += Time.deltaTime; //increment the cool down timer

                if (timeSinceLastAction >= secTimer)
                {
                    if (inhaleSFX == null)
                    {
                        mls.LogError("inhalerSFX is null");
                        return;
                    }

                    if (enableOxygenSFX && !sor.fearLevelIncreasing)
                    {
                        if (!pController.isInHangarShipRoom || (pController.isInHangarShipRoom && enableOxygenSFXInShip))
                        {
                            int index = Random.Range(0, inhaleSFX.Length);
                            PlaySFX(pController, inhaleSFX[index]);
                        }
                    }

                    // if player running the oxygen goes away faster
                    if (pController.isSprinting)
                    {
                        localDecValue += oxygenRunning;
                        mls.LogInfo($"The player is running, oxygen consumption is increased by {oxygenRunning}");
                    }

                    // outside and not in ship
                    if (!pController.isInsideFactory && !pController.isInHangarShipRoom)
                    {
                        //isRecovering = false; // just to prevent creating a lot logs about recovering oxygen when player in ship

                        oxyUI.fillAmount -= localDecValue;
                        mls.LogInfo($"current oxygen level: {oxyUI.fillAmount}");
                    }

                    // inside factory
                    if (pController.isInsideFactory)
                    {
                        oxyUI.fillAmount -= localDecValue;
                        mls.LogInfo($"current oxygen level: {oxyUI.fillAmount}");
                    }

                    // notification about low level of oxygen
                    if (oxyUI.fillAmount < 0.45)
                    {
                        if (!fisrtNotification) { 
                            if (isNotification)
                            {
                                HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                            }
                            fisrtNotification = true;
                        }
                    }

                    // system warning
                    if (oxyUI.fillAmount < 0.35)
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
                    if (oxyUI.fillAmount < 0.33)
                    {
                        pController.drunkness += oxygenDeficiency;
                        mls.LogInfo($"current oxygen deficiency level: {pController.drunkness}");
                    }

                    // 0.30 is the lowest value when we see UI meter
                    if (oxyUI.fillAmount < 0.30)
                    {
                        pController.DamagePlayer(playerDamage);
                    }

                    // timer resets
                    timeSinceLastAction = 0f;
                }

                // in ship
                if (pController.isInHangarShipRoom)
                {
                    if (oxyUI.fillAmount != 1) {
                        oxyUI.fillAmount += increasingOxygen;
                        mls.LogInfo($"Oxygen is recovering: {oxyUI.fillAmount}");
                    }

                    if (pController.drunkness != 0) pController.drunkness -= increasingOxygen;
                }

                timeSinceLastAction += Time.deltaTime; //increment the cool down timer
            } else
            {
                if (!deadNotification)
                {
                    oxyUI.fillAmount = 1;
                    pController.drunkness = 0;
                    mls.LogInfo("Player is dead, oxygen recovered to 1");

                    // to prevent spamming the message above
                    deadNotification = true;

                    // resets notifications
                    backroomsNotification = false;
                    fisrtNotification = false;
                    warningNotification = false;
                }
            }
            
        }
    }
}
