using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.Configuration;
using UnityEngine;
using BepInEx.Bootstrap;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("io.github.CSync")]
    [BepInDependency(backroomsGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(shyHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class OxygenBase : BaseUnityPlugin
    {
        public static OxygenBase Instance { get; private set; }

        public const string modName = "Oxygen";
        public const string modGUID = "consequential.Oxygen";
        public const string modVersion = "1.2.0";

        private const string backroomsGUID = "Neekhaulas.Backrooms";
        private const string shyHUDGUID = "ShyHUD";

        public bool isBackroomsFound { get; private set; } = false;
        public bool isShyHUDFound { get; private set; } = false;

        private readonly Harmony harmony = new(modGUID);
        public ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        public AudioClip[] inhaleSFX;

        internal static Config Config;

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
                    mls.LogWarning($"Found {backroomsGUID}.");
                    isBackroomsFound = true;
                }

                if (metadata.GUID.Equals(shyHUDGUID))
                {
                    mls.LogWarning($"Found {shyHUDGUID}.");
                    isShyHUDFound = true;
                }
            }

            AssetBundle bundle = Utils.LoadAssetFromStream("Oxygen.Assets.oxygensounds");
            if (bundle == null)
            {
                return;
            }

            inhaleSFX = bundle.LoadAllAssets<AudioClip>();
            mls.LogInfo($"Sounds are loaded.");

            harmony.PatchAll(typeof(HUDPatch));
            harmony.PatchAll(typeof(OxygenHUD));
            harmony.PatchAll(typeof(KillPlayerPatch));
            harmony.PatchAll(typeof(Config));

            Config = new Config(((BaseUnityPlugin)this).Config);
            mls.LogInfo($"Config is loaded.");

            mls.LogInfo($"{modName} is loaded!");
        }
    }
}