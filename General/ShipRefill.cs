using Oxygen.Items;
using System;

namespace Oxygen.General
{
    internal static class ShipRefill
    {
        internal static void Refill()
        {
            var charger = OxyCharger.Instance;
            if (charger != null && charger.IsSpawned)
            {
                charger.AutoRefillOxygen();
            }
            else if (!WeatherHandler.IsOxygenOnShipLimited)
            {
                // Auto refill is a separate host option from the physical charger.
                OxygenInit.Percent = Math.Max(0f, Math.Min(1f,
                    OxygenInit.Percent + OxygenBase.OxygenConfig.autoFillingOnShip_increasingOxygen.Value));
            }
        }
    }
}
