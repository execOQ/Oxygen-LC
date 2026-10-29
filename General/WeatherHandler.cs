using Oxygen.Items;
using Unity.Netcode;

namespace Oxygen.General
{
    internal static class WeatherHandler
    {
        internal static LevelWeatherType weatherType = LevelWeatherType.None;
        private static bool reserveConfigured;
        private static float configuredCapacity;
        private static float? carriedReserve;
        internal static float ReserveCapacity => reserveConfigured ? configuredCapacity : 0f;

        public static bool IsOxygenOnShipLimited => StartOfRound.Instance != null &&
            !StartOfRound.Instance.shipIsLeaving && !StartOfRound.Instance.inShipPhase &&
            weatherType == LevelWeatherType.Eclipsed && OxygenBase.OxygenConfig.eclipsed_LimitOxygen.Value;

        internal static void UpdateWeatherType(SelectableLevel level)
        {
            weatherType = level != null ? level.currentWeather : LevelWeatherType.None;
            reserveConfigured = false;
            carriedReserve = null;
            RefreshLimit();
        }

        internal static void RefreshLimit()
        {
            var round = StartOfRound.Instance;
            if (round != null && round.currentLevel != null)
                weatherType = round.currentLevel.currentWeather;
            bool limited = weatherType == LevelWeatherType.Eclipsed && OxygenBase.OxygenConfig.eclipsed_LimitOxygen.Value;
            if (!limited)
            {
                reserveConfigured = false;
                carriedReserve = null;
                return;
            }

            var charger = OxyCharger.Instance;
            var network = NetworkManager.Singleton;
            // A late join must never refill the host's remaining reserve through a client RPC.
            if (network == null || !network.IsServer || charger == null || !charger.IsSpawned) return;
            float capacity = OxygenBase.OxygenConfig.eclipsed_LimitedOxygenAmount.Value;
            if (!reserveConfigured || configuredCapacity != capacity)
            {
                charger.RemainedOxygenAmount = capacity;
                configuredCapacity = capacity;
                reserveConfigured = true;
                carriedReserve = null;
            }
            else if (carriedReserve.HasValue)
            {
                charger.RemainedOxygenAmount = carriedReserve.Value;
                carriedReserve = null;
            }
        }

        internal static void ChargerDespawned(float remaining)
        {
            var network = NetworkManager.Singleton;
            if (network != null && network.IsServer && network.IsListening && reserveConfigured)
                carriedReserve = remaining;
        }

        internal static void Reset()
        {
            weatherType = LevelWeatherType.None;
            reserveConfigured = false;
            configuredCapacity = 0;
            carriedReserve = null;
        }
    }
}
