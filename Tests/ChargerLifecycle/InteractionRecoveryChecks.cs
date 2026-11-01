using System;
using LethalLib.Modules;
using Oxygen;
using Oxygen.General;
using Oxygen.Items;
using Unity.Netcode;
using UnityEngine;

static class InteractionRecoveryChecks
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool pass, string message)
        {
            if (!pass) throw new Exception(message);
            checks++;
        }

        var round = StartOfRound.Instance;
        var live = OxygenInit.oxyCharger;
        var charger = live.GetComponent<OxyCharger>();
        var networkObject = live.GetComponent<NetworkObject>();
        var interaction = new GameObject();
        var refillCollider = new BoxCollider();
        interaction.Components.Add(refillCollider);
        var trigger = new InteractTrigger { gameObject = interaction };
        live.Components.Add(trigger);
        var placement = new BoxCollider();
        live.GetComponent<PlaceableShipObject>().placeObjectCollider = placement;
        var unrelated = new BoxCollider { enabled = false };
        live.Components.Add(unrelated);
        var item = Unlockables.Registered;
        int spawns = round.Spawns;
        ulong networkId = networkObject.NetworkObjectId;
        charger.RemainedOxygenAmount = 0.375f;

        void Disable()
        {
            interaction.SetActive(false);
            trigger.enabled = trigger.interactable = false;
            refillCollider.enabled = placement.enabled = false;
        }
        void Recovered(string context)
        {
            Check(interaction.activeSelf && trigger.enabled && trigger.interactable, context + ": trigger not restored.");
            Check(refillCollider.enabled && placement.enabled, context + ": refill/build collider not restored.");
            Check(!unrelated.enabled, context + ": unrelated collider enabled.");
        }

        Disable();
        round.firingPlayersCutsceneRunning = true;
        ChargerUnlockable.RestoreInteraction();
        Check(!refillCollider.enabled, "Do not override native ejection collision suppression.");
        round.firingPlayersCutsceneRunning = false;
        round.suckingFurnitureOutOfShip = true;
        ChargerUnlockable.RestoreInteraction();
        Check(!refillCollider.enabled, "Wait until furniture suction ends.");
        round.suckingFurnitureOutOfShip = false;
        ChargerUnlockable.RestoreInteraction();
        Recovered("Host after firing");
        Check(charger.RemainedOxygenAmount == 0.375f && round.Spawns == spawns && networkObject.NetworkObjectId == networkId,
            "Recovery must not respawn furniture or replenish the reserve.");

        Disable();
        NetworkManager.Singleton.IsServer = false;
        ChargerUnlockable.RestoreInteraction();
        Recovered("Client after firing");
        ChargerUnlockable.RestoreInteraction();
        Recovered("Repeated ready event");
        NetworkManager.Singleton.IsServer = true;

        Disable();
        item.inStorage = true;
        ChargerUnlockable.RestoreInteraction();
        Check(!interaction.activeSelf && !refillCollider.enabled, "Do not reactivate stored furniture.");
        item.inStorage = false;
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = false;
        ChargerUnlockable.RestoreInteraction();
        Check(!interaction.activeSelf && !refillCollider.enabled, "Do not reactivate a disabled charger.");
        OxygenBase.OxygenConfig.oxyCharger_Enabled.Value = true;
        networkObject.IsSpawned = false;
        ChargerUnlockable.RestoreInteraction();
        Check(!refillCollider.enabled, "Do not repair an unspawned object.");
        networkObject.IsSpawned = true;
        OxygenInit.oxyCharger = null;
        ChargerUnlockable.RestoreInteraction();
        Check(!refillCollider.enabled, "Missing station must remain harmless.");
        OxygenInit.oxyCharger = live;
        ChargerUnlockable.RestoreInteraction();
        Recovered("Next valid ready event");
        return checks;
    }
}
