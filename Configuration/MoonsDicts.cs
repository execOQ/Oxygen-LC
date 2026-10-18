using BepInEx.Logging;
using Oxygen.Extras;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Oxygen.Configuration
{
    internal class MoonsDicts
    {
        private static readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture; // This is important, no touchy

        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > ConfigHandler");

        private static string MoonName => Utilities.GetNumberlessPlanetName(StartOfRound.Instance.currentLevel.PlanetName).ToLowerInvariant().Replace(" ", string.Empty);

        internal static Dictionary<string, float> greenPlanets;
        internal static Dictionary<string, float> decreasingOxygenOutsideMoons;
        internal static Dictionary<string, float> decreasingOxygenInFactoryMoons;
        internal static Dictionary<string, float> runningMultiplierMoons; 
        internal static Dictionary<string, float> oxygenDepletionInWaterMoons;

        internal static bool GreenPlanetsValue => greenPlanets != null && greenPlanets.ContainsKey(MoonName);

        internal static float DecreasingOxygenOutsideMoonsValue =>
            GetValueFromDictionary(decreasingOxygenOutsideMoons, OxygenBase.OxygenConfig.decreasingOxygenOutside.Value);

        internal static float DecreasingOxygenInFactoryMoonsValue =>
            GetValueFromDictionary(decreasingOxygenInFactoryMoons, OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value);

        internal static float RunningMultiplierMoonsValue =>
            GetValueFromDictionary(runningMultiplierMoons, OxygenBase.OxygenConfig.runningMultiplier.Value);

        internal static float OxygenDepletionInWaterMoonsValue =>
            GetValueFromDictionary(oxygenDepletionInWaterMoons, OxygenBase.OxygenConfig.oxygenDepletionInWater.Value);

        private static float GetValueFromDictionary(Dictionary<string, float> dictionary, float defaultValue)
        {
            if (dictionary != null && dictionary.TryGetValue(MoonName, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        internal sealed class PreparedValues
        {
            internal Dictionary<string, float> Green, Outside, Inside, Running, Water;
        }

        internal static PreparedValues Prepare(OxygenConfig config,
            IReadOnlyDictionary<IHostConfigEntry, object> overrides = null)
        {
            T Value<T>(HostConfigEntry<T> entry) => overrides == null ? entry.Value : (T)overrides[entry];
            return new PreparedValues
            {
                Green = GetLevelValue(Value(config.greenPlanets), 0, "greenPlanets"),
                Outside = GetLevelValue(Value(config.decreasingOxygenOutsideMoons), Value(config.decreasingOxygenOutside), "decreasingOxygenOutsideMoons"),
                Inside = GetLevelValue(Value(config.decreasingOxygenInFactoryMoons), Value(config.decreasingOxygenInFactory), "decreasingOxygenInFactoryMoons"),
                Running = GetLevelValue(Value(config.runningMultiplierMoons), Value(config.runningMultiplier), "runningMultiplierMoons"),
                Water = GetLevelValue(Value(config.oxygenDepletionInWaterMoons), Value(config.oxygenDepletionInWater), "oxygenDepletionInWaterMoons")
            };
        }

        internal static void UpdateMoonsValues()
        {
            // Parse every list before replacing any active dictionary.
            var values = Prepare(OxygenBase.OxygenConfig);
            greenPlanets = values.Green;
            decreasingOxygenOutsideMoons = values.Outside;
            decreasingOxygenInFactoryMoons = values.Inside;
            runningMultiplierMoons = values.Running;
            oxygenDepletionInWaterMoons = values.Water;
        }

        internal static void Clear()
        {
            greenPlanets = decreasingOxygenOutsideMoons = decreasingOxygenInFactoryMoons =
                runningMultiplierMoons = oxygenDepletionInWaterMoons = null;
        }
        private static Dictionary<string, float> GetLevelValue(string str, float defValue, string nameOfVariable)
        {
            Dictionary<string, float> result = [];

            // to simplify my work...
            result.Add("default", defValue);

            if (string.IsNullOrEmpty(str))
            {
                return result;
            }

            str = str.Replace(" ", string.Empty).ToString(cultureInfo);
            mls.LogInfo($"Processing the variable {nameOfVariable}:\n{str}");

            foreach (string x in str.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(x)) continue;
                if (!x.Contains("@"))
                {
                    if (result.ContainsKey(x.ToLowerInvariant()))
                        throw new InvalidDataException($"Duplicate moon '{x}' in {nameOfVariable}.");
                    result.Add(x.ToLowerInvariant().Replace(" ", string.Empty), defValue);
                }
                else
                {
                    string[] array = x.Split('@');

                    string moonName = array[0].ToLowerInvariant().Replace(" ", string.Empty);
                    if (string.IsNullOrEmpty(moonName) || array.Length != 2)
                        throw new InvalidDataException($"Invalid moon entry '{x}' in {nameOfVariable}.");
                    if (result.ContainsKey(moonName))
                        throw new InvalidDataException($"Duplicate moon '{moonName}' in {nameOfVariable}.");

                    if (array[1].Contains(","))
                    {
                        mls.LogWarning($"Found a comma in {nameOfVariable}! Change it to a period, please.");
                        array[1] = array[1].Replace(',', '.');
                    }

                    if (float.TryParse(array[1], NumberStyles.Float, cultureInfo, out var value))
                    {
                        if (float.IsNaN(value) || float.IsInfinity(value))
                            throw new InvalidDataException($"Non-finite moon value in {nameOfVariable}: '{x}'.");
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
