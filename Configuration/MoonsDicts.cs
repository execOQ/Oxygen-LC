using System.Collections.Generic;
using System.Linq;

namespace Oxygen.Configuration
{
    internal class MoonsDicts
    {
        internal static string MoonName => NumberLessPlanetName(StartOfRound.Instance.currentLevel.PlanetName);

        internal static Dictionary<string, float> greenPlanets;

        internal static bool GreenPlanetsValue
        {
            get
            {
                if (greenPlanets != null && greenPlanets.TryGetValue(MoonName.ToLower(), out var _))
                {
                    return true;
                }

                return false;
            }
        }

        /* internal static Dictionary<string, float> increasingOxygenMoons;

        internal static float IncreasingOxygenMoonsValue
        {
            get
            {
                if (increasingOxygenMoons != null && increasingOxygenMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenConfig.Instance.increasingOxygen.Value;
            }
        } */

        internal static Dictionary<string, float> decreasingOxygenOutsideMoons;

        internal static float DecreasingOxygenOutsideMoonsValue
        {
            get
            {
                if (decreasingOxygenOutsideMoons != null && decreasingOxygenOutsideMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenConfig.Instance.decreasingOxygenOutside.Value;
            }
        }

        internal static Dictionary<string, float> decreasingOxygenInFactoryMoons;

        internal static float DecreasingOxygenInFactoryMoonsValue
        {
            get
            {
                if (decreasingOxygenInFactoryMoons != null && decreasingOxygenInFactoryMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenConfig.Instance.decreasingOxygenInFactory.Value;
            }
        }

        internal static Dictionary<string, float> oxygenRunningMoons;

        internal static float OxygenRunningMoonsValue
        {
            get
            {
                if (oxygenRunningMoons != null && oxygenRunningMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenConfig.Instance.oxygenRunning.Value;
            }
        }

        internal static Dictionary<string, float> oxygenDepletionInWaterMoons;

        internal static float OxygenDepletionInWaterMoonsValue
        {
            get
            {
                if (oxygenDepletionInWaterMoons != null && oxygenDepletionInWaterMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenConfig.Instance.oxygenDepletionInWater.Value;
            }
        }

        public static string NumberLessPlanetName(string moon)
        {
            return new(moon.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
        }
    }
}
