using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxygen.General
{
    internal class DieEarly : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > DieEarly");

        private static TextMeshProUGUI[] controlTips;

        private static TextMeshProUGUI dieEarlyText;
        private static Image dieEarlyMeter;
        private static Image dieEarlyMeterFrame;

        public static bool isDieEarlyUIEnabled = false;
        public static float dieEarlyMeterFillAmount => dieEarlyMeter.fillAmount;

        internal static void Init()
        {
            GameObject endGameEarlyUI = GameObject.Find("Systems/UI/Canvas/DeathScreen/SpectateUI/EndGameEarly");
            GameObject topRightCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopRightCorner");

            if (endGameEarlyUI == null || topRightCorner == null)
            {
                mls.LogError("endGameEarlyUI or topRightCorner is null");
                return;
            }

            GameObject dieEarlyUI = Instantiate(endGameEarlyUI, topRightCorner.transform);
            dieEarlyUI.name = "DieEarly";
            dieEarlyUI.transform.localPosition = new Vector3(-378f, -196f, 0f);
            dieEarlyUI.transform.rotation = Quaternion.Euler(0f, 16f, 0f);
            dieEarlyUI.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);


            // That's unnecessary
            Destroy(dieEarlyUI.transform.Find("Votes").gameObject);

            dieEarlyText = dieEarlyUI.transform.Find("Header").GetComponent<TextMeshProUGUI>();
            dieEarlyText.text = $"Would you like to die earlier? ;) : [{OxygenBase.InputActionsInstance.DieEarlyButton.bindings[0].ToDisplayString()}] (Hold)";
            
            dieEarlyMeter = dieEarlyUI.transform.Find("Meter/Fill").GetComponent<Image>();
            dieEarlyMeterFrame = dieEarlyUI.transform.Find("Meter/Fill/Frame").GetComponent<Image>();

            controlTips = [
                topRightCorner.transform.Find("ControlTip1").gameObject.GetComponent<TextMeshProUGUI>(),
                topRightCorner.transform.Find("ControlTip2").gameObject.GetComponent<TextMeshProUGUI>(),
                topRightCorner.transform.Find("ControlTip3").gameObject.GetComponent<TextMeshProUGUI>(),
                topRightCorner.transform.Find("ControlTip4").gameObject.GetComponent<TextMeshProUGUI>()
            ];

            DisplayDieEarlyMeter(false);
            SetDieEarlyMeterValue(0f);

            mls.LogMessage("DieEarly initialized");
        }

        internal static void SetDieEarlyMeterValue(float value)
        {
            if (dieEarlyMeter == null)
            {
                return;
            }

            if (dieEarlyMeter.fillAmount != value)
            {
                dieEarlyMeter.fillAmount = value;
            }
        }

        internal static void DisplayDieEarlyMeter(bool value)
        {
            if (dieEarlyText == null || dieEarlyMeter == null)
            {
                mls.LogError("dieEarlyText or dieEarlyMeter is null");
                return;
            }

            mls.LogDebug($"DisplayDieEarlyMeter: {value}");
            if (value)
            {
                dieEarlyText.CrossFadeAlpha(1f, 2f, ignoreTimeScale: false);
                dieEarlyMeter.CrossFadeAlpha(1f, 2f, ignoreTimeScale: false);
                dieEarlyMeterFrame.CrossFadeAlpha(1f, 2f, ignoreTimeScale: false);

                foreach (TextMeshProUGUI tip in controlTips)
                {
                    tip.CrossFadeAlpha(0f, 0.5f, ignoreTimeScale: false);
                }
            }
            else
            {
                dieEarlyText.CrossFadeAlpha(0f, 0.5f, ignoreTimeScale: false);
                dieEarlyMeter.CrossFadeAlpha(0f, 0.5f, ignoreTimeScale: false);
                dieEarlyMeterFrame.CrossFadeAlpha(0f, 0.5f, ignoreTimeScale: false);

                foreach (TextMeshProUGUI tip in controlTips)
                {
                    tip.CrossFadeAlpha(1f, 2f, ignoreTimeScale: false);
                }
            }
            isDieEarlyUIEnabled = value;
        }
    }
}
