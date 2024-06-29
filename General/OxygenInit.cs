using BepInEx.Logging;
using static System.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Oxygen.GameObjects;
using System.Collections;
using LCVR.Player;
using DunGen;

namespace Oxygen
{
    public class OxygenInit : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenInit");

        private const float AccurateMinValue = 0.2978f;

        private const float AccurateMaxValue = 0.9101f;

        private const float AccurateValueRange = 0.6123f;

        public static bool IsOxygenHUDInitialized => oxygenHUD != null;

        public static float StaminaFillAmount => sprintMeterImage.fillAmount;

        // Elements
        private static Image sprintMeterImage;

        private static Image oxygenHUD;

        private static TextMeshProUGUI eladsUIText;

        // taken from DramaMask mod (https://github.com/Henit3/DramaMask/blob/main/src/UI/StealthMeter.cs)
        public static float Percent
        {
            get
            {
                if (oxygenHUD == null)
                {
                    return 0f;
                }
                return OxygenBase.Instance.IsEladsHUDFound ? oxygenHUD.fillAmount : InvAdjustFillAmount(oxygenHUD.fillAmount);
            }
            internal set
            {
                if (oxygenHUD == null)
                {
                    return;
                }
                float adjustedFillAmount = Mathf.Clamp01(OxygenBase.Instance.IsEladsHUDFound ? value : AdjustFillAmount(value));
                if (oxygenHUD.fillAmount != adjustedFillAmount)
                {
                    oxygenHUD.fillAmount = adjustedFillAmount;
                    if (eladsUIText != null)
                    {
                        float roundedValue = (float)Round(Percent, 2);
                        int oxygenInPercent = (int)(roundedValue * 100f);
                        eladsUIText.text = $"{oxygenInPercent}<size=75%><voffset=1>%</voffset></size>";
                    }
                    if (OxygenBase.Instance.IsShyHUDFound && OxygenBase.OxygenConfig.shyHUDSupport.Value)
                    {
                        bool toFadeOut = value >= 0.75f; // previously was 0.55f
                        oxygenHUD.CrossFadeAlpha(toFadeOut ? 0f : 1f, toFadeOut ? 5f : 0.5f, ignoreTimeScale: false);
                    }
                }
            }
        }

        private static float AdjustFillAmount(float value)
        {
            return OxygenBase.OxygenConfig.accurateMeter.Value ? (value * AccurateValueRange + AccurateMinValue) : value;
        }

        private static float InvAdjustFillAmount(float value)
        {
            return OxygenBase.OxygenConfig.accurateMeter.Value ? ((value - AccurateMinValue) / AccurateValueRange) : value;
        }

        internal static void Init()
        {
            GameObject sprintMeter = OxygenBase.Instance.IsEladsHUDFound ?
                        GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/Stamina") : GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/SprintMeter");

            if (sprintMeter == null)
            {
                mls.LogError("sprintMeter is null");
                return;
            }

            sprintMeterImage = OxygenBase.Instance.IsEladsHUDFound ? sprintMeter.transform.Find("Bar/StaminaBar").GetComponent<Image>() : sprintMeter.GetComponent<Image>();

            if (!OxygenBase.Instance.IsEladsHUDFound)
            {
                Init_vanilla(sprintMeter);

                if (OxygenBase.Instance.IsLCVRFound)
                {
                    CoroutineHelper.Start(Init_LCVR(sprintMeter));
                }
            }
            else
            {
                Init_EladsHUD(sprintMeter);
            }

            OxygenLogic.ResetAllNotifications();

            Percent = 1f;
            mls.LogInfo("Oxygen is initialized");
        }

        private static void Init_vanilla(GameObject sprintMeter)
        {
            Transform sprintMeterParent = sprintMeter.transform.parent;

            GameObject oxygenMeter = Instantiate(sprintMeter, sprintMeterParent);
            oxygenMeter.name = "OxygenMeter";

            oxygenHUD = oxygenMeter.GetComponent<Image>();
            oxygenHUD.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

            RectTransform rectTransform = oxygenMeter.GetComponent<RectTransform>();

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            int valueX = OxygenBase.OxygenConfig.XOffset.Value;
            int valueY = OxygenBase.OxygenConfig.YOffset.Value;

            rectTransform.anchoredPosition = new Vector2((float)(131.6 + valueX), (float)(-127.3715 + valueY));
            rectTransform.localScale = new Vector3(2.0392f, 2.0392f, 1.6892f);
            rectTransform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);

            // fixes overlapping
            sprintMeterParent.Find("StatusEffects").localPosition = new Vector3(20.1763f, -4.0355f, 0.0046f);
            sprintMeterParent.Find("WeightUI").localPosition = new Vector3(-270f, 83f, 17f);

            mls.LogInfo(OxygenBase.OxygenConfig.autoFillingOnShip_increasingOxygen);
        }

        private static void Init_EladsHUD(GameObject sprintMeter)
        {
            Transform sprintMeterParent = sprintMeter.transform.parent;

            GameObject oxygenMeter = Instantiate(sprintMeter, sprintMeterParent);
            oxygenMeter.name = "OxygenMeter";
            oxygenMeter.transform.localPosition = new Vector3(135f, -91.2254f, 0f);
            //oxygenMeter.transform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);
            //oxygenMeter.transform.localScale = new Vector3(2.0164f, 2.0018f, 1f);

            // oxygenHUD
            oxygenHUD = oxygenMeter.transform.Find("Bar/StaminaBar").GetComponent<Image>();
            oxygenHUD.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);
            //oxygenUI.fillAmount = 1f;

            // HUD's text field
            GameObject oxygenInfo = oxygenMeter.transform.Find("StaminaInfo").gameObject;
            oxygenInfo.transform.localPosition = new Vector3(45.4834f, -6.4f, 2.8f);
            eladsUIText = oxygenInfo.GetComponent<TextMeshProUGUI>();
            eladsUIText.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

            // divider between stamina info and oxygen info
            GameObject divider = Instantiate(oxygenInfo, oxygenMeter.transform);
            divider.transform.localPosition = new Vector3(39.2f, -6.4f, 2.8f);

            // renaming breaks the object, idk why
            //divider.name = "Text Divider";

            TextMeshProUGUI textDivider = divider.GetComponent<TextMeshProUGUI>();
            textDivider.text = "<size=50%><voffset=-3>•</voffset></size>";
            textDivider.color = new Color(r: 1, g: 1, b: 1, a: 0.37f);

            // just to not duplicate carryInfo :)
            Destroy(oxygenMeter.transform.Find("CarryInfo").gameObject);

            // nah, we're not needed in it :)))
            Destroy(oxygenMeter.transform.Find("Bar/Stamina Change FG").gameObject);

            // fixes overlapping
            sprintMeter.transform.Find("StaminaInfo").localPosition = new Vector3(0f, -15.4f, 0f);
            sprintMeter.transform.Find("CarryInfo").localPosition = new Vector3(0f, -15.4f, 0f);
            sprintMeterParent.Find("Health").localPosition = new Vector3(135f, -160, 0f);
        }

        private static IEnumerator Init_LCVR(GameObject sprintMeter)
        {
            if (!VRSession.InVR)
            {
                yield break; 
            }

            if (oxygenHUD == null)
            {
                mls.LogError("Init_LCVR: oxygenHUD is null, WTF...");
                yield break;
            }

            // Wait until VR Instance exists
            yield return new WaitUntil(() => VRSession.Instance != null);

            oxygenHUD.transform.SetParent(sprintMeter.transform.parent, false);
            
            if (LCVR.Plugin.Config.DisableArmHUD.Value)
            {
                oxygenHUD.transform.localPosition = new Vector3(-279.0514f, 74.9563f, 0);
                oxygenHUD.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                oxygenHUD.transform.localScale = new Vector3(2.55f, 2.5f, 2.5f);

                sprintMeter.transform.parent.Find("WeightUI").localPosition = new Vector3(-171f, 25f, 0f);
            } else
            {
                oxygenHUD.transform.localPosition = new Vector3(-45f, 95f, 70f);
                oxygenHUD.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                oxygenHUD.transform.localScale = new Vector3(1.35f, 1.25f, 1f);

                sprintMeter.transform.parent.Find("WeightUI").localPosition = new Vector3(-50f, 60f, 67f);
            }

            mls.LogInfo("OxygenHUD initialized (VR)");
        }

        internal static void Init_OxyCharger()
        {
            if (OxygenBase.OxygenConfig.oxyCharger_Enabled.Value)
            {
                GameObject suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
                if (suitParts == null)
                {
                    mls.LogError("suitParts are null");
                    return;
                }

                GameObject oxyCylinders = suitParts.transform.Find("Circle.002").gameObject;

                GameObject go = Instantiate(OxygenBase.Instance.oxyCharger, suitParts.transform);
                go.transform.rotation = oxyCylinders.transform.rotation;
                go.transform.position = new Vector3(5.9905f, 0.7598f, -11.2452f);

                Destroy(oxyCylinders);
            }
        }
    }
}
