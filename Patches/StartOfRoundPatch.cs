using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using Oxygen.Items;
using UnityEngine;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class StartOfRoundPatch : MonoBehaviour
    {
        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public static int oxygenFillOption => OxygenConfig.Instance.OxygenFillOption.Value;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        private static void ShipHasLeft_Patch()
        {
            OxygenHUD.oxygenUI.fillAmount = 1f;
            mls.LogInfo("Ship has left, oxygen was recovered >.<");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        private static void Patch_RoundAwake()
        {
            GameObject OxyCylinders = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts/Circle.002");
            GameObject suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");

            OxyCylinders.SetActive(false);
            OxygenBase.Instance.oxyCharger.transform.rotation = OxyCylinders.transform.rotation;

            GameObject oxyCharger = Instantiate(OxygenBase.Instance.oxyCharger, suitParts.transform);
            oxyCharger.transform.position = new Vector3(5.9905f, 0.7598f, -11.2452f);

            if (oxygenFillOption != 1)
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

            OxyCharger.audioSource = oxyCharger.GetComponent<AudioSource>();

            mls.LogInfo("Oxygen cylinders were replaced");
        }
    }
}
