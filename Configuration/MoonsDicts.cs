using BepInEx.Logging;
using Oxygen.Extras;
using System.Collections.Generic;
using System.Globalization;

namespace Oxygen.Configuration
{
    internal class MoonsDicts
    {
        private static readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture; // This is important, no touchy

        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > ConfigHandler");

        private static string MoonName => Utilities.NumberLessPlanetName(StartOfRound.Instance.currentLevel.PlanetName).ToLower();

        internal static Dictionary<string, float> greenPlanets;
        internal static Dictionary<string, float> decreasingOxygenOutsideMoons;
        internal static Dictionary<string, float> decreasingOxygenInFactoryMoons;
        internal static Dictionary<string, float> runningMultiplierMoons; 
        internal static Dictionary<string, float> oxygenDepletionInWaterMoons;
        // internal static Dictionary<string, float> increasingOxygenMoons;

        internal static bool GreenPlanetsValue => greenPlanets != null && greenPlanets.ContainsKey(MoonName);

        internal static float DecreasingOxygenOutsideMoonsValue =>
            GetValueFromDictionary(decreasingOxygenOutsideMoons, OxygenBase.OxygenConfig.decreasingOxygenOutside.Value);

        internal static float DecreasingOxygenInFactoryMoonsValue =>
            GetValueFromDictionary(decreasingOxygenInFactoryMoons, OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value);

        internal static float RunningMultiplierMoonsValue =>
            GetValueFromDictionary(runningMultiplierMoons, OxygenBase.OxygenConfig.runningMultiplier.Value);

        internal static float OxygenDepletionInWaterMoonsValue =>
            GetValueFromDictionary(oxygenDepletionInWaterMoons, OxygenBase.OxygenConfig.oxygenDepletionInWater.Value);

        // internal static float IncreasingOxygenMoonsValue => GetValueFromDictionary(increasingOxygenMoons, OxygenBase.OxygenConfig.increasingOxygen.Value);

        private static float GetValueFromDictionary(Dictionary<string, float> dictionary, float defaultValue)
        {
            if (dictionary != null && dictionary.TryGetValue(MoonName, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        internal static void UpdateMoonsValues()
        {
            greenPlanets = GetLevelValue(
                OxygenBase.OxygenConfig.greenPlanets.Value,
                0,
                "greenPlanets"
            );

            /* MoonsDicts.increasingOxygenMoons = GetLevelValue(
                OxygenBase.OxygenConfig.increasingOxygenMoons.Value, 
                OxygenBase.OxygenConfig.increasingOxygen.Value, 
                "increasingOxygenMoons"
            ); */

            decreasingOxygenOutsideMoons = GetLevelValue(
                OxygenBase.OxygenConfig.decreasingOxygenOutsideMoons.Value,
                OxygenBase.OxygenConfig.decreasingOxygenOutside.Value,
                "decreasingOxygenOutsideMoons"
            );

            decreasingOxygenInFactoryMoons = GetLevelValue(
                OxygenBase.OxygenConfig.decreasingOxygenInFactoryMoons.Value,
                OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value,
                "decreasingOxygenInFactoryMoons"
            );

            runningMultiplierMoons = GetLevelValue(
                OxygenBase.OxygenConfig.runningMultiplierMoons.Value,
                OxygenBase.OxygenConfig.runningMultiplier.Value,
                "oxygenRunningMoons"
            );

            oxygenDepletionInWaterMoons = GetLevelValue(
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
            mls.LogInfo($"Processing the variable {nameOfVariable}:\n{str}");

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
