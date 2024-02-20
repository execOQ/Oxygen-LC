using BepInEx.Configuration;
using BepInEx.Logging;

namespace Oxygen.ConfigBase
{
    public class Config
    {
        public readonly ConfigEntry<int> playerDamage;

        public readonly ConfigEntry<float> increasingOxygen;

        public readonly ConfigEntry<float> decreasingOxygen;

        public readonly ConfigEntry<float> oxygenRunning;

        public readonly ConfigEntry<float> oxygenDeficiency;

        public readonly ConfigEntry<float > secTimer;

        public readonly ConfigEntry<bool> notifications;

        public readonly ConfigEntry<bool> enableOxygenSFX;

        public readonly ConfigEntry<bool> enableOxygenSFXInShip;

        internal readonly ConfigFile File;
        
        private readonly ManualLogSource LogSource = new ManualLogSource($"{OxygenBase.modName} > Config");

        public Config(ConfigFile file)
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0010: Expected O, but got Unknown
            File = file;
            Logger.Sources.Add((ILogSource)(object)LogSource);

            new ConfigBuilder<int>(this)
                .SetSection("Player")
                .SetKey("playerDamage")
                .SetDefault(value: 15)
                .SetDescription("Sets how many damage player should get when he has no oxygen")
                .Build(out playerDamage);

            new ConfigBuilder<float>(this)
                .SetSection("Oxygen")
                .SetKey("increasingOxygen")
                .SetDefault(value: 0.001f)
                .SetDescription("How fast oxygen should be recovered. Happens every frame")
                .Build(out increasingOxygen);

            new ConfigBuilder<float>(this)
                .SetSection("Oxygen")
                .SetKey("decreasingOxygen")
                .SetDefault(value: 0.0083f)
                .SetDescription("How fast oxygen is decreasing. Depends on timer setting")
                .Build(out decreasingOxygen);

            new ConfigBuilder<float>(this)
                .SetSection("Oxygen")
                .SetKey("oxygenRunning")
                .SetDefault(value: 0.006f)
                .SetDescription("Increases oxygen drain when player running. Depends on timer setting")
                .Build(out oxygenRunning);

            new ConfigBuilder<float>(this)
                .SetSection("Oxygen")
                .SetKey("oxygenDeficiency")
                .SetDefault(value: 0.15f)
                .SetDescription("Increases oxygen drain when player running. Depends on timer setting")
                .Build(out oxygenDeficiency);

            new ConfigBuilder<float>(this)
                .SetSection("Timer")
                .SetKey("secTimer")
                .SetDefault(value: 5f)
                .SetDescription("Number of seconds the cool down timer lasts")
                .Build(out secTimer);

            new ConfigBuilder<bool>(this)
                .SetSection("Notifications")
                .SetKey("notifications")
                .SetDefault(value: true)
                .SetDescription("Should mod notify you if oxygen getting low?")
                .Build(out notifications);

            new ConfigBuilder<bool>(this)
                .SetSection("Sounds")
                .SetKey("enableOxygenSFX")
                .SetDefault(value: true)
                .SetDescription("Enables oxygen inhalation sounds")
                .Build(out enableOxygenSFX);

            new ConfigBuilder<bool>(this)
                .SetSection("Sounds")
                .SetKey("enableOxygenSFXInShip")
                .SetDefault(value: true)
                .SetDescription("Remains oxygen inhalation sounds when player in ship. Depends on enableOxygenSFX variable.")
                .Build(out enableOxygenSFXInShip);
        }
    }

    // thanks flerouwu for code
    internal class ConfigBuilder<T>
    {
        private static readonly ManualLogSource LogSource;

        private readonly Config Config;

        private T Default;

        private string Description;

        private string Key;

        private string Section;

        static ConfigBuilder()
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_000f: Expected O, but got Unknown
            LogSource = new ManualLogSource($"{OxygenBase.modName} > Config");
            Logger.Sources.Add((ILogSource)(object)LogSource);
        }

        public ConfigBuilder(Config config)
        {
            Config = config;
        }

        public void Build(out ConfigEntry<T> entry)
        {
            entry = Config.File.Bind<T>(Section, Key, Default, Description);
        }

        public bool TryGet(out ConfigEntry<T> entry)
        {
            return Config.File.TryGetEntry<T>(Section, Key, out entry);
        }

        public ConfigBuilder<T> SetSection(string section)
        {
            Section = section;
            return this;
        }

        public ConfigBuilder<T> SetKey(string key)
        {
            Key = key;
            return this;
        }

        public ConfigBuilder<T> SetDescription(string description)
        {
            Description = description;
            return this;
        }

        public ConfigBuilder<T> SetDefault(T value)
        {
            Default = value;
            return this;
        }
    }
}
