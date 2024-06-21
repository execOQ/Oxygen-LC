using Oxygen.Extras;
using System.Collections.Generic;

namespace Oxygen.Configuration
{
    internal class MoonsDicts
    {
        private static string MoonName => Utilities.NumberLessPlanetName(StartOfRound.Instance.currentLevel.PlanetName).ToLower();

        internal static Dictionary<string, float> greenPlanets;
        internal static Dictionary<string, float> decreasingOxygenOutsideMoons;
        internal static Dictionary<string, float> decreasingOxygenInFactoryMoons;
        internal static Dictionary<string, float> oxygenRunningMoons;
        internal static Dictionary<string, float> oxygenDepletionInWaterMoons;
        // internal static Dictionary<string, float> increasingOxygenMoons;

        internal static bool GreenPlanetsValue => greenPlanets != null && greenPlanets.ContainsKey(MoonName);

        private static float GetValueFromDictionary(Dictionary<string, float> dictionary, float defaultValue)
        {
            if (dictionary != null && dictionary.TryGetValue(MoonName, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        internal static float DecreasingOxygenOutsideMoonsValue =>
            GetValueFromDictionary(decreasingOxygenOutsideMoons, OxygenBase.OxygenConfig.decreasingOxygenOutside.Value);

        internal static float DecreasingOxygenInFactoryMoonsValue =>
            GetValueFromDictionary(decreasingOxygenInFactoryMoons, OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value);

        internal static float OxygenRunningMoonsValue =>
            GetValueFromDictionary(oxygenRunningMoons, OxygenBase.OxygenConfig.oxygenRunning.Value);

        internal static float OxygenDepletionInWaterMoonsValue =>
            GetValueFromDictionary(oxygenDepletionInWaterMoons, OxygenBase.OxygenConfig.oxygenDepletionInWater.Value);

        // internal static float IncreasingOxygenMoonsValue => GetValueFromDictionary(increasingOxygenMoons, OxygenBase.OxygenConfig.increasingOxygen.Value);
    }
}
