using CSync.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oxygen.Configuration
{
    internal class MoonsDicts
    {
        internal static string MoonName => NumberLessPlanetName(StartOfRound.Instance.currentLevel.PlanetName);

        internal static Dictionary<string, float> increasingOxygenMoons;

        internal static float IncreasingOxygenMoonsValue
        {
            get
            {
                if (increasingOxygenMoons != null && increasingOxygenMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.increasingOxygen.Value;
            }
        }

        internal static Dictionary<string, float> decreasingOxygenOutsideMoons;

        internal static float decreasingOxygenOutsideMoonsValue
        {
            get
            {
                if (decreasingOxygenOutsideMoons != null && decreasingOxygenOutsideMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.decreasingOxygenOutside.Value;
            }
        }

        internal static Dictionary<string, float> decreasingOxygenInFactoryMoons;

        internal static float decreasingOxygenInFactoryMoonsValue
        {
            get
            {
                if (decreasingOxygenInFactoryMoons != null && decreasingOxygenInFactoryMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.decreasingOxygenInFactory.Value;
            }
        }

        internal static Dictionary<string, float> decreasingInFearMoons;

        internal static float decreasingInFearMoonsValue
        {
            get
            {
                if (decreasingInFearMoons != null && decreasingInFearMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.decreasingInFear.Value;
            }
        }

        internal static Dictionary<string, float> oxygenRunningMoons;

        internal static float oxygenRunningMoonsValue
        {
            get
            {
                if (oxygenRunningMoons != null && oxygenRunningMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.oxygenRunning.Value;
            }
        }

        internal static Dictionary<string, float> oxygenDepletionInWaterMoons;

        internal static float oxygenDepletionInWaterMoonsValue
        {
            get
            {
                if (oxygenDepletionInWaterMoons != null && oxygenDepletionInWaterMoons.TryGetValue(MoonName.ToLower(), out var value))
                {
                    return value;
                }

                return OxygenBase.OxygenConfig.oxygenDepletionInWater.Value;
            }
        }

        public static string NumberLessPlanetName(string moon)
        {
            return new(moon.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
        }
    }
}
