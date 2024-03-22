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
            MoonsDicts.greenPlanets = GetLevelValue(OxygenBase.OxygenConfig.greenPlanets.Value, 0);

            MoonsDicts.increasingOxygenMoons = GetLevelValue(OxygenBase.OxygenConfig.increasingOxygenMoons.Value, OxygenBase.OxygenConfig.increasingOxygen.Value);

            MoonsDicts.decreasingOxygenOutsideMoons = GetLevelValue(OxygenBase.OxygenConfig.decreasingOxygenOutsideMoons.Value, OxygenBase.OxygenConfig.decreasingOxygenOutside.Value);
            
            MoonsDicts.decreasingOxygenInFactoryMoons = GetLevelValue(OxygenBase.OxygenConfig.decreasingOxygenInFactoryMoons.Value, OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value);

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
            mls.LogWarning($"str: {str}");

            foreach (string x in str.Split(';'))
            {
                mls.LogWarning($"x: {x}");
                if (!x.Contains("@"))
                {
                    mls.LogWarning($"if: {x.ToLower().Replace(" ", string.Empty)}, {defValue}");
                    result.Add(x.ToLower().Replace(" ", string.Empty), defValue);
                }
                else
                {
                    string[] array = x.Split('@');
                    mls.LogWarning($"array: {array}");
                    if (float.TryParse(array[1], out var value))
                    {
                        mls.LogMessage($"{array[0].ToLower().Replace(" ", string.Empty)}, {value}");
                        result.Add(array[0].ToLower().Replace(" ", string.Empty), value);
                    }
                    else
                    {
                        mls.LogError($"Failed to parse value for {array[0].ToLower().Replace(" ", string.Empty)}, using default one: {defValue}. You should check the syntax in the config!");
                        result.Add(array[0].ToLower().Replace(" ", string.Empty), defValue);
                    }
                }
            }

            return result;
        }

    }
}
