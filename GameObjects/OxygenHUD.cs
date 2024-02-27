using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace Oxygen
{
    internal class OxygenHUD : MonoBehaviour
    {
        public static Image oxygenUI;

        public static bool initialized = false;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public static float volume => OxygenBase.Config.SFXvolume.Value;

        public static bool diedBecauseOfOxygen = false;

        public static void Init()
        {
            if (!initialized)
            {
                GameObject sprintMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/SprintMeter");
                GameObject topLeftCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");

                if (sprintMeter == null || topLeftCorner == null)
                {
                    mls.LogError("oxygenMeter or topLeftCorner is null");
                    return;
                }

                GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCorner.transform);

                oxygenMeter.name = "OxygenMeter";
                oxygenMeter.transform.localPosition = OxygenBase.Config.OxygenHUDPosition.Value;
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

                initialized = true;
            }
        }

        internal static void PlaySFX(PlayerControllerB pc, AudioClip clip)
        {
            AudioSource audio = pc.waterBubblesAudio;
            if (audio.isPlaying) audio.Stop();

            audio.PlayOneShot(clip, Random.Range(volume - 0.18f, volume));
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        public static void UnInstantiate()
        {
            initialized = false;
        }
    }
}
