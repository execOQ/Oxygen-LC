using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using System.Collections.Generic;

namespace Oxygen.Patches
{
    internal class RoundManagerPatch
    {
        public static ManualLogSource mls = OxygenBase.Instance.mls;

        [HarmonyPriority(0)]
        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        private static void RoundManagerPatch_Postfix()
        {

            MoonsDicts.increasingOxygenMoons = GetLevelValue(OxygenBase.oxygenConfig.increasingOxygenMoons.Value, OxygenBase.oxygenConfig.increasingOxygen.Value);

            MoonsDicts.decreasingOxygenMoons = GetLevelValue(OxygenBase.oxygenConfig.decreasingOxygenMoons.Value, OxygenBase.oxygenConfig.decreasingOxygen.Value);

            MoonsDicts.decreasingInFearMoons = GetLevelValue(OxygenBase.oxygenConfig.decreasingInFearMoons.Value, OxygenBase.oxygenConfig.decreasingInFear.Value);

            MoonsDicts.oxygenRunningMoons = GetLevelValue(OxygenBase.oxygenConfig.oxygenRunningMoons.Value, OxygenBase.oxygenConfig.oxygenRunning.Value);

            MoonsDicts.oxygenDepletionInWaterMoons = GetLevelValue(OxygenBase.oxygenConfig.oxygenDepletionInWaterMoons.Value, OxygenBase.oxygenConfig.oxygenDepletionInWater.Value);
        }

        private static Dictionary<string, float> GetLevelValue(string str, float defValue)
        {
            Dictionary<string, float> result = [];

            if (string.IsNullOrEmpty(str))
            {
                return result;
            }

            str = str.Replace(" ", string.Empty);

            foreach (string x in str.Split(','))
            {
                if (!x.Contains(":"))
                {
                    result.Add(x.ToLower().Replace(" ", string.Empty), defValue);
                }
                else
                {
                    string[] array = x.Split(':');
                    if (float.TryParse(array[1], out var value))
                    {
                        mls.LogMessage($"{array[0].ToLower().Replace(" ", string.Empty)}, {value}");
                        result.Add(array[0].ToLower().Replace(" ", string.Empty), value);
                    }
                    else
                    {
                        mls.LogError($"Failed to parse value, using default one... Check the syntax!");
                        mls.LogMessage($"{array[0].ToLower().Replace(" ", string.Empty)}, {defValue}");
                        result.Add(array[0].ToLower().Replace(" ", string.Empty), defValue);
                    }
                }
            }

            return result;
        }

    }
}
