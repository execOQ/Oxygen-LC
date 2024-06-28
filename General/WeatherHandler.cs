using BepInEx.Logging;
using Oxygen.Extras;
using Oxygen.Items;

namespace Oxygen.General
{
    internal class WeatherHandler
    {
        private readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > WeatherHandler");

        internal static LevelWeatherType weatherType = LevelWeatherType.None;

        private static bool _isOxygenOnShipLimited;

        public static bool IsOxygenOnShipLimited
        {
            get
            {
                StartOfRound sor = StartOfRound.Instance;
                if (sor != null)
                {
                    // if the ship is on the orbit there's no need to limit oxygen
                    if (sor.shipIsLeaving)
                    {
                        return false;
                    }
                }
                return _isOxygenOnShipLimited;
            }
            private set
            {
                _isOxygenOnShipLimited = value;
            } 
        }

        internal static void UpdateWeatherType(SelectableLevel level)
        {
            OxyCharger.Instance.ResetRemainedOxygenAmount();

            IsOxygenOnShipLimited = false;

            if (level == null)
            {
                mls.LogError("Current level is null. WeatherType was setted to None.");
                weatherType = LevelWeatherType.None;
                return;
            }

            weatherType = level.currentWeather;

            string debugString = "\nCurrent level report:\n";
            debugString += "Current level name: " + level.name;
            debugString += "\nPlanet name: " + Utilities.GetLLLNameOfLevel(level.PlanetName);
            debugString += "\nCurrent weather: " + level.currentWeather.ToString();
            debugString += "\n\n";
            mls.LogInfo(debugString);

            if (weatherType == LevelWeatherType.Eclipsed && OxygenBase.OxygenConfig.eclipsed_LimitOxygen.Value)
            {
                mls.LogInfo("The weather on the planet is \"Eclipsed\", the oxygen on the ship has now become limited.");
                IsOxygenOnShipLimited = true;
            }
        }

    }
}
