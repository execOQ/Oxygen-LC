using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Extensions;
using CSync.Lib;
using Oxygen.Patches;
using System;
using System.Runtime.Serialization;

namespace Oxygen.Configuration
{
    [DataContract]
    public class OxygenConfig : SyncedConfig2<OxygenConfig>
    {
        public readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > OxygenConfig");

        // General
        internal ConfigEntry<bool> accurateMeter;

        [SyncedEntryField]
        internal SyncedEntry<int> oxygenFillOption;

        [SyncedEntryField]
        internal SyncedEntry<bool> recoverOxygenOnceShipLeft;

        [SyncedEntryField]
        internal SyncedEntry<int> playerDamage;

        [SyncedEntryField]
        internal SyncedEntry<string> greenPlanets;

        [SyncedEntryField]
        internal SyncedEntry<float> increasingOxygen;
        //[DataMember]
        //internal SyncedEntry<string> increasingOxygenMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> decreasingOxygenOutside;
        [SyncedEntryField]
        internal SyncedEntry<float> decreasingOxygenInFactory;

        [SyncedEntryField]
        internal SyncedEntry<string> decreasingOxygenOutsideMoons;
        [SyncedEntryField]
        internal SyncedEntry<string> decreasingOxygenInFactoryMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> oxygenRunning;
        [SyncedEntryField]
        internal SyncedEntry<string> oxygenRunningMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> oxygenDepletionInWater;
        [SyncedEntryField]
        internal SyncedEntry<string> oxygenDepletionInWaterMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> decreasingInFear;

        [SyncedEntryField]
        internal SyncedEntry<float> oxygenDeficiency;

        // Timer
        [SyncedEntryField]
        internal SyncedEntry<float> secTimer;

        // Compatibilities and supports
        [SyncedEntryField]
        internal SyncedEntry<bool> infinityOxygenInModsPlaces;

        [SyncedEntryField]
        internal SyncedEntry<bool> shyHUDSupport;

        [SyncedEntryField]
        internal SyncedEntry<bool> immersiveVisorSupport;
        [SyncedEntryField]
        internal SyncedEntry<float> immersiveVisor_OxygenDecreasing;

        // OxyBoost
        [SyncedEntryField]
        internal SyncedEntry<float> oxyBoost_increasingValue;
        [SyncedEntryField]
        internal SyncedEntry<int> oxyBoost_price;

        // HUD offsets
        internal ConfigEntry<int> XOffset;
        internal ConfigEntry<int> YOffset;

        // Notifications
        internal ConfigEntry<bool> notifications;

        // Sounds
        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableInhaleSFXWhileWalking;
        internal ConfigEntry<bool> enableOxygenSFXInShip;
        internal ConfigEntry<bool> enableOxygenSFXOnTheCompany;
        internal ConfigEntry<float> oxyCharger_SFXVolume;

        internal ConfigEntry<float> walkingSFX_volume, runningSFX_volume, exhaustedSFX_volume, scaredSFX_volume;

        void DoSomethingAfterSync(object s, EventArgs e)
        {
            mls.LogInfo("Config was synced, updating moons value!");
            RoundManagerPatch.UpdateMoonsValues();

            if (oxyBoost_price.Value != oxyBoost_price.LocalValue)
            {
                mls.LogInfo("Updating price for OxyBoost");
                OxygenBase.UpdateCustomItemPrice(OxygenBase.Instance.oxyBoost, oxyBoost_price.Value);
            }
        } 

        public OxygenConfig(ConfigFile file) : base(OxygenBase.modGUID)
        {
            InitialSyncCompleted += DoSomethingAfterSync;

            oxygenFillOption = file.BindSyncedEntry(
                "General", // Section
                "OxygenFillOption", // Key
                1, // Default value
                "0 - without oxygen filling;" +
                "\n1 - only using oxygen cylinders located in the ship;" +
                "\n2 - only automatic oxygen filling when the player is on the ship;" +
                "\n(syncing with host)" // Description
            );

            accurateMeter = file.Bind(
                "General", // Section
                "accurateMeter", // Key
                true, // Default value
                "Whether the oxygen meter ring should be accurate or have vanilla behaviour." // Description
            );

            recoverOxygenOnceShipLeft = file.BindSyncedEntry(
                "General", // Section
                "recoverOxygenOnceShipLeft", // Key
                true, // Default value
                "If this is true, oxygen will be recovered once ship has left." // Description
            );

            secTimer = file.BindSyncedEntry(
                "General", // Section
                "secTimer", // Key
                5f, // Default value
                "How many seconds must pass before oxygen is taken away?" +
                "\n(syncing with host)" // Description
            );

            playerDamage = file.BindSyncedEntry(
                "Player", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen." +
                "\n(syncing with host)" // Description
            );

            greenPlanets = file.BindSyncedEntry(
                "Oxygen", // Section
                "greenPlanets", // Key
                "March@0;Vow@0;Gordion@0", // Default value
                "Disables oxygen consumption outside on listed planets. " +
                "\nOxygen consumption by a player while underwater will still exist." +
                "\nFollow the syntax of the default value." +
                "\n(syncing with host)" // Description
            );

            increasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygen", // Key
                0.001f, // Default value
                "How fast oxygen should be recovered when OxygenFillOption is set to 2." +
                "\n(syncing with host)" // Description
            );

            /* increasingOxygenMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygenMoons", // Key
                string.Empty, // Default value
                "How fast oxygen is recovered when OxygenFillOption is set to 2." +
                "\nThis takes priority over the increasingOxygen config option." +
                "\nNot listed moons will use the increasingOxygen config option." +
                "\n(e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)." // Description
            ); */

            decreasingOxygenOutside = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutside", // Key
                0.0083f, // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenOutsideMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutsideMoons", // Key
                "Dine@0.0086;Rend@0.0086;Titan@0.009", // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenOutside config option." +
                "\nNot listed moons will use the decreasingOxygenOutside config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );
            
            decreasingOxygenInFactory = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactory", // Key
                0.0083f, // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenInFactoryMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactoryMoons", // Key
                "Dine@0.0086;Rend@0.0086;Titan@0.009", // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenInFactory config option." +
                "\nNot listed moons will use the decreasingOxygenInFactory config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            decreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingInFear", // Key
                0.01f, // Default value
                "Increases oxygen leakage when the player is in fear and is triggered every 2 seconds." +
                "\n(syncing with host)" // Description
            );

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.004f, // Default value
                "Increases oxygen drain when player running and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            oxygenRunningMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunningMoons", // Key
                "Dine@0.006;Rend@0.006;Titan@0.008", // Default value
                "Increases oxygen drain when player running and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenRunning config option." +
                "\nNot listed moons will use the oxygenRunning config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            oxygenDepletionInWaterMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWaterMoons", // Key
                string.Empty, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenDepletionInWater config option." +
                "\nNot listed moons will use the oxygenDepletionInWater config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases screen fog when the player runs out of oxygen." +
                "\nDepends on the secTimer variable." +
                "\n(syncing with host)" // Description
            );

            notifications = file.Bind(
                "Notifications", // Section
                "notifications", // Key
                true, // Default value
                "Should mod notify you if oxygen getting low?" // Description
            );

            walkingSFX_volume = file.Bind(
                "Sounds", // Section
                "walkingSFX_volume", // Key
                0.9f, // Default value
                "volume of walking SFX." // Description
            );

            runningSFX_volume = file.Bind(
                "Sounds", // Section
                "runningSFX_volume", // Key
                1f, // Default value
                "volume of running SFX." // Description
            );

            exhaustedSFX_volume = file.Bind(
                "Sounds", // Section
                "exhaustedSFX_volume", // Key
                0.3f, // Default value
                "volume of exhausted SFX." // Description
            );

            scaredSFX_volume = file.Bind(
                "Sounds", // Section
                "scaredSFX_volume", // Key
                1f, // Default value
                "volume of scared SFX." // Description
            );

            oxyCharger_SFXVolume = file.Bind(
                "Sounds", // Section
                "oxyCharger_SFXVolume", // Key
                1f, // Default value
                "OxyCharger's SFX volume" // Description
            );

            enableOxygenSFX = file.Bind(
                "Sounds", // Section
                "enableOxygenSFX", // Key
                true, // Default value
                "Enables oxygen inhalation sounds." // Description
            );

            enableInhaleSFXWhileWalking = file.Bind(
                "Sounds", // Section
                "enableInhaleSFXWhileWalking", // Key
                true, // Default value
                "Enables oxygen inhalation sounds while walking." // Description
            );

            enableOxygenSFXInShip = file.Bind(
                "Sounds", // Section
                "enableOxygenSFXInShip", // Key
                false, // Default value
                "Remains oxygen inhalation sounds when player in ship." +
                "\nWorks if enableOxygenSFX variable is enabled." // Description
            );

            enableOxygenSFXOnTheCompany = file.Bind(
                "Sounds", // Section
                "enableOxygenSFXOnTheCompany", // Key
                true, // Default value
                "Remains oxygen inhalation sounds when player on the Gordion (The Company) planet." +
                "\nWorks if enableOxygenSFX variable is enabled." // Description
            );

            infinityOxygenInModsPlaces = file.BindSyncedEntry(
                "Compatibility", // Section
                "InfinityOxygenInModsPlaces", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to mod's places to simplificate gameplay." +
                "\n(syncing with host)" // Description
            );

            shyHUDSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ShyHUDSupport", // Key
                true, // Default value
                "HUD disappears if oxygen value > 75" + // previously 0.55f
                "\n(syncing with host)" // Description
            );

            immersiveVisorSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ImmersiveVisorSupport", // Key
                true, // Default value
                "Enables oxygen consumption when the helmet's crack level is 2." +
                "\n(syncing with host)" // Description
            );

            immersiveVisor_OxygenDecreasing = file.BindSyncedEntry(
                "Compatibility", // Section
                "ImmersiveVisor_OxygenDecreasing", // Key
                0.002f, // Default value
                "How much additional oxygen will be consumed if the helmet's crack level is 2?" +
                "\nDepends on the secTimer variable." +
                "\n(syncing with host)" // Description
            );

            XOffset = file.Bind(
                "Position", 
                "XOffset", 
                0, 
                "Horizontal offset for the oxygenHUD position."
            );

            YOffset = file.Bind(
                "Position", 
                "YOffset", 
                0,
                "Vertical offset for the oxygenHUD position."
            );

            oxyBoost_increasingValue = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_increasingValue", // Key
                0.001f, // Default value
                "How much oxygen does OxyBoost add to a player." +
                "\n(syncing with host)" // Description
            );

            oxyBoost_price = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_price", // Key
                70, // Default value
                "OxyBoost's price." +
                "\n(syncing with host)" // Description
            );

            ConfigManager.Register(this);
        }
    }
}
