using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.Configuration;
using UnityEngine;
using BepInEx.Bootstrap;
using Oxygen.Utils;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("io.github.CSync")]
    [BepInDependency(shyHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LCAPIGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class OxygenBase : BaseUnityPlugin
    {
        public static OxygenBase Instance { get; private set; }

        public const string modName = "Oxygen";
        public const string modGUID = "consequential.Oxygen";
        public const string modVersion = "1.2.2";

        private const string shyHUDGUID = "ShyHUD";
        private const string LCAPIGUID = "LC_API";

        public bool isShyHUDFound { get; private set; } = false;
        public bool isLCAPIFound { get; private set; } = false;

        private readonly Harmony harmony = new(modGUID);
        public ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        public AudioClip[] inhaleSFX;

        public static Config Config { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            mls.LogInfo($"{modName} is loading...");

            CheckForDependencies();

            AssetBundle bundle = Utilities.LoadAssetFromStream("Oxygen.Assets.oxygensounds");
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
            //harmony.PatchAll(typeof(WritePlayerNotesPatch));

            Config = new Config(((BaseUnityPlugin)this).Config);
            mls.LogInfo($"Config is loaded.");

            mls.LogInfo($"{modName} is loaded!");

            //DeathBroadcaster.Initialize();
        }

        private void CheckForDependencies()
        {
            mls.LogInfo((object)"Checking for soft dependencies...");
            if (Chainloader.PluginInfos.ContainsKey(LCAPIGUID))
            {
                Chainloader.PluginInfos.TryGetValue(LCAPIGUID, out var value);
                if (value == null)
                {
                    mls.LogError("Detected LC_API, but could not get plugin info!");
                }
                else
                {
                    mls.LogInfo("LCAPI is present! " + value.Metadata.GUID + ":" + value.Metadata.Version);
                    isLCAPIFound = true;
                }
            }
            if (Chainloader.PluginInfos.ContainsKey(shyHUDGUID))
            {
                Chainloader.PluginInfos.TryGetValue(shyHUDGUID, out var value);
                if (value == null)
                {
                    mls.LogError("Detected ShyHUD, but could not get plugin info!");
                }
                else
                {
                    mls.LogInfo("ShyHUD is present! " + value.Metadata.GUID + ":" + value.Metadata.Version);
                    isShyHUDFound = true;
                }
            }
        }
    }
}