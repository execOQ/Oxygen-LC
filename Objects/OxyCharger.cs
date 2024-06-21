using BepInEx.Logging;
using UnityEngine;

namespace Oxygen.Items
{
    internal class OxyCharger : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxyCharger");
 
        private static float Volume => OxygenBase.OxygenConfig.oxyCharger_SFXVolume.Value;

        private static AudioClip[] OxyChargerSFX => OxygenBase.Instance.oxyChargerSFX;

        internal static AudioSource audioSource;

        public static void FillOxygen()
        {
            OxygenInit.Percent = 1f;

            PlaySFX(OxyChargerSFX[Random.Range(0, OxyChargerSFX.Length)]);

            mls.LogInfo("Oxygen was recovered");
        }

        private static void PlaySFX(AudioClip clip)
        {
            if (audioSource == null)
            {
                mls.LogError("Audio source was not found");
            }

            if (audioSource.isPlaying) audioSource.Stop();

            audioSource.PlayOneShot(clip, Volume);
        }

        internal static void Init()
        {
            GameObject suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
            if (suitParts == null)
            {
                mls.LogError("suitParts are null");
                return;
            }
            GameObject oxyCylinders = suitParts.transform.Find("Circle.002").gameObject;

            oxyCylinders.SetActive(false);
            OxygenBase.Instance.oxyCharger.transform.rotation = oxyCylinders.transform.rotation;

            GameObject oxyCharger = Instantiate(OxygenBase.Instance.oxyCharger, suitParts.transform);
            oxyCharger.transform.position = new Vector3(5.9905f, 0.7598f, -11.2452f);

            if (OxygenBase.OxygenConfig.oxygenFillOption.Value != 1)
            {
                for (int i = oxyCharger.transform.childCount - 1; i >= 0; i--)
                {
                    mls.LogInfo($"{oxyCharger.transform.GetChild(i).gameObject.tag}");

                    if (oxyCharger.transform.GetChild(i).gameObject.tag == "InteractTrigger")
                    {
                        Destroy(oxyCharger.transform.GetChild(i).gameObject);
                        mls.LogInfo("InteractTrigger was deleted");
                    }
                }
            }

            audioSource = oxyCharger.GetComponent<AudioSource>();

            mls.LogInfo("Oxygen cylinders were replaced");
        }
    }
}
