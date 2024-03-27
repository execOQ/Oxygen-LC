using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using System.Collections.Generic;

namespace Oxygen.Patches
{
    internal class RoundManagerPatch
    {
        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > RoundManagerPatch");

        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        private static void RoundManagerPatch_Postfix()
        {
            UpdateMoonsValues();
        }

        [HarmonyPriority(0)]
        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        private static void RoundManagerPatch_Postfix2(RoundManager __instance)
        {
            mls.LogInfo("Use these moons names in the config:\n");
            foreach (SelectableLevel level in __instance.playersManager.levels)
            {
                string numberlessPlanetName = MoonsDicts.NumberLessPlanetName(level.PlanetName);
                mls.LogInfo(numberlessPlanetName);
            }
        }

        internal static void UpdateMoonsValues()
        {
            MoonsDicts.greenPlanets = GetLevelValue(
                OxygenConfig.Instance.greenPlanets.Value, 
                0, 
                "greenPlanets"
            );

            /* MoonsDicts.increasingOxygenMoons = GetLevelValue(
                OxygenConfig.Instance.increasingOxygenMoons.Value, 
                OxygenConfig.Instance.increasingOxygen.Value, 
                "increasingOxygenMoons"
            ); */

            MoonsDicts.decreasingOxygenOutsideMoons = GetLevelValue(
                OxygenConfig.Instance.decreasingOxygenOutsideMoons.Value, 
                OxygenConfig.Instance.decreasingOxygenOutside.Value,
                "decreasingOxygenOutsideMoons"
            );

            MoonsDicts.decreasingOxygenInFactoryMoons = GetLevelValue(
                OxygenConfig.Instance.decreasingOxygenInFactoryMoons.Value, 
                OxygenConfig.Instance.decreasingOxygenInFactory.Value,
                "decreasingOxygenInFactoryMoons"
            );

            MoonsDicts.oxygenRunningMoons = GetLevelValue(
                OxygenConfig.Instance.oxygenRunningMoons.Value, 
                OxygenConfig.Instance.oxygenRunning.Value,
                "oxygenRunningMoons"
            );

            MoonsDicts.oxygenDepletionInWaterMoons = GetLevelValue(
                OxygenConfig.Instance.oxygenDepletionInWaterMoons.Value, 
                OxygenConfig.Instance.oxygenDepletionInWater.Value,
                "oxygenDepletionInWaterMoons"
            );
        }

        private static Dictionary<string, float> GetLevelValue(string str, float defValue, string nameOfVariable)
        {
            Dictionary<string, float> result = [];

            // for simplification my work...
            result.Add("default", defValue);

            if (string.IsNullOrEmpty(str))
            {
                return result;
            }

            str = str.Replace(" ", string.Empty);
            mls.LogDebug($"processing: {str}");

            foreach (string x in str.Split(';'))
            {
                if (!x.Contains("@"))
                {
                    result.Add(x.ToLower().Replace(" ", string.Empty), defValue);
                }
                else
                {
                    string[] array = x.Split('@');

                    string moonName = array[0].ToLower().Replace(" ", string.Empty);

                    if (float.TryParse(array[1], out var value))
                    {
                        mls.LogInfo($"Parsed {value} for {moonName}");
                        result.Add(moonName, value);
                    }
                    else
                    {
                        mls.LogError($"Failed to parse value for {moonName}, using default one: {defValue}.\nYou should check the syntax in the variable {nameOfVariable} in the config!");
                        result.Add(moonName, defValue);
                    }
                }
            }

            return result;
        }

    }
}
