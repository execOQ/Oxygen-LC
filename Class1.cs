using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.ConfigBase;
using UnityEngine;
using BepInEx.Bootstrap;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(backroomsGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class OxygenBase : BaseUnityPlugin
    {
        private static OxygenBase Instance;

        public const string modName = "Oxygen";
        private const string modGUID = "consequential.Oxygen";
        private const string modVersion = "1.0.2";

        private const string backroomsGUID = "Neekhaulas.Backrooms";
        internal static bool isBackroomsFound = false;

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        public static new Config Config { get; private set; }

        internal static AudioClip[] inhaleSFX;
        internal static AudioClip[] heavyInhaleSFX;
        public AssetBundle assets;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            

            mls.LogInfo($"{modName} is loading...");

            foreach (var plugin in Chainloader.PluginInfos)
            {
                var metadata = plugin.Value.Metadata;
                if (metadata.GUID.Equals(backroomsGUID))
                {
                    // found it
                    mls.LogWarning($"Found {backroomsGUID}.");
                    isBackroomsFound = true;
                    break;
                }
            }

            AssetBundle bundle = Util.LoadAssetFromStream("Oxygen.Assets.oxygensounds");
            if (bundle == null)
            {
                return;
            }
            inhaleSFX = bundle.LoadAllAssets<AudioClip>();

            AssetBundle bundle2 = Util.LoadAssetFromStream("Oxygen.Assets.heavyinhale");
            if (bundle2 == null)
            {
                return;
            }
            heavyInhaleSFX = bundle2.LoadAllAssets<AudioClip>();

            mls.LogInfo($"Sounds are loaded.");

            harmony.PatchAll(typeof(HUDPatch));

            Config = new Config(((BaseUnityPlugin)this).Config);
            mls.LogInfo($"Config is loaded.");

            mls.LogInfo($"{modName} is loaded!");
        }
    }
}