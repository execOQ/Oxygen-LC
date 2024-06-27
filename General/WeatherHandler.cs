using BepInEx.Logging;
using Oxygen.Extras;

namespace Oxygen.General
{
    internal class WeatherHandler
    {
        private readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > WeatherHandler");

        internal static LevelWeatherType weatherType = LevelWeatherType.None;

        public static bool IsOxygenOnShipLimited { get; private set; }

        internal static void UpdateWeatherType(SelectableLevel level)
        {
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
                IsOxygenOnShipLimited = true;
            }
        }

    }
}
