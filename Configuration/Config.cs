using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using CSync.Util;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.Configuration
{
    [DataContract]
    public class Config : SyncedConfig<Config>
    {

        [DataMember]
        internal SyncedEntry<int> playerDamage;

        [DataMember]
        internal SyncedEntry<float> increasingOxygen;

        [DataMember]
        internal SyncedEntry<float> decreasingOxygen;

        [DataMember]
        internal SyncedEntry<float> decreasingInFear;

        [DataMember]
        internal SyncedEntry<float> oxygenRunning;

        [DataMember]
        internal SyncedEntry<float> oxygenDepletionInWater;

        [DataMember]
        internal SyncedEntry<float> oxygenDeficiency;

        [DataMember]
        internal SyncedEntry<float> secTimer;

        [DataMember]
        internal SyncedEntry<bool> InfinityOxygenInModsPlaces;

        [DataMember]
        internal SyncedEntry<bool> ShyHUDSupport;

        [DataMember]
        internal SyncedEntry<float> oxy99_increasingValue;

        [DataMember]
        internal SyncedEntry<int> oxy99_price;

        internal ConfigEntry<int> XOffset;

        internal ConfigEntry<int> YOffset;

        internal ConfigEntry<bool> notifications;

        internal ConfigEntry<float> SFXvolume;

        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableOxygenSFXInShip;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public Config(ConfigFile file) : base(OxygenBase.modGUID)
        {
            ConfigManager.Register(this);

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
                "How much oxygen should be released when the timer (The timing of the timer is the secTimer variable) goes off? (syncing with host)" // Description
            );

            // old
            file.Bind(
                "Oxygen", // Section
                "multiplyDecreasingInFear", // Key
                0.02f, // Default value
                "An old variable, now it's decreasingInFear" // Description
            );

            decreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingInFear", // Key
                0.02f, // Default value
                "Increases oxygen leakage when the player is in fear. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.006f, // Default value
                "Increases oxygen drain when player running. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases screen fog when the player runs out of oxygen. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen consumption when the player is underwater. Depends on the secTimer variable. (syncing with host)" // Description
            );

            secTimer = file.BindSyncedEntry(
                "Timer", // Section
                "secTimer", // Key
                5f, // Default value
                "How many seconds must pass before oxygen is taken away? (syncing with host)" // Description
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
                0.3f, // Default value
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
                "Remains oxygen inhalation sounds when player in ship. Works if enableOxygenSFX variable is enabled." // Description
            );

            InfinityOxygenInModsPlaces = file.BindSyncedEntry(
                "Compatibility", // Section
                "InfinityOxygenInModsPlaces", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to mod's places to simplificate gameplay. (syncing with host)" // Description
            );

            ShyHUDSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ShyHUDSupport", // Key
                true, // Default value
                "hud disappears if oxygen value > 55 (syncing with host)" // Description
            );

            file.Bind(
                "Compatibility", // Section
                "OxygenHUDPosition", // Key
                new Vector3(-317.386f, 125.961f, -13.0994f), // Default value
                "An old variable, use the XOffset and YOffset" // Description
            );

            XOffset = file.Bind<int>(
                "Position", 
                "XOffset", 
                0, 
                "Horizontal offset for the health text position."
            );

            YOffset = file.Bind<int>(
                "Position", 
                "YOffset", 
                0, 
                "Vertical offset for the health text position."
            );

            // im too lazy to change variables name in unity
            oxy99_increasingValue = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_increasingValue", // Key
                0.001f, // Default value
                "How much oxygen does OxyBoost add to a player" // Description
            );

            oxy99_price = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_price", // Key
                70, // Default value
                "OxyBoost's price" // Description
            );
        }
    }
}
