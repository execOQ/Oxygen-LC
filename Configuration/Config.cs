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

namespace Oxygen.Configuration
{
    [DataContract]
    public class Config : SyncedInstance<Config>
    {
        //[DataMember]
        //internal SyncedEntry<bool> useMoonsValues;

        // 
        [DataMember]
        internal SyncedEntry<int> playerDamage;

        //[DataMember]
        //internal SyncedEntry<string> playerDamageMoons;

        [DataMember]
        internal SyncedEntry<float> increasingOxygen;

        //[DataMember]
        //internal SyncedEntry<string> increasingOxygenMoons;

        [DataMember]
        internal SyncedEntry<float> decreasingOxygen;

        //[DataMember]
        //internal SyncedEntry<string> decreasingOxygenMoons;

        [DataMember]
        internal SyncedEntry<float> multiplyDecreasingInFear;

        //[DataMember]
        //internal SyncedEntry<string> multiplyDecreasingInFearMoons;

        [DataMember]
        internal SyncedEntry<float> oxygenRunning;

        //[DataMember]
        //internal SyncedEntry<string> oxygenRunningMoons;

        [DataMember]
        internal SyncedEntry<float> oxygenDepletionInWater;

        //[DataMember]
        //internal SyncedEntry<string> oxygenDepletionInWaterMoons;

        [DataMember]
        internal SyncedEntry<float> oxygenDeficiency;

        //[DataMember]
        //internal SyncedEntry<string> oxygenDeficiencyMoons;

        //

        [DataMember]
        internal SyncedEntry<float> secTimer;

        [DataMember]
        internal SyncedEntry<bool> InfinityOxygenInbackrooms;

        internal ConfigEntry<bool> notifications;

        internal ConfigEntry<float> SFXvolume;

        internal ConfigEntry<bool> enableOxygenSFX;

        internal ConfigEntry<bool> enableOxygenSFXInShip;

        public static ManualLogSource mls = OxygenBase.Instance.mls;

        public Config(ConfigFile file)
        {
            InitInstance(this);

            /*
            useMoonsValues = file.BindSyncedEntry(
                "Settings", // Section
                "useMoonsValues", // Key
                true, // Default value
                "Should mod use different values on each moon? If true then mod will use setting from moons.json. Creates after creating a lobby." // Description
            );
            */

            playerDamage = file.BindSyncedEntry(
                "Player", // Section
                "playerDamage", // Key
                15, // Default value
                "Sets how many damage player should get when he has no oxygen. (syncing with host)" // Description
            );

            /*
            playerDamageMoons = file.BindSyncedEntry(
                "Player", // Section
                "playerDamageMoons", // Key
                "null", // Default value
                "Sets how many damage player should get when he has no oxygen for each moon. Enables if useMoonsValues is enabled. Example: 'Experimentation-15,Secret Labs-15'" // Description
            );
            */

            increasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygen", // Key
                0.001f, // Default value
                "How fast oxygen should be recovered. Happens every frame. (syncing with host)" // Description
            );

            /*
            increasingOxygenMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "increasingOxygenMoons", // Key
                "null", // Default value
                "Sets how many damage player should get when he has no oxygen for each moon. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.001f,Secret Labs-0.001f'" // Description
            );
            */

            decreasingOxygen = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygen", // Key
                0.0083f, // Default value
                "How fast oxygen is decreasing. Depends on timer setting. (syncing with host)" // Description
            );

            /*
            decreasingOxygenMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "decreasingOxygenMoons", // Key
                "null", // Default value
                "Sets how many damage player should get when he has no oxygen for each moon. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.0083f,Secret Labs-0.0083f'" // Description
            );
            */

            multiplyDecreasingInFear = file.BindSyncedEntry(
                "Oxygen", // Section
                "multiplyDecreasingInFear", // Key
                0.02f, // Default value
                "Multiplies oxygen drain when player in fear. Depends on timer setting. (syncing with host)" // Description
            );

            /*
            multiplyDecreasingInFearMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "multiplyDecreasingInFearMoons", // Key
                "null", // Default value
                "Multiplies oxygen drain when player in fear for each moon. Depends on timer setting. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.006f,Secret Labs-0.006f'" // Description
            );
            */

            oxygenRunning = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunning", // Key
                0.006f, // Default value
                "Increases oxygen drain when player running. Depends on timer setting. (syncing with host)" // Description
            );

            /*
            oxygenRunningMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenRunningMoons", // Key
                "null", // Default value
                "Multiplies oxygen drain when player in fear for each moon. Depends on timer setting. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.006f,Secret Labs-0.006f'" // Description
            );
            */

            oxygenDeficiency = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiency", // Key
                0.15f, // Default value
                "Increases oxygen deficiency when the player's oxygen runs out. Depends on timer setting. (syncing with host)" // Description
            );

            /*
            oxygenDeficiencyMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDeficiencyMoons", // Key
                "null", // Default value
                "Multiplies oxygen drain when player in fear for each moon. Depends on timer setting. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.15f,Secret Labs-0.15f'" // Description
            );
            */

            oxygenDepletionInWater = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWater", // Key
                0.020f, // Default value
                "Increases oxygen deficiency when the player underwater. Depends on timer setting. (syncing with host)" // Description
            );

            /* 
            oxygenDepletionInWaterMoons = file.BindSyncedEntry(
                "Oxygen", // Section
                "oxygenDepletionInWaterMoons", // Key
                "null", // Default value
                "Multiplies oxygen drain when player in fear for each moon. Depends on timer setting. Enables if useMoonsValues is enabled. Example: 'Experimentation-0.020f,Secret Labs-0.020f'" // Description
            ); 
            */

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

    public class ConfigHelper
    {
        public static ManualLogSource mls = OxygenBase.Instance.mls;

        static dynamic ExtractValueInt(string moon, string text)
        {
            dynamic value = null;

            if (moon == "null" || moon == "")
            {
                string[] pairs = text.Split(',');

                foreach (string pair in pairs)
                {
                    string[] parts = pair.Split('-');

                    if (parts.Length == 2)
                    {
                        string moonName = parts[0].Trim();
                        string moonValue = parts[1].Trim();

                        mls.LogError($"moonName {moonName}");
                        mls.LogError($"moonValue {moonValue}");

                        if (moonName == moon)
                        {
                            if (int.TryParse(moonValue, out var result))
                            {
                                value = result;
                            }

                            break;
                        }
                    }
                }
            }

            return value;
        }

        static dynamic ExtractValueFloat(string moon, string text)
        {
            dynamic value = null;

            if (moon == "null" || moon == "")
            {
                string[] pairs = text.Split(',');

                foreach (string pair in pairs)
                {
                    string[] parts = pair.Split('-');

                    if (parts.Length == 2)
                    {
                        string moonName = parts[0].Trim();
                        string moonValue = parts[1].Trim();

                        mls.LogError($"moonName {moonName}");
                        mls.LogError($"moonValue {moonValue}");

                        if (moonName == moon)
                        {
                            if (float.TryParse(moonValue, out var result))
                            {
                                value = result;
                            }

                            break;
                        }
                    }
                }
            }

            return value;
        }

        public static void LoadMoonsConfig(string loadedMoons)
        {
            List<string> loadedMoonsList = new List<string>(loadedMoons.Split(','));

            // Вывод элементов списка для проверки
            foreach (var moon in loadedMoonsList)
            {
                Console.WriteLine($"found a {moon}");
            }
        }
    }
}
