using System;
using System.Reflection;
using Oxygen;
using Oxygen.General;
using Oxygen.Items;
using Unity.Netcode;

// Exercise production state on independently replicated host/client components.
// This fixture copies NGO variable snapshots; it does not simulate NGO transport.
static class ReserveGaugeChecks
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    static void Tick(OxyCharger charger) => typeof(OxyCharger).GetMethod("Update", Fields).Invoke(charger, null);

    static void Replicate<T>(OxyCharger from, OxyCharger to, string field)
    {
        var info = typeof(OxyCharger).GetField(field, Fields);
        ((NetworkVariable<T>)info.GetValue(to)).Value = ((NetworkVariable<T>)info.GetValue(from)).Value;
    }

    internal static int Run()
    {
        var host = OxyCharger.Instance;
        var round = StartOfRound.Instance;
        var config = OxygenBase.OxygenConfig;
        NetworkManager.Singleton.IsServer = true;
        round.inShipPhase = false;
        round.shipIsLeaving = false;
        round.currentLevel.currentWeather = LevelWeatherType.Eclipsed;
        config.eclipsed_LimitOxygen.Value = true;
        config.eclipsed_LimitedOxygenAmount.Value = 20;
        WeatherHandler.UpdateWeatherType(round.currentLevel);
        Tick(host);
        Check(host.ReserveLimited && host.ReserveCapacity == 20 && host.ReserveFraction == 1,
            "Gauge must start at the round reserve capacity.");

        host.RemainedOxygenAmount = 5;
        Tick(host);
        Check(host.ReserveCapacity == 20 && host.ReserveFraction == 0.25f,
            "Consumption must use the initial ship reserve as denominator.");
        OxygenInit.Percent = 0.98f;
        Check(host.ReserveFraction == 0.25f, "Gauge must not read the viewer's personal tank.");

        var client = new OxyCharger { gameObject = new UnityEngine.GameObject() };
        client.gameObject.Components.Add(new NetworkObject { IsSpawned = true });
        Replicate<float>(host, client, "_remainedOxygenAmount");
        Replicate<float>(host, client, "_reserveCapacity");
        Replicate<bool>(host, client, "_reserveLimited");
        NetworkManager.Singleton.IsServer = false;
        config.eclipsed_LimitedOxygenAmount.Value = 999;
        config.eclipsed_LimitOxygen.Value = false;
        Tick(client);
        Check(client.ReserveLimited && client.ReserveCapacity == 20 && client.ReserveFraction == 0.25f,
            "Late-join snapshot must override stale local mode and capacity.");

        NetworkManager.Singleton.IsServer = true;
        config.eclipsed_LimitedOxygenAmount.Value = 20;
        config.eclipsed_LimitOxygen.Value = true;
        host.RemainedOxygenAmount = 0;
        Tick(host);
        Check(host.ReserveLimited && host.ReserveFraction == 0, "Empty reserve must display zero.");
        host.RemainedOxygenAmount = 0.00000012f;
        Check(host.ReserveEmpty, "Float residue from competing refills must show the empty flag.");
        host.RemainedOxygenAmount = 0.0001f;
        Check(!host.ReserveEmpty, "A usable small reserve must not show the empty flag.");
        host.RemainedOxygenAmount = 0;
        round.inShipPhase = true;
        Tick(host);
        Check(!host.ReserveLimited && host.RemainedOxygenAmount == 0,
            "Orbit must show unlimited mode without replenishing the landed reserve.");
        round.inShipPhase = false;
        Tick(host);
        Check(host.ReserveLimited && host.ReserveFraction == 0, "Landing must restore empty limited state.");
        round.shipIsLeaving = true;
        Tick(host);
        Check(!host.ReserveLimited, "Departure must show unlimited mode.");
        round.shipIsLeaving = false;

        config.eclipsed_LimitedOxygenAmount.Value = 0;
        WeatherHandler.RefreshLimit();
        Tick(host);
        Check(host.ReserveLimited && host.ReserveCapacity == 0 && host.ReserveFraction == 0,
            "Zero capacity must remain finite and empty.");
        config.eclipsed_LimitedOxygenAmount.Value = 12;
        WeatherHandler.UpdateWeatherType(round.currentLevel);
        Tick(host);
        Check(host.ReserveCapacity == 12 && host.ReserveFraction == 1, "Next round must use its new capacity.");
        return checks;
    }
}
