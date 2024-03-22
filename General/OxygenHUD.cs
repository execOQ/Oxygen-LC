using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using static System.Math;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oxygen
{
    public class OxygenHUD : MonoBehaviour
    {
        public static Image oxygenUI;

        public static TextMeshProUGUI EladsOxygenUIText;

        public static bool initialized = false;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public static float Volume => OxygenBase.OxygenConfig.SFXvolume.Value;

        public static bool diedBecauseOfOxygen = false;

        public static bool IsNotification => OxygenBase.OxygenConfig.notifications.Value;
        internal static bool backroomsNotification = false;
        internal static bool firstNotification = false;
        internal static bool warningNotification = false;

        public static void Init()
        {
            if (!initialized)
            {
                if (!OxygenBase.Instance.isEladsHUDFound)
                {
                    Init_vanilla();
                } 
                else 
                {
                    Init_ElandHUD();
                }

                mls.LogWarning($"config synced: {OxygenConfig.Synced}");

                initialized = true;
            }
        }

        private static void Init_vanilla()
        {
            GameObject sprintMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/SprintMeter");
            GameObject topLeftCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner");

            if (sprintMeter == null || topLeftCorner == null)
            {
                mls.LogError("Init_vanilla / oxygenMeter or topLeftCorner is null");
                return;
            }

            GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCorner.transform);

            oxygenMeter.name = "OxygenMeter";

            oxygenUI = oxygenMeter.transform.GetComponent<Image>();
            oxygenUI.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);

            RectTransform rectTransform = oxygenMeter.GetComponent<RectTransform>();

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            int valueX = OxygenBase.OxygenConfig.XOffset.Value;
            int valueY = OxygenBase.OxygenConfig.YOffset.Value;

            rectTransform.anchoredPosition = new Vector2((float)(131.6 + valueX), (float)(-127.3715 + valueY));
            rectTransform.localScale = new Vector3(2.0392f, 2.0392f, 1.6892f);

            rectTransform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);

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
        }

        private static void Init_ElandHUD()
        {
            GameObject sprintMeter = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/Stamina");
            GameObject topLeftCorner = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)");

            if (sprintMeter == null || topLeftCorner == null)
            {
                mls.LogError("Init_ElandHUD / oxygenMeter or topLeftCorner is null");
                return;
            }

            GameObject oxygenMeter = Instantiate(sprintMeter, topLeftCorner.transform);

            oxygenMeter.name = "OxygenMeter";
            oxygenMeter.transform.localPosition = new Vector3(135, -115, -2.8f);
            //oxygenMeter.transform.rotation = Quaternion.Euler(0f, 323.3253f, 0f);
            //oxygenMeter.transform.localScale = new Vector3(2.0164f, 2.0018f, 1f);
            
            // hm just to not duplicating)
            GameObject carryInfo = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/OxygenMeter/CarryInfo");
            Destroy(carryInfo);

            // we're not needed in it)))
            GameObject staminaChangeFG = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/OxygenMeter/Bar/Stamina Change FG");
            Destroy(staminaChangeFG);

            //Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/Stamina/Bar/
            GameObject staminaBar = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/OxygenMeter/Bar/StaminaBar");
            oxygenUI = staminaBar.transform.GetComponent<Image>();

            // fixing position of health
            GameObject healthBar = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/Health");
            healthBar.transform.localPosition = new Vector3(134.9906f, -178, 0.007f);

            // getting text field
            GameObject oxygenInfo = GameObject.Find("Systems/UI/Canvas/IngamePlayerHUD/PlayerInfo(Clone)/OxygenMeter/StaminaInfo");
            EladsOxygenUIText = oxygenInfo.GetComponent<TextMeshProUGUI>();

            oxygenUI.color = new Color(r: 0.593f, g: 0.667f, b: 1, a: 1);
            oxygenUI.fillAmount = 1f;

            mls.LogInfo("Oxygen UI instantiated");
        }

        public static void UpdateModsCompatibility()
        {
            if (EladsOxygenUIText != null)
            {
                float roundedValue = (float)Round(oxygenUI.fillAmount, 2);
                int oxygenInPercent = (int)(roundedValue * 100);

                EladsOxygenUIText.text = $"{oxygenInPercent}<size=75%><voffset=1>%</voffset></size>";
            }

            if (OxygenBase.Instance.isShyHUDFound && OxygenConfig.Instance.ShyHUDSupport)
            {
                if (oxygenUI.fillAmount >= 0.55f)
                {
                    oxygenUI.CrossFadeAlpha(0f, 5f, ignoreTimeScale: false);
                }
                else
                {
                    oxygenUI.CrossFadeAlpha(1f, 0.5f, ignoreTimeScale: false);
                }
            }
        }

        public static void ShowNotifications()
        {
            if (IsNotification)
            {
                // notification about low level of oxygen
                if (oxygenUI.fillAmount < 0.45 && !firstNotification)
                {
                    HUDManager.Instance.DisplayTip("System...", "The oxygen tanks are running low.");
                    firstNotification = true;
                }

                // system warning
                if (oxygenUI.fillAmount < 0.35 && !warningNotification)
                {
                    HUDManager.Instance.DisplayTip(
                        "System...",
                        "There is a critical level of oxygen in the oxygen tanks, fill it up immediately!",
                        isWarning: true
                    );
                    warningNotification = true;
                }
            }
        }

        // who cares how it's called...?
        public static void ShowAnotherNotification()
        {
            if (!backroomsNotification)
            {
                if (IsNotification)
                {
                    HUDManager.Instance.DisplayTip("System...", "Oxygen outside is breathable, oxygen supply through cylinders is turned off");
                }
                backroomsNotification = true;
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "Disconnect")]
        [HarmonyPrefix]
        public static void UnInitialize()
        {
            initialized = false;
        }
    }
}
