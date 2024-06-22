using BepInEx.Logging;
using static System.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Oxygen.GameObjects;

namespace Oxygen
{
    public class OxygenInit : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxygenInit");

        private const float AccurateMinValue = 0.2978f;

        private const float AccurateMaxValue = 0.9101f;

        private const float AccurateValueRange = 0.6123f;

        public static bool IsOxygenHUDInitialized => oxygenHUD != null;

        public static float StaminaFillAmount => staminaHUD.fillAmount;

        // Elements
        private static Image staminaHUD;

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
                    if (OxygenBase.Instance.IsShyHUDFound && OxygenBase.OxygenConfig.shyHUDSupport)
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
            if (!OxygenBase.Instance.IsEladsHUDFound)
            {
                Init_vanilla();
            }
            else
            {
                Init_EladsHUD();
            }

            OxygenLogic.breathablePlace_Notification = false;
            OxygenLogic.lowLevel_Notification = false;
            OxygenLogic.criticalLevel_Notification = false;
            OxygenLogic.immersiveVisor_Notification = false;

            Percent = 1f;
            mls.LogInfo("Oxygen is initialized");
        }

        private static void Init_vanilla()
        {
            GameObject topLeftCornerUI = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");
            if (topLeftCornerUI == null)
            {
                mls.LogError("Init_vanilla: topLeftCornerUI is null");
                return;
            }

            GameObject sprintMeter = topLeftCornerUI.transform.Find("SprintMeter").gameObject;
            if (sprintMeter == null)
            {
                mls.LogError("Init_vanilla: sprintMeter is null");
                return;
            }
            staminaHUD = sprintMeter.transform.GetComponent<Image>();

            GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCornerUI.transform);

            oxygenMeter.name = "OxygenMeter";

            // а нах здесь transform?
            oxygenHUD = oxygenMeter.transform.GetComponent<Image>();
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

            GameObject statusEffectHUD = topLeftCornerUI.transform.Find("StatusEffects").gameObject;
            if (statusEffectHUD != null)
            {
                statusEffectHUD.transform.localPosition = new Vector3(20.1763f, -4.0355f, 0.0046f);
                //HUDManager.Instance.DisplayStatusEffect("Oxygen critically low!");

                mls.LogInfo("statusEffectHUD is fixed");
            }
        }

        private static void Init_EladsHUD()
        {
            GameObject topLeftCornerUI = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)");
            if (topLeftCornerUI == null)
            {
                mls.LogError("Init_EladsHUD: topLeftCornerUI is null");
                return;
            }

            GameObject sprintMeter = topLeftCornerUI.transform.Find("Stamina").gameObject;
            if (sprintMeter == null)
            {
                mls.LogError("Init_EladsHUD: sprintMeter is null");
                return;
            }
            staminaHUD = sprintMeter.transform.Find("Bar/StaminaBar").GetComponent<Image>();

            GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCornerUI.transform);
            oxygenMeter.name = "OxygenMeter";
            oxygenMeter.transform.localPosition = new Vector3(134.8f, -91.2254f, -2.8f);
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

            // adding divider between stamina info and oxygen info
            GameObject divider = Instantiate(oxygenInfo, oxygenMeter.transform);
            //divider.name = "Text Divider";
            divider.transform.localPosition = new Vector3(39.8443f, -6.4f, 2.8f);
            TextMeshProUGUI textDivider = divider.GetComponent<TextMeshProUGUI>();
            textDivider.text = "<size=50%><voffset=-3>•</voffset></size>";
            textDivider.color = new Color(r: 1, g: 1, b: 1, a: 0.37f);

            // just to not duplicating carryInfo :)
            Destroy(oxygenMeter.transform.Find("CarryInfo").gameObject);

            // nah, we're not needed in it :)))
            Destroy(oxygenMeter.transform.Find("Bar/Stamina Change FG").gameObject);

            // fixing position of stamina info
            Transform staminaMeterInfo = sprintMeter.transform.Find("StaminaInfo");
            staminaMeterInfo.localPosition = new Vector3(-0.2038f, -17.6235f, 2.7936f);

            // fixing position of carry info
            Transform carryInfo = sprintMeter.transform.Find("CarryInfo");
            carryInfo.localPosition = new Vector3(-0.0001f, -17.901f, 2.7999f);

            // fixing position of health
            Transform healthBar = topLeftCornerUI.transform.Find("Health");
            healthBar.localPosition = new Vector3(134.9906f, -178, 0.007f);
        }
    }
}
