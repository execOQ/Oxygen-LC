using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.ConfigBase;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class OxygenBase : BaseUnityPlugin
    {
        private static OxygenBase Instance;

        public const string modName = "Oxygen";
        private const string modGUID = "consequential.Oxygen";
        private const string modVersion = "1.0.2";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        public static new Config Config { get; private set; }

        internal static AudioClip[] SFX;
        public AssetBundle assets;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls.LogInfo($"{modName} is loading...");

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Oxygen.Assets.oxygensounds");
            if (stream == null)
            {
                mls.LogError("Failed to get stream of resources...");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            if (bundle == null)
            {
                mls.LogError("Failed to load audio assets...");
                return;
            }

            SFX = bundle.LoadAllAssets<AudioClip>();

            mls.LogInfo($"Sounds are loaded.");

            harmony.PatchAll(typeof(HUDPatch));

            Config = new Config(((BaseUnityPlugin)this).Config);
            mls.LogInfo($"Config is loaded.");

            mls.LogInfo($"{modName} is loaded!");
        }


    }
}