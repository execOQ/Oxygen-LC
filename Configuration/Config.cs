using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using CSync.Util;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.Configuration
{
    [DataContract]
    public class Config : SyncedInstance<Config>
    {

        [DataMember]
        internal SyncedEntry<int> playerDamage;

        [DataMember]
        internal SyncedEntry<float> increasingOxygen;

        [DataMember]
        internal SyncedEntry<float> decreasingOxygen;

        [DataMember]
        internal SyncedEntry<float> multiplyDecreasingInFear;

        [DataMember]
        internal SyncedEntry<float> oxygenRunning;

        [DataMember]
        internal SyncedEntry<float> oxygenDepletionInWater;

        [DataMember]
        internal SyncedEntry<float> oxygenDeficiency;

        [DataMember]
        internal SyncedEntry<float> secTimer;

        [DataMember]
        internal SyncedEntry<bool> InfinityOxygenInbackrooms;

        [DataMember]
        internal SyncedEntry<bool> ShyHUDSupport;

        internal ConfigEntry<Vector3> OxygenHUDPosition;

        internal ConfigEntry<bool> notifications;

        internal ConfigEntry<float> SFXvolume;

        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableOxygenSFXInShip;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public Config(ConfigFile file)
        {
            InitInstance(this);

            playerDamage = file.BindSyncedEntry(
                "Player", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen. (syncing with host)" // Description
            );

            increasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygen", // Key
                0.001f, // Default value
                "How fast oxygen should be recovered. Happens every frame. (syncing with host)" // Description
            );

            decreasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygen", // Key
                0.0083f, // Default value
                "How fast oxygen is decreasing. Depends on timer setting. (syncing with host)" // Description
            );

            multiplyDecreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "multiplyDecreasingInFear", // Key
                0.02f, // Default value
                "Multiplies oxygen drain when player in fear. Depends on timer setting. (syncing with host)" // Description
            );

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.006f, // Default value
                "Increases oxygen drain when player running. Depends on timer setting. (syncing with host)" // Description
            );

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases oxygen deficiency when the player's oxygen runs out. Depends on timer setting. (syncing with host)" // Description
            );

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen deficiency when the player underwater. Depends on timer setting. (syncing with host)" // Description
            );

            secTimer = file.BindSyncedEntry(
                "Timer", // Section
                "secTimer", // Key
                5f, // Default value
                "Number of seconds the cool down timer lasts. (syncing with host)" // Description
            );

            notifications = file.Bind(
                "Notifications", // Section
                "notifications", // Key
                true, // Default value
                "Should mod notify you if oxygen getting low?" // Description
            );

            SFXvolume = file.Bind(
                "Sounds", // Section
                "SFXvolume", // Key
                1.0f, // Default value
                "volume of SFX's." // Description
            );

            enableOxygenSFX = file.Bind(
                "Sounds", // Section
                "enableOxygenSFX", // Key
                true, // Default value
                "Enables oxygen inhalation sounds." // Description
            );

            enableOxygenSFXInShip = file.Bind(
                "Sounds", // Section
                "enableOxygenSFXInShip", // Key
                false, // Default value
                "Remains oxygen inhalation sounds when player in ship. Depends on enableOxygenSFX variable." // Description
            );

            InfinityOxygenInbackrooms = file.BindSyncedEntry(
                "Compatibility", // Section
                "InfinityOxygenInbackrooms", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to the backrooms (mod). (syncing with host)" // Description
            );

            ShyHUDSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ShyHUDSupport", // Key
                true, // Default value
                "hud disappears if oxygen value > 55 (syncing with host)" // Description
            );

            OxygenHUDPosition = file.Bind(
                "Compatibility", // Section
                "OxygenHUDPosition", // Key
                new Vector3(-317.386f, 125.961f, -13.0994f), // Default value
                "Oxygen HUD postion (X, Y, Z)" // Description
            );
        }

        internal static void RequestSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(IntSize, Allocator.Temp);

            // Method `OnRequestSync` will then get called on host.
            stream.SendMessage($"{OxygenBase.modGUID}_OnRequestConfigSync");
        }

        internal static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                stream.SendMessage($"{OxygenBase.modGUID}_OnReceiveConfigSync", clientId);
            }
            catch (Exception e)
            {
                mls.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        internal static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(IntSize))
            {
                mls.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                mls.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            try
            {
                SyncInstance(data);
            }
            catch (Exception e)
            {
                mls.LogError($"Error syncing config instance!\n{e}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (IsHost)
            {
                Config.MessageManager.RegisterNamedMessageHandler($"{OxygenBase.modGUID}_OnRequestConfigSync", Config.OnRequestSync);
                Config.Synced = true;
                return;
            }

            Config.Synced = false;
            Config.MessageManager.RegisterNamedMessageHandler($"{OxygenBase.modGUID}_OnReceiveConfigSync", Config.OnReceiveSync);
            Config.RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            Config.RevertSync();
        }
    }
}
