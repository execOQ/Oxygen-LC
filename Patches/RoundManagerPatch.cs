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
            MoonsDicts.increasingOxygenMoons = GetLevelValue(OxygenBase.OxygenConfig.increasingOxygenMoons.Value, OxygenBase.OxygenConfig.increasingOxygen.Value);

            MoonsDicts.decreasingOxygenOutsideMoons = GetLevelValue(OxygenBase.OxygenConfig.decreasingOxygenOutsideMoons.Value, OxygenBase.OxygenConfig.decreasingOxygenOutside.Value);
            
            MoonsDicts.decreasingOxygenInFactoryMoons = GetLevelValue(OxygenBase.OxygenConfig.decreasingOxygenInFactoryMoons.Value, OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value);

            MoonsDicts.decreasingInFearMoons = GetLevelValue(OxygenBase.OxygenConfig.decreasingInFearMoons.Value, OxygenBase.OxygenConfig.decreasingInFear.Value);

            MoonsDicts.oxygenRunningMoons = GetLevelValue(OxygenBase.OxygenConfig.oxygenRunningMoons.Value, OxygenBase.OxygenConfig.oxygenRunning.Value);

            MoonsDicts.oxygenDepletionInWaterMoons = GetLevelValue(OxygenBase.OxygenConfig.oxygenDepletionInWaterMoons.Value, OxygenBase.OxygenConfig.oxygenDepletionInWater.Value);
        }

        private static Dictionary<string, float> GetLevelValue(string str, float defValue)
        {
            Dictionary<string, float> result = [];

            // for simplification my work...
            result.Add("default", defValue);

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
