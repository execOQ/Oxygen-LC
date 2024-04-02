﻿using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using System.Collections.Generic;
using System.Globalization;

namespace Oxygen.Patches
{
    internal class RoundManagerPatch
    {
        private static readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture; // This is important, no touchy

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
                OxygenBase.OxygenConfig.greenPlanets.Value, 
                0, 
                "greenPlanets"
            );

            /* MoonsDicts.increasingOxygenMoons = GetLevelValue(
                OxygenBase.OxygenConfig.increasingOxygenMoons.Value, 
                OxygenBase.OxygenConfig.increasingOxygen.Value, 
                "increasingOxygenMoons"
            ); */

            MoonsDicts.decreasingOxygenOutsideMoons = GetLevelValue(
                OxygenBase.OxygenConfig.decreasingOxygenOutsideMoons.Value,
                OxygenBase.OxygenConfig.decreasingOxygenOutside.Value,
                "decreasingOxygenOutsideMoons"
            );

            MoonsDicts.decreasingOxygenInFactoryMoons = GetLevelValue(
                OxygenBase.OxygenConfig.decreasingOxygenInFactoryMoons.Value,
                OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value,
                "decreasingOxygenInFactoryMoons"
            );

            MoonsDicts.oxygenRunningMoons = GetLevelValue(
                OxygenBase.OxygenConfig.oxygenRunningMoons.Value,
                OxygenBase.OxygenConfig.oxygenRunning.Value,
                "oxygenRunningMoons"
            );

            MoonsDicts.oxygenDepletionInWaterMoons = GetLevelValue(
                OxygenBase.OxygenConfig.oxygenDepletionInWaterMoons.Value,
                OxygenBase.OxygenConfig.oxygenDepletionInWater.Value,
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

            str = str.Replace(" ", string.Empty).ToString(cultureInfo);
            mls.LogInfo($"processing the variable {nameOfVariable}:\n{str}");

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

                    if (array[1].Contains(","))
                    {
                        mls.LogWarning($"Found a comma in {nameOfVariable}! Change it to a period, please.");
                        array[1] = array[1].Replace(',', '.');
                    }

                    if (float.TryParse(array[1], NumberStyles.Float, cultureInfo, out var value)) 
                    {
                        mls.LogInfo($"Parsed {value} for {moonName}");
                        result.Add(moonName, value);
                    }
                    else
                    {
                        mls.LogError($"Failed to parse {value} for {moonName}, using default one: {defValue}.\nYou should check the syntax in the variable {nameOfVariable} in the config!");
                        result.Add(moonName, defValue);
                    }
                }
            }

            return result;
        }

    }
}
