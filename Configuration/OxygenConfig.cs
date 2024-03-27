using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using CSync.Util;
using Oxygen.Patches;
using System;
using System.Runtime.Serialization;

namespace Oxygen.Configuration
{
    [DataContract]
    public class OxygenConfig : SyncedConfig<OxygenConfig>
    {
        internal ConfigEntry<bool> MakeItVanilla;

        [DataMember]
        internal SyncedEntry<int> OxygenFillOption;

        [DataMember]
        internal SyncedEntry<bool> recoverOxygenOnceShipLeft;

        [DataMember]
        internal SyncedEntry<int> playerDamage;

        [DataMember]
        internal SyncedEntry<string> greenPlanets;

        [DataMember]
        internal SyncedEntry<float> increasingOxygen;

        //[DataMember]
        //internal SyncedEntry<string> increasingOxygenMoons;

        [DataMember]
        internal SyncedEntry<float> decreasingOxygenOutside;

        [DataMember]
        internal SyncedEntry<float> decreasingOxygenInFactory;

        [DataMember]
        internal SyncedEntry<string> decreasingOxygenOutsideMoons;

        [DataMember]
        internal SyncedEntry<string> decreasingOxygenInFactoryMoons;

        [DataMember]
        internal SyncedEntry<float> decreasingInFear;

        [DataMember]
        internal SyncedEntry<float> oxygenRunning;

        [DataMember]
        internal SyncedEntry<string> oxygenRunningMoons;

        [DataMember]
        internal SyncedEntry<float> oxygenDepletionInWater;

        [DataMember]
        internal SyncedEntry<string> oxygenDepletionInWaterMoons;

        [DataMember]
        internal SyncedEntry<float> oxygenDeficiency;

        [DataMember]
        internal SyncedEntry<float> secTimer;

        [DataMember]
        internal SyncedEntry<bool> InfinityOxygenInModsPlaces;

        [DataMember]
        internal SyncedEntry<bool> ShyHUDSupport;

        [DataMember]
        internal SyncedEntry<bool> ImmersiveVisorSupport;

        [DataMember]
        internal SyncedEntry<bool> EladsHUD_QuickFix;

        [DataMember]
        internal SyncedEntry<float> ImmersiveVisor_OxygenDecreasing;

        [DataMember]
        internal SyncedEntry<float> oxyBoost_increasingValue;

        [DataMember]
        internal SyncedEntry<int> oxyBoost_price;

        internal ConfigEntry<int> XOffset;

        internal ConfigEntry<int> YOffset;

        internal ConfigEntry<bool> notifications;

        internal ConfigEntry<float> walkingSFX_volume, runningSFX_volume, exhaustedSFX_volume, scaredSFX_volume;

        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableInhaleSFXWhileWalking;

        internal ConfigEntry<bool> enableOxygenSFXInShip;

        internal ConfigEntry<bool> enableOxygenSFXOnTheCompany;

        internal ConfigEntry<float> oxyCharger_SFXVolume;

        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > OxygenConfig");

        void DoSomethingAfterSync(object s, EventArgs e)
        {
            mls.LogWarning("Config was synced, imma updating moons values :)");
            RoundManagerPatch.UpdateMoonsValues();

            if (!MakeItVanilla.Value)
            {
                if (Instance.oxyBoost_price.Value != (int)Instance.oxyBoost_price.DefaultValue)
                {
                    mls.LogWarning("Updating price for oxyBoost");
                    OxygenBase.UpdateCustomItemPrice(OxygenBase.Instance.oxyBoost, Instance.oxyBoost_price.Value);
                }
            }
        }

        public OxygenConfig(ConfigFile file) : base(OxygenBase.modGUID)
        {
            ConfigManager.Register(this);
            SyncComplete += DoSomethingAfterSync;

            MakeItVanilla = file.Bind(
                "General", // Section
                "MakeItVanilla", // Key
                false, // Default value
                "If this is true, custom items from this mod will not load. " +
                "\nIt's not synced with the host, you need to manually change it." +
                "\nLeave it to 'false' if you want to play with a host who hasn't enabled it." // Description
            );

            OxygenFillOption = file.BindSyncedEntry(
                "General", // Section
                "OxygenFillOption", // Key
                1, // Default value
                "0 - without oxygen filling;" +
                "\n1 - only using oxygen cylinders located in the ship;" +
                "\n2 - only automatic oxygen filling when the player is on the ship;" +
                "\n(syncing with host)" // Description
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
                "Dine@0,0086;Rend@0,0086;Titan@0,009", // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenOutside config option." +
                "\nNot listed moons will use the decreasingOxygenOutside config option." +
                "\n(e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)" +
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
                "Dine@0,0086;Rend@0,0086;Titan@0,009", // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenInFactory config option." +
                "\nNot listed moons will use the decreasingOxygenInFactory config option." +
                "\n(e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)" +
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
                "Dine@0,006;Rend@0,006;Titan@0,008", // Default value
                "Increases oxygen drain when player running and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenRunning config option." +
                "\nNot listed moons will use the oxygenRunning config option." +
                "\n(e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)" +
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
                "\n(e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)" +
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

            InfinityOxygenInModsPlaces = file.BindSyncedEntry(
                "Compatibility", // Section
                "InfinityOxygenInModsPlaces", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to mod's places to simplificate gameplay." +
                "\n(syncing with host)" // Description
            );

            ShyHUDSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ShyHUDSupport", // Key
                true, // Default value
                "HUD disappears if oxygen value > 55" +
                "\n(syncing with host)" // Description
            );

            ImmersiveVisorSupport = file.BindSyncedEntry(
                "Compatibility", // Section
                "ImmersiveVisorSupport", // Key
                true, // Default value
                "Enables oxygen consumption when the helmet's crack level is 2." +
                "\n(syncing with host)" // Description
            );

            ImmersiveVisor_OxygenDecreasing = file.BindSyncedEntry(
                "Compatibility", // Section
                "ImmersiveVisor_OxygenDecreasing", // Key
                0.002f, // Default value
                "How much additional oxygen will be consumed if the helmet's crack level is 2?" +
                "\nDepends on the secTimer variable." +
                "\n(syncing with host)" // Description
            );

            EladsHUD_QuickFix = file.BindSyncedEntry(
                "Compatibility", // Section
                "EladsHUD_QuickFix", // Key
                true, // Default value
                "If EladsHUD is enabled, the mod will damage you by 0.1 instead of 0.3 amounts of oxygen, and it will give you slightly more oxygen" +
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
        }
    }
}
