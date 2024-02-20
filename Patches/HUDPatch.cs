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

        public static AudioClip[] inhalerSFX = OxygenBase.SFX;

        public static bool enableOxygenSFX = OxygenBase.Config.enableOxygenSFX.Value;
        public static bool enableOxygenSFXInShip = OxygenBase.Config.enableOxygenSFXInShip.Value;

        public static int playerDamage = OxygenBase.Config.playerDamage.Value;

        public static float increasingOxygen = OxygenBase.Config.increasingOxygen.Value; 
        public static float decreasingOxygen = OxygenBase.Config.decreasingOxygen.Value;

        public static float oxygenRunning = OxygenBase.Config.oxygenRunning.Value;

        public static float oxygenDeficiency = OxygenBase.Config.oxygenDeficiency.Value;

        public static float secTimer = OxygenBase.Config.secTimer.Value;  // number of seconds the cool down timer lasts

        private static float timeSinceLastAction = 0f;  //number of seconds since we did something

        public static bool isNotification = OxygenBase.Config.notifications.Value;
         
        //private static bool isRecovering = false; // just to prevent creating a lot logs about recovering oxygen when player in ship

        private static bool deadNotification = false;
        private static bool fisrtNotification = false;
        private static bool warningNotification = false;



        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        public static void Init_oxyHUD()
        {
            if (instantiating)
            { 
                GameObject sprintMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/SprintMeter");
                GameObject uiPlace = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");

                if (sprintMeter == null || uiPlace == null)
                {
                    mls.LogInfo("One or more GameObjects not found...");
                    return;
                }
                
                GameObject oxygenMeter = Instantiate(sprintMeter, uiPlace.transform);

                oxygenMeter.name = "OxygenMeter";
                oxygenMeter.transform.localPosition = new Vector3(-317.386f, 125.961f, -13.0994f);
                oxygenMeter.transform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);
                oxygenMeter.transform.localScale = new Vector3(2.0164f, 2.0018f, 1f);
               
                Image omImage = oxygenMeter.transform.GetComponent<Image>();
                omImage.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

                instantiating = false;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        public static void UnInstantiate()
        {
            instantiating = true;
        }

        private static void PlayOxygenInhailsSFX(PlayerControllerB pc) 
        {
            AudioSource audio = pc.waterBubblesAudio;
            float oneShotVolume = 1f;

            if (audio.isPlaying) return;

            int index = Random.Range(0, inhalerSFX.Length);
            audio.PlayOneShot(inhalerSFX[index], Random.Range(oneShotVolume - 0.18f, oneShotVolume));
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void Update()
        {
            PlayerControllerB pController = GameNetworkManager.Instance.localPlayerController;

            //StartOfRound sor = StartOfRound.Instance;

            if (pController == null || instantiating)
            {
                mls.LogError("PlayerControllerB is null or HUDPatch is still instantiating");
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
                if (deadNotification) deadNotification = false;

                // Problem: there is something with variable sinkingValue, it doesn't update in any case.
                // it's should increasing when player underwater. so here's a crutch... or maybe i'm dumb :)

                // uses if player is sinking
                /* if (pController.isUnderwater)
                {
                    mls.LogError($"sinking: {pController.sinkingValue}");

                    oxyUI.fillAmount -= 0.0020f;
                    mls.LogInfo($"{oxyUI.fillAmount}");
                    return;
                } */

                // need to be reworked cause it's doesn't sound as I expected... it should sounds more panicked
                /* if (sor.fearLevelIncreasing)
                {
                    PlayOxygenInhailsSFX(pController);
                    mls.LogError("playing sound cause fearLevelIncreasing");
                } */

                if (timeSinceLastAction >= secTimer)
                {
                    float localDecValue = decreasingOxygen;

                    if (inhalerSFX == null)
                    {
                        mls.LogError("inhalerSFX is null");
                        return;
                    }

                    if (enableOxygenSFX && !pController.isInHangarShipRoom)
                    { 
                        PlayOxygenInhailsSFX(pController);
                    }

                    if (pController.isInHangarShipRoom && enableOxygenSFX && enableOxygenSFXInShip)
                    {
                        PlayOxygenInhailsSFX(pController);
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

                //sor.fearLevelIncreasing = false;

                // in ship
                if (pController.isInHangarShipRoom && oxyUI.fillAmount != 1)
                {
                    oxyUI.fillAmount += increasingOxygen;

                    mls.LogInfo($"Oxygen is recovering: {oxyUI.fillAmount}");

                    pController.drunkness -= increasingOxygen;
                }

                timeSinceLastAction += Time.deltaTime; //increment the cool down timer
            } else
            {
                if (!deadNotification)
                {
                    oxyUI.fillAmount = 1;
                    mls.LogInfo("Player is dead, oxygen recovered to 1");

                    deadNotification = true;
                    fisrtNotification = false;
                    warningNotification = false;
                }
            }
            
        }
    }
}
