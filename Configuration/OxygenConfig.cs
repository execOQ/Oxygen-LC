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
        internal SyncedEntry<string> decreasingInFearMoons;

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
        internal SyncedEntry<bool> oxygenConsumptionOnTheCompany;

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

        [DataMember]
        internal SyncedEntry<float> oxyCharger_fillingValue;

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
                "If this is true, custom items from this mod will not load. It's not synced with the host, you need to manually change it. Leave it to 'false' if you want to play with a host who hasn't enabled it." // Description
            );

            OxygenFillOption = file.BindSyncedEntry(
                "General", // Section
                "OxygenFillOption", // Key
                1, // Default value
                "0 - without oxygen filling; 1 - only using oxygen cylinders located in the ship; 2 - only automatic oxygen filling when the player is on the ship" // Description
            );

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

            increasingOxygenMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygenMoons", // Key
                "Experimentation:2.0,Vow:0.9", // Default value
                "How fast oxygen should be recovered. Happens every frame. (syncing with host)" // Description
            );

            decreasingOxygenOutside = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutside", // Key
                0.0083f, // Default value
                "How much oxygen should be released when the timer (The timing of the timer is the secTimer variable) goes off? (syncing with host)" // Description
            );

            decreasingOxygenInFactory = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactory", // Key
                0.0083f, // Default value
                "How much oxygen should be released when the timer (The timing of the timer is the secTimer variable) goes off? (syncing with host)" // Description
            );

            decreasingOxygenOutsideMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenOutsideMoons", // Key
                string.Empty, // Default value
                "How much oxygen should be released when the timer (The timing of the timer is the secTimer variable) goes off? (syncing with host)" // Description
            );

            decreasingOxygenInFactoryMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenInFactoryMoons", // Key
                string.Empty, // Default value
                "How much oxygen should be released when the timer (The timing of the timer is the secTimer variable) goes off? (syncing with host)" // Description
            );

            decreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingInFear", // Key
                0.02f, // Default value
                "Increases oxygen leakage when the player is in fear. Depends on the secTimer variable. (syncing with host)" // Description
            );

            decreasingInFearMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingInFearMoons", // Key
                "Experimentation:1.0", // Default value
                "How fast oxygen should be recovered. Happens every frame. (syncing with host)" // Description
            );

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.006f, // Default value
                "Increases oxygen drain when player running. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenRunningMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunningMoons", // Key
                "Experimentation:1.0, March:22", // Default value
                "How fast oxygen should be recovered. Happens every frame. (syncing with host)" // Description
            );

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen consumption when the player is underwater. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenDepletionInWaterMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWaterMoons", // Key
                "Experimentation:1.0, March:5", // Default value
                "Increases oxygen consumption when the player is underwater. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases screen fog when the player runs out of oxygen. Depends on the secTimer variable. (syncing with host)" // Description
            );

            oxygenConsumptionOnTheCompany = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenConsumptionOnTheCompany", // Key
                true, // Default value
                "If true, then there will be oxygen consumption on the company's planet. (syncing with host)" // Description
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

            oxyCharger_fillingValue = file.BindSyncedEntry(
                "oxyCharger", // Section
                "oxyCharger_fillingValue", // Key
                0.001f, // Default value
                "oxyCharger's filling value" // Description
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
