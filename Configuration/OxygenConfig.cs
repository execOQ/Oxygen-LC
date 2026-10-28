using BepInEx.Configuration;
using BepInEx.Logging;
using Oxygen.General;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxygen.Configuration
{
    public class OxygenConfig
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

        internal ConfigEntry<bool> autoHideHUD;

        internal ConfigEntry<float> autoHideHUD_value;

        internal HostConfigEntry<AutoFillingOnShip> autoFillingOnShip;
        internal HostConfigEntry<float> autoFillingOnShip_increasingOxygen;

        internal HostConfigEntry<bool> recoverOxygen_ShipLeft;

        internal HostConfigEntry<bool> recoverOxygen_StartOfRound;

        internal HostConfigEntry<float> secTimer;

        internal HostConfigEntry<int> playerDamage;
        internal HostConfigEntry<float> transferRate;

        private void BindGeneralEntries(ConfigFile file)
        {
            transferRate = BindHostEntry(file, "Oxygen transfer", "transferRate", 0.05f,
                "Fraction of a tank transferred per second (0.001-1). Synced with host; default 5% per second.",
                new AcceptableValueRange<float>(0.001f, 1f));
            autoFillingOnShip = BindHostEntry(file,
                "General", // Section
                "autoFillingOnShip", // Key
                AutoFillingOnShip.WhenDoorsClosed, // Default value
                "Usually, on the ship, you use oxygen canisters to refill the oxygen, but with this option, you can choose some additional ways." +
                "\n\n\"WhenDoorsClosed\" - Auto filling works only if the ship's doors are closed." +
                "\n\"WhenPlayerOnShip\" - Works in both situations (doors are open or closed), the player just needs to be on the ship." +
                "\n\"Off\" - Disabled." +
                "\n\n(syncing with host)" // Description
            );

            autoFillingOnShip_increasingOxygen = BindHostEntry(file,
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

            autoHideHUD = file.Bind(
                "General", // Section
                "autoHideHUD", // Key
                false, // Default value
                "If true, the oxygen HUD disappears if the oxygen value > 'autoHideHUD_value' setting. " +
                "\nWorks without installing ShyHUD. If ShyHUD is installed, but this option is not enabled, the HUD won't disappear" // Description
            );

            autoHideHUD_value = file.Bind(
                "General", // Section
                "autoHideHUD_value", // Key
                0.75f, // Default value
                "The oxygen HUD disappears if the oxygen value > 'autoHideHUD_value' setting (value from 0 to 1.0)." +
                "\nYou can completely hide the oxygen HUD by setting the 'autoHideHUD_value' to 0." +
                "\nIf you set the value to 1, this will prevent the oxygen HUD from disappearing." // Description
            );

            recoverOxygen_ShipLeft = BindHostEntry(file,
                "General", // Section
                "recoverOxygen_ShipLeft", // Key
                true, // Default value
                "If this is set to TRUE, oxygen will be recovered once ship has left." // Description
            );

            recoverOxygen_StartOfRound = BindHostEntry(file,
                "General", // Section
                "recoverOxygen_StartOfRound", // Key
                true, // Default value
                "If this is set to TRUE, oxygen will be recovered as soon as the round has started." // Description
            );

            secTimer = BindHostEntry(file,
                "General", // Section
                "secTimer", // Key
                5f, // Default value
                "Every how many seconds should the mod check the player's condition and take away some oxygen." +
                "\n(syncing with host)" // Description
            );

            playerDamage = BindHostEntry(file,
                "General", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen." +
                "\n(syncing with host)" // Description
            );
        }
        #endregion

        #region Oxygen
        internal HostConfigEntry<string> greenPlanets;

        internal HostConfigEntry<float> decreasingOxygenOutside;
        internal HostConfigEntry<float> decreasingOxygenInFactory;

        internal HostConfigEntry<string> decreasingOxygenOutsideMoons;
        internal HostConfigEntry<string> decreasingOxygenInFactoryMoons;

        internal HostConfigEntry<float> runningMultiplier;
        internal HostConfigEntry<string> runningMultiplierMoons;

        internal HostConfigEntry<float> oxygenDepletionInWater;
        internal HostConfigEntry<string> oxygenDepletionInWaterMoons;

        internal HostConfigEntry<float> decreasingInFear;

        internal HostConfigEntry<float> oxygenDeficiency;

        private void BindOxygenEntries(ConfigFile file)
        {
            greenPlanets = BindHostEntry(file,
                "Oxygen", // Section
                "greenPlanets", // Key
                "March@0;Vow@0;Gordion@0", // Default value
                "Disables oxygen consumption outside on listed planets. " +
                "\nOxygen consumption by a player while underwater will still exist. The consumption caused by the damaged helmet (ImmersiveVisor) will be omitted." +
                "\nFollow the syntax of the default value." +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenOutside = BindHostEntry(file,
                "Oxygen", // Section
                "decreasingOxygenOutside", // Key
                0.0075f, // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenOutsideMoons = BindHostEntry(file,
                "Oxygen", // Section
                "decreasingOxygenOutsideMoons", // Key
                "Dine@0.0086;Rend@0.0086;Titan@0.009", // Default value
                "Indicates how much oxygen is consumed when a player is outside and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenOutside config option." +
                "\nNot listed moons will use the decreasingOxygenOutside config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenInFactory = BindHostEntry(file,
                "Oxygen", // Section
                "decreasingOxygenInFactory", // Key
                0.007f, // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            decreasingOxygenInFactoryMoons = BindHostEntry(file,
                "Oxygen", // Section
                "decreasingOxygenInFactoryMoons", // Key
                "Dine@0.0086;Rend@0.0086;Titan@0.009", // Default value
                "Indicates how much oxygen is consumed when a player is in the facility and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the decreasingOxygenInFactory config option." +
                "\nNot listed moons will use the decreasingOxygenInFactory config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            decreasingInFear = BindHostEntry(file,
                "Oxygen", // Section
                "decreasingInFear", // Key
                0.01f, // Default value
                "Increases oxygen leakage when the player is in fear and is triggered every 2 seconds." +
                "\n(syncing with host)" // Description
            );

            runningMultiplier = BindHostEntry(file,
                "Oxygen", // Section
                "runningMultiplier", // Key
                1f, // Default value
                "Multiplies oxygen drain when player is running." +
                "\n(syncing with host)" // Description
            );

            runningMultiplierMoons = BindHostEntry(file,
                "Oxygen", // Section
                "runningMultiplierMoons", // Key
                "Dine@1.2;Rend@1.2;Titan@1.25", // Default value
                "Multiplies oxygen drain when player is running." +
                "\nThis takes priority over the 'runningMultiplier' config option." +
                "\nNot listed moons will use the 'runningMultiplier' config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            oxygenDepletionInWater = BindHostEntry(file,
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.018f, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds." +
                "\n(syncing with host)" // Description
            );

            oxygenDepletionInWaterMoons = BindHostEntry(file,
                "Oxygen", // Section
                "oxygenDepletionInWaterMoons", // Key
                string.Empty, // Default value
                "Increases oxygen consumption when the player is underwater and is triggered every secTimer (config option) seconds." +
                "\nThis takes priority over the oxygenDepletionInWater config option." +
                "\nNot listed moons will use the oxygenDepletionInWater config option." +
                "\n(e.g. Experimentation@1.9;Vow@0.09;CUSTOM_MOON_NAME@10)" +
                "\n(syncing with host)" // Description
            );

            oxygenDeficiency = BindHostEntry(file,
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
        internal HostConfigEntry<bool> infinityOxygenInModsPlaces;

        internal HostConfigEntry<bool> immersiveVisorSupport;
        internal HostConfigEntry<float> immersiveVisor_OxygenDecreasing;

        private void BindCompatibilitiesEntries(ConfigFile file)
        {
            infinityOxygenInModsPlaces = BindHostEntry(file,
                "Compatibility", // Section
                "InfinityOxygenInModsPlaces", // Key
                true, // Default value
                "Oxygen becomes infinite when the player teleports to mod's places to simplificate gameplay." +
                "\n(syncing with host)" // Description
            );

            immersiveVisorSupport = BindHostEntry(file,
                "Compatibility", // Section
                "ImmersiveVisorSupport", // Key
                true, // Default value
                "Enables oxygen consumption when the helmet's crack level is 2." +
                "\n(syncing with host)" // Description
            );

            immersiveVisor_OxygenDecreasing = BindHostEntry(file,
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
        internal HostConfigEntry<bool> enable_OxyBoost;
        internal HostConfigEntry<float> oxyBoost_increasingValue;
        internal HostConfigEntry<int> oxyBoost_price;

        private void BindOxyBoostEntries(ConfigFile file)
        {
            enable_OxyBoost = BindHostEntry(file,
                "OxyBoost", // Section
                "EnableOxyBoost", // Key
                true, // Default value
                "Enables the OxyBoost" // Description
            );

            oxyBoost_increasingValue = BindHostEntry(file,
                "OxyBoost", // Section
                "OxyBoost_increasingValue", // Key
                0.001f, // Default value
                "How much oxygen does OxyBoost add to the player." +
                "\n(syncing with host)" // Description
            );

            oxyBoost_price = BindHostEntry(file,
                "OxyBoost", // Section
                "OxyBoost_price", // Key
                70, // Default value
                "OxyBoost's price." +
                "\n(syncing with host)" // Description
            );
        }

        #endregion

        #region OxyCharger
        internal HostConfigEntry<bool> oxyCharger_Enabled;

        internal ConfigEntry<float> oxyCharger_SFXVolume;

        private void BindOxyChargerEntries(ConfigFile file)
        {
            oxyCharger_Enabled = BindHostEntry(file,
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
        internal ConfigEntry<bool> enableLowOxygenAlarm;
        internal ConfigEntry<float> lowOxygenAlarm_volume;

        internal ConfigEntry<bool> enableInhaleSFXWhileWalking;
        internal ConfigEntry<bool> enableOxygenSFXInShip;
        internal ConfigEntry<bool> enableOxygenSFXOnTheCompany;

        internal ConfigEntry<float> walkingSFX_volume, runningSFX_volume, exhaustedSFX_volume, scaredSFX_volume;

        private void BindSoundsEntries(ConfigFile file)
        {
            enableLowOxygenAlarm = file.Bind(
                "Sounds",
                "enableLowOxygenAlarm",
                true,
                "Enables the repeating low-oxygen alarm. Set to false to stop and completely disable this alarm." +
                "\nPlays below 20% oxygen every 5 seconds, and below 10% every 2 seconds." +
                "\nWorks with the HUD hidden and independently of inhalation sounds and notifications."
            );

            lowOxygenAlarm_volume = file.Bind(
                "Sounds",
                "lowOxygenAlarm_volume",
                0.1f,
                new ConfigDescription("Volume of the low-oxygen alarm. 0 = muted, 1 = maximum volume. Default: 0.1 (10%).", new AcceptableValueRange<float>(0f, 1f))
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
                false, // Default value
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
        internal HostConfigEntry<bool> eclipsed_LimitOxygen;
        internal HostConfigEntry<float> eclipsed_LimitedOxygenAmount;

        private void BindWeatherComplicationsEntries(ConfigFile file)
        {
            eclipsed_LimitOxygen = BindHostEntry(file,
                "Weather Complications", // Section
                "LimitOxygenOnEclipsed", // Key
                true, // Default value
                "The oxygen recovery ability will be limited by 'oxyCharger_RemainedOxygenAmount' option if the weather on the current level is eclipsed." +
                "\n(syncing with host)" // Description
            );

            eclipsed_LimitedOxygenAmount = BindHostEntry(file,
                "Weather Complications", // Section
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

        internal HostConfigEntry<bool> enableDieEarlyUI;
        internal ConfigEntry<float> dieEarlyUI_Timer;

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

            enableDieEarlyUI = BindHostEntry(file,
                "Other", // Section
                "EnableDieEarlyUI", // Key
                true, // Default value
                "If you can't get out from under the water for a long time, you could just kill yourself :)" +
                "\n(syncing with host)" // Description
            );

            dieEarlyUI_Timer = file.Bind(
                "Other", // Section
                "dieEarlyUI_Timer", // Key
                60f, // Default value
                "After selected number of seconds the UI to die early would appear." // Description
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

        internal ConfigSync Sync { get; private set; }
        private readonly List<IHostConfigEntry> hostEntries = new List<IHostConfigEntry>();

        private HostConfigEntry<T> BindHostEntry<T>(ConfigFile file, string section, string key, T defaultValue, string description,
            AcceptableValueBase range = null)
        {
            var entry = new HostConfigEntry<T>(file.Bind(section, key, defaultValue, new ConfigDescription(description, range)));
            hostEntries.Add(entry);
            entry.Entry.SettingChanged += (sender, args) => Sync?.OnLocalSettingChanged();
            return entry;
        }

        internal void RefreshEffects(bool updateCharger = true)
        {
            MoonsDicts.UpdateMoonsValues();
            if (OxygenBase.Instance == null || StartOfRound.Instance == null) return;
            OxygenBase.Instance.UpdateOxyBoostShop();
            if (!updateCharger) return;
            ChargerUnlockable.Refresh();
            WeatherHandler.RefreshLimit();
        }

        internal void ValidateHostSettings(IReadOnlyDictionary<IHostConfigEntry, object> values)
            => MoonsDicts.Prepare(this, values);

        internal void ResetSessionState()
        {
            MoonsDicts.Clear();
            WeatherHandler.Reset();
            ChargerUnlockable.Reset();
            VanillaCanisters.Reset();
            OxyBoostShop.Reset();
        }
        public OxygenConfig(ConfigFile file)
        {

            BindGeneralEntries(file);
            BindOxygenEntries(file);
            BindSoundsEntries(file);
            BindCompatibilitiesEntries(file);
            BindOxyBoostEntries(file);
            BindOxyChargerEntries(file);
            BindWeatherComplicationsEntries(file);
            BindOtherEntries(file);

            RemoveOldConfigEntries(file);

            Sync = new ConfigSync(this, hostEntries);
        }
    }
}
