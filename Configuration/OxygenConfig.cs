using BepInEx.Configuration;
using BepInEx.Logging;
using CSync.Extensions;
using CSync.Lib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Oxygen.Configuration
{
    [DataContract]
    public class OxygenConfig : SyncedConfig2<OxygenConfig>
    {
        public readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > OxygenConfig");

        public enum AutoFillingOnShip
        {
            WhenDoorsClosed,
            WhenPlayerOnShip,
            Off
        }

        #region General
        internal ConfigEntry<bool> accurateMeter;

        [SyncedEntryField]
        internal SyncedEntry<AutoFillingOnShip> autoFillingOnShip;
        [SyncedEntryField]
        internal SyncedEntry<float> autoFillingOnShip_increasingOxygen;

        [SyncedEntryField]
        internal SyncedEntry<bool> recoverOxygen_ShipLeft;

        [SyncedEntryField]
        internal SyncedEntry<bool> recoverOxygen_StartOfRound;

        [SyncedEntryField]
        internal SyncedEntry<float> secTimer;

        [SyncedEntryField]
        internal SyncedEntry<int> playerDamage;

        private void BindGeneralEntries(ConfigFile file)
        {
            autoFillingOnShip = file.BindSyncedEntry(
                "General", // Section
                "autoFillingOnShip", // Key
                AutoFillingOnShip.WhenDoorsClosed, // Default value
                "Usually, on the ship, you use oxygen canisters to refill the oxygen, but with this option, you can choose some additional ways." +
                "\n\"WhenDoorsClosed\" - Auto filling works only if the ship's doors are closed." +
                "\"WhenPlayerOnShip\" - Works in both situations (doors are open or closed), the player just needs to be on the ship." +
                "\"Off\" - Disabled." +
                "\n(syncing with host)" // Description
            );

            autoFillingOnShip_increasingOxygen = file.BindSyncedEntry(
                "General", // Section
                "autoFillingOnShip_increasingOxygen", // Key
                0.002f, // Default value
                "How fast oxygen should be refilled when 'autoFillingOnShip' is not Off." +
                "\n(syncing with host)" // Description
            );

            accurateMeter = file.Bind(
                "General", // Section
                "accurateMeter", // Key
                true, // Default value
                "Whether the oxygen meter ring should be accurate or have vanilla behaviour." // Description
            );

            recoverOxygen_ShipLeft = file.BindSyncedEntry(
                "General", // Section
                "recoverOxygen_ShipLeft", // Key
                true, // Default value
                "If this is set to TRUE, oxygen will be recovered once ship has left." // Description
            );

            recoverOxygen_StartOfRound = file.BindSyncedEntry(
                "General", // Section
                "recoverOxygen_StartOfRound", // Key
                true, // Default value
                "If this is set to TRUE, oxygen will be recovered as soon as the round has started." // Description
            );

            secTimer = file.BindSyncedEntry(
                "General", // Section
                "secTimer", // Key
                5f, // Default value
                "Every how many seconds should the mod check the player's condition and take away some oxygen." +
                "\n(syncing with host)" // Description
            );

            playerDamage = file.BindSyncedEntry(
                "General", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen." +
                "\n(syncing with host)" // Description
            );
        }
        #endregion

        #region Oxygen
        [SyncedEntryField]
        internal SyncedEntry<string> greenPlanets;

        [SyncedEntryField]
        internal SyncedEntry<float> decreasingOxygenOutside;
        [SyncedEntryField]
        internal SyncedEntry<float> decreasingOxygenInFactory;

        [SyncedEntryField]
        internal SyncedEntry<string> decreasingOxygenOutsideMoons;
        [SyncedEntryField]
        internal SyncedEntry<string> decreasingOxygenInFactoryMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> runningMultiplier;
        [SyncedEntryField]
        internal SyncedEntry<string> runningMultiplierMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> oxygenDepletionInWater;
        [SyncedEntryField]
        internal SyncedEntry<string> oxygenDepletionInWaterMoons;

        [SyncedEntryField]
        internal SyncedEntry<float> decreasingInFear;

        [SyncedEntryField]
        internal SyncedEntry<float> oxygenDeficiency;

        private void BindOxygenEntries(ConfigFile file)
        {
            greenPlanets = file.BindSyncedEntry(
                "Oxygen", // Section
                "greenPlanets", // Key
                "March@0;Vow@0;Gordion@0", // Default value
                "Disables oxygen consumption outside on listed planets. " +
                "\nOxygen consumption by a player while underwater will still exist. The consumption caused by the damaged helmet (ImmersiveVisor) will be omitted." +
                "\nFollow the syntax of the default value." +
                "\n(syncing with host)" // Description
            );

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

            runningMultiplier = file.BindSyncedEntry(
                "Oxygen", // Section
                "runningMultiplier", // Key
                1f, // Default value
                "Multiplies oxygen drain when player is running." +
                "\n(syncing with host)" // Description
            );

            runningMultiplierMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "runningMultiplierMoons", // Key
                "Dine@1.2;Rend@1.2;Titan@1.25", // Default value
                "Multiplies oxygen drain when player is running." +
                "\nThis takes priority over the 'runningMultiplier' config option." +
                "\nNot listed moons will use the 'runningMultiplier' config option." +
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
        }
        #endregion

        #region Compatibilities
        [SyncedEntryField]
        internal SyncedEntry<bool> infinityOxygenInModsPlaces;

        internal ConfigEntry<bool> shyHUDSupport;

        [SyncedEntryField]
        internal SyncedEntry<bool> immersiveVisorSupport;
        [SyncedEntryField]
        internal SyncedEntry<float> immersiveVisor_OxygenDecreasing;

        private void BindCompatibilitiesEntries(ConfigFile file)
        {
            infinityOxygenInModsPlaces = file.BindSyncedEntry(
                "Compatibility", // Section
                "InfinityOxygenInModsPlaces", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to mod's places to simplificate gameplay." +
                "\n(syncing with host)" // Description
            );

            shyHUDSupport = file.Bind(
                "Compatibility", // Section
                "ShyHUDSupport", // Key
                true, // Default value
                "HUD disappears if oxygen value > 75" // previously 0.55f
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
        }
        #endregion

        #region OxyBoost
        [SyncedEntryField]
        internal SyncedEntry<float> oxyBoost_increasingValue;
        [SyncedEntryField]
        internal SyncedEntry<int> oxyBoost_price;

        private void BindOxyBoostEntries(ConfigFile file)
        {
            oxyBoost_increasingValue = file.BindSyncedEntry(
                "OxyBoost", // Section
                "OxyBoost_increasingValue", // Key
                0.001f, // Default value
                "How much oxygen does OxyBoost add to the player." +
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

        #endregion

        #region OxyCharger
        [SyncedEntryField]
        internal SyncedEntry<bool> oxyCharger_Enabled;

        internal ConfigEntry<float> oxyCharger_SFXVolume;

        private void BindOxyChargerEntries(ConfigFile file)
        {
            oxyCharger_Enabled = file.BindSyncedEntry(
                "OxyCharger", // Section
                "EnableOxyCharger", // Key
                true, // Default value
                "Should the oxyCharger be enabled? (oxygen canisters on the ship)" +
                "\n(syncing with host)" // Description
            );

            oxyCharger_SFXVolume = file.Bind(
                "OxyCharger", // Section
                "oxyCharger_SFXVolume", // Key
                1f, // Default value
                "OxyCharger's SFX volume" // Description
            );
        }
        #endregion

        #region Sounds
        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableInhaleSFXWhileWalking;
        internal ConfigEntry<bool> enableOxygenSFXInShip;
        internal ConfigEntry<bool> enableOxygenSFXOnTheCompany;

        internal ConfigEntry<float> walkingSFX_volume, runningSFX_volume, exhaustedSFX_volume, scaredSFX_volume;

        private void BindSoundsEntries(ConfigFile file)
        {
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
        }

        #endregion

        #region Weather complications
        [SyncedEntryField]
        internal SyncedEntry<bool> eclipsed_LimitOxygen;
        [SyncedEntryField]
        internal SyncedEntry<float> eclipsed_LimitedOxygenAmount;

        private void BindWeatherComplicationsEntries(ConfigFile file)
        {
            eclipsed_LimitOxygen = file.BindSyncedEntry(
                "Weather Complications", // Section
                "LimitOxygenOnEclipsed", // Key
                true, // Default value
                "The oxygen recovery ability will be limited by 'oxyCharger_RemainedOxygenAmount' option if the weather on the current level is eclipsed." +
                "\n(syncing with host)" // Description
            );

            eclipsed_LimitedOxygenAmount = file.BindSyncedEntry(
                "OxyCharger", // Section
                "LimitedOxygenAmountOnEclipsed", // Key
                10f, // Default value
                "How much oxygen you will have in such rounds if \"eclipsed_LimitOxygen\" is enabled." +
                "\n(syncing with host)" // Description
            );
        }
        #endregion

        #region Other
        internal ConfigEntry<bool> notifications;

        // Vanilla HUD offsets
        internal ConfigEntry<int> XOffset;
        internal ConfigEntry<int> YOffset;

        private void BindOtherEntries(ConfigFile file)
        {
            notifications = file.Bind(
                "Other", // Section
                "notifications", // Key
                true, // Default value
                "Should the mod notify you when oxygen is getting low?" // Description
            );

            XOffset = file.Bind(
                "Other",
                "XOffset",
                0,
                "Horizontal offset for the oxygenHUD position."
            );

            YOffset = file.Bind(
                "Other",
                "YOffset",
                0,
                "Vertical offset for the oxygenHUD position."
            );
        }
        #endregion

        private void RemoveOldConfigEntries(ConfigFile cfg)
        {
            mls.LogInfo("Looking for outdated entries in the config.");

            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            foreach (var item in orphanedEntries)
            {
                mls.LogDebug(item.Key.Section + " | " + item.Key + " | " + item.Value);
                if (item.Key.Key == "MakeItVanilla" && item.Key.Section == "Oxygen")
                {
                    mls.LogWarning("Found old \"MakeItVanilla\". Moving the value to the new entry \"autoFillingOnShip\".");
                    if (int.TryParse(item.Value, out int result))
                    {
                        autoFillingOnShip_increasingOxygen.LocalValue = result;
                        if (result == 0)
                        {
                            autoFillingOnShip.LocalValue = AutoFillingOnShip.Off;
                        }
                        else if (result == 1)
                        {
                            autoFillingOnShip.LocalValue = AutoFillingOnShip.WhenDoorsClosed;
                        }
                        else
                        {
                            autoFillingOnShip.LocalValue = AutoFillingOnShip.WhenPlayerOnShip;
                        }
                    }
                }
                if (item.Key.Key == "increasingOxygen" && item.Key.Section == "Oxygen")
                {
                    mls.LogWarning("Found old \"increasingOxygen\". Moving the value to the new entry \"autoFillingOnShip_increasingOxygen\".");
                    if (float.TryParse(item.Value, out float result))
                    {
                        autoFillingOnShip_increasingOxygen.LocalValue = result;
                    }
                }
                if (item.Key.Key == "recoverOxygenOnceShipLeft" && item.Key.Section == "General")
                {
                    mls.LogWarning("Found old \"recoverOxygenOnceShipLeft\". Moving the value to the new entry \"recoverOxygen_ShipLeft\".");
                    if (bool.TryParse(item.Value, out bool result))
                    {
                        recoverOxygen_ShipLeft.LocalValue = result;
                    }
                }
                if (item.Key.Key == "playerDamage" && item.Key.Section == "Player")
                {
                    mls.LogWarning("Found old \"playerDamage\". Moving the entry to the new section \"General\".");
                    if (int.TryParse(item.Value, out int result))
                    {
                        playerDamage.LocalValue = result;
                    }
                }
                if (item.Key.Key == "oxyCharger_SFXVolume" && item.Key.Section == "Sounds")
                {
                    mls.LogWarning("Found old \"oxyCharger_SFXVolume\". Moving the entry to the new section \"OxyCharger\".");
                    if (int.TryParse(item.Value, out int result))
                    {
                        oxyCharger_SFXVolume.Value = result;
                    }
                }
                if (item.Key.Key == "notifications" && item.Key.Section == "Notifications")
                {
                    mls.LogWarning("Found old \"notifications\". Moving the entry to the new section \"Other\".");
                    if (bool.TryParse(item.Value, out bool result))
                    {
                        notifications.Value = result;
                    }
                }
                if (item.Key.Key == "XOffset" && item.Key.Section == "Position")
                {
                    mls.LogWarning("Found old \"XOffset\". Moving the entry to the new section \"Other\".");
                    if (int.TryParse(item.Value, out int result))
                    {
                        XOffset.Value = result;
                    }
                }
                if (item.Key.Key == "YOffset" && item.Key.Section == "Position")
                {
                    mls.LogWarning("Found old \"YOffset\". Moving the entry to the new section \"Other\".");
                    if (int.TryParse(item.Value, out int result))
                    {
                        YOffset.Value = result;
                    }
                }
            }

            orphanedEntries.Clear();
            cfg.Save();
        }

        void DoSomethingAfterSync(object s, EventArgs e)
        {
            mls.LogInfo("Config was synced, updating moons value!");
            MoonsDicts.UpdateMoonsValues();

            if (oxyBoost_price.Value != oxyBoost_price.LocalValue)
            {
                mls.LogInfo("Updating price for OxyBoost");
                OxygenBase.UpdateCustomItemPrice(OxygenBase.Instance.oxyBoost, oxyBoost_price.Value);
            }
        } 

        public OxygenConfig(ConfigFile file) : base(OxygenBase.modGUID)
        {
            InitialSyncCompleted += DoSomethingAfterSync;

            BindGeneralEntries(file);
            BindOxygenEntries(file);
            BindSoundsEntries(file);
            BindCompatibilitiesEntries(file);
            BindOxyBoostEntries(file);
            BindOxyChargerEntries(file);
            BindWeatherComplicationsEntries(file);
            BindOtherEntries(file);

            RemoveOldConfigEntries(file);

            ConfigManager.Register(this);
        }
    }
}
