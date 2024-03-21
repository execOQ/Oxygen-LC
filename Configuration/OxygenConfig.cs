using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Lib;
using CSync.Util;
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
        internal SyncedEntry<int> playerDamage;

        [DataMember]
        internal SyncedEntry<string> greenPlanets;

        [DataMember]
        internal SyncedEntry<float> increasingOxygen;

        [DataMember]
        internal SyncedEntry<string> increasingOxygenMoons;

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
        internal SyncedEntry<float> oxyBoost_increasingValue;

        [DataMember]
        internal SyncedEntry<int> oxyBoost_price;

        internal ConfigEntry<int> XOffset;

        internal ConfigEntry<int> YOffset;

        internal ConfigEntry<bool> notifications;

        internal ConfigEntry<float> SFXvolume;

        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableOxygenSFXInShip;

        internal ConfigEntry<bool> enableOxygenSFXOnTheCompany;

        internal ConfigEntry<float> oxyCharger_SFXVolume;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public OxygenConfig(ConfigFile file) : base(OxygenBase.modGUID)
        {
            ConfigManager.Register(this);

            MakeItVanilla = file.Bind(
                "General", // Section
                "MakeItVanilla", // Key
                false, // Default value
                "If this is true, custom items from this mod will not load. It's not synced with the host, you need to manually change it.\nLeave it to 'false' if you want to play with a host who hasn't enabled it." // Description
            );

            OxygenFillOption = file.BindSyncedEntry(
                "General", // Section
                "OxygenFillOption", // Key
                1, // Default value
                "0 - without oxygen filling;\n1 - only using oxygen cylinders located in the ship;\n2 - only automatic oxygen filling when the player is on the ship;" // Description
            );

            playerDamage = file.BindSyncedEntry(
                "Player", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen. (syncing with host)" // Description
            );

            greenPlanets = file.BindSyncedEntry(
                "Oxygen", // Section
                "greenPlanets", // Key
                "March@0;Vow@0;Gordion@0", // Default value
                "Disables oxygen consumption outside on listed planets. Follow the syntax of the default value. (syncing with host)" // Description
            );

            increasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygen", // Key
                0.001f, // Default value
                "How fast oxygen should be recovered when OxygenFillOption is set to 2. (syncing with host)" // Description
            );

            increasingOxygenMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygenMoons", // Key
                string.Empty, // Default value
                "How fast oxygen is recovered when OxygenFillOption is set to 2." +
                "\nThis takes priority over the increasingOxygen config option (e.g. Experimentation@2.0;Vow@0.9;CUSTOM_MOON_NAME@10)." +
                "\nNot listed moons will use the increasingOxygen config option." // Description
            );

            decreasingOxygenOutside = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutside", // Key
                0.0083f, // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds. (syncing with host)" // Description
            );

            decreasingOxygenOutsideMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutsideMoons", // Key
                "Dine@0,0086;Rend@0,0086;Titan@0,009", // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenOutside config option (e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)." +
                "\nNot listed moons will use the decreasingOxygenOutside config option. (syncing with host)" // Description
            );
            
            decreasingOxygenInFactory = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactory", // Key
                0.0083f, // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds. (syncing with host)" // Description
            );

            decreasingOxygenInFactoryMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactoryMoons", // Key
                "Dine@0,0086;Rend@0,0086;Titan@0,009", // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenInFactory config option (e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)." +
                "\nNot listed moons will use the decreasingOxygenInFactory config option. (syncing with host)" // Description
            );

            decreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingInFear", // Key
                0.02f, // Default value
                "Increases oxygen leakage when the player is in fear and is triggered every 2 seconds. (syncing with host)" // Description
            );

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.004f, // Default value
                "Increases oxygen drain when player running and is triggered every secTimer (config option) seconds. (syncing with host)" // Description
            );

            oxygenRunningMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunningMoons", // Key
                "Dine@0,006;Rend@0,006;Titan@0,008", // Default value
                "Increases oxygen drain when player running and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenRunning config option (e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)." +
                "\nNot listed moons will use the oxygenRunning config option. (syncing with host)" // Description
            );

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds. (syncing with host)" // Description
            );

            oxygenDepletionInWaterMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWaterMoons", // Key
                string.Empty, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenDepletionInWater config option (e.g. Experimentation@2,0;Vow@0,9;CUSTOM_MOON_NAME@10)." +
                "\nNot listed moons will use the oxygenDepletionInWater config option. (syncing with host)" // Description
            );

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases screen fog when the player runs out of oxygen. Depends on the secTimer variable. (syncing with host)" // Description
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
                0.4f, // Default value
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

            enableOxygenSFXOnTheCompany = file.Bind(
                "Sounds", // Section
                "enableOxygenSFXOnTheCompany", // Key
                true, // Default value
                "Remains oxygen inhalation sounds when player on the Gordion (The Company) planet. Works if enableOxygenSFX variable is enabled." // Description
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

            XOffset = file.Bind<int>(
                "Position", 
                "XOffset", 
                0, 
                "Horizontal offset for the oxygenHUD position."
            );

            YOffset = file.Bind<int>(
                "Position", 
                "YOffset", 
                0,
                "Vertical offset for the oxygenHUD position."
            );

            oxyBoost_increasingValue = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_increasingValue", // Key
                0.001f, // Default value
                "How much oxygen does OxyBoost add to a player" // Description
            );

            oxyBoost_price = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_price", // Key
                70, // Default value
                "OxyBoost's price" // Description
            );

            oxyCharger_SFXVolume = file.Bind(
                "Sounds", // Section
                "oxyCharger_SFXVolume", // Key
                1f, // Default value
                "oxyCharger's SFX volume" // Description
            );
        }
    }
}
