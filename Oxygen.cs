using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.Configuration;
using UnityEngine;
using BepInEx.Bootstrap;
using LL = LethalLib.Modules;
using Oxygen.Extras;
using System.Reflection;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.sigurd.csync")]
    [BepInDependency("evaisa.lethallib")]
    [BepInDependency(shyHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(immersiveVisorGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(eladsHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LCVRGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class OxygenBase : BaseUnityPlugin
    {
        private readonly ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        public const string modName = "Oxygen";
        public const string modGUID = "consequential.Oxygen";
        public const string modVersion = "1.6.0";

        private readonly Harmony harmony = new(modGUID);
        public static OxygenConfig OxygenConfig { get; private set; }

        private const string immersiveVisorGUID = "ImmersiveVisor"; 
        private const string shyHUDGUID = "ShyHUD";
        private const string eladsHUDGUID = "me.eladnlg.customhud";
        private const string LCVRGUID = "io.daxcess.lcvr";

        #region Getters
        public bool IsShyHUDFound { get; private set; } = false;
        public bool IsImmersiveVisorFound { get; private set; } = false;
        public bool IsEladsHUDFound { get; private set; } = false;
        public bool IsLCVRFound { get; private set; } = false;
        #endregion

        #region Assets
        internal AudioClip[] inhalesSFX = [];
        internal AudioClip[] heavyInhalesSFX = [];

        internal GameObject oxyAudioExample;

        internal GameObject oxyCharger;
        internal AudioClip[] oxyChargerSFX;

        internal Item oxyBoost;
        #endregion

        public static OxygenBase Instance { get; private set; }

        internal static void UpdateCustomItemPrice(Item item, int price) => LL.Items.UpdateShopItemPrice(item, price);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            mls.LogInfo($"{modName} is loading...");

            NetworkPatcher();

            IsShyHUDFound = CheckForDependency(shyHUDGUID);
            IsImmersiveVisorFound = CheckForDependency(immersiveVisorGUID);
            IsEladsHUDFound = CheckForDependency(eladsHUDGUID);
            IsLCVRFound = CheckForDependency(LCVRGUID);

            AssetBundle _oxyAudio = Utilities.LoadAssetFromStream("Oxygen.Assets.oxygenaudio");
            if (_oxyAudio == null) return;
            inhalesSFX = Utilities.LoadAudioClips(_oxyAudio,
                "Assets/OxygenAudio/Inhale_1.wav",
                "Assets/OxygenAudio/Inhale_2.wav",
                "Assets/OxygenAudio/Inhale_3.wav");

            heavyInhalesSFX = Utilities.LoadAudioClips(_oxyAudio,
                "Assets/OxygenAudio/heavy_Inhale_1.wav",
                "Assets/OxygenAudio/heavy_Inhale_2.wav",
                "Assets/OxygenAudio/heavy_Inhale_end.wav");

            oxyChargerSFX = Utilities.LoadAudioClips(_oxyAudio,
                "Assets/OxyCharger/Audio/OxyChargeSFX.wav",
                "Assets/OxyCharger/Audio/OxyChargeSFX2.wav",
                "Assets/OxyCharger/Audio/OxyChargeSFX3.wav");

            AssetBundle _oxyPrefabs = Utilities.LoadAssetFromStream("Oxygen.Assets.oxygenprefabs");
            if (_oxyPrefabs == null) return;
            oxyAudioExample = _oxyPrefabs.LoadAsset<GameObject>("Assets/OxygenAudio/OxygenAudio.prefab");
            oxyCharger = _oxyPrefabs.LoadAsset<GameObject>("Assets/OxyCharger/OxyCharger.prefab");
            oxyBoost = _oxyPrefabs.LoadAsset<Item>("Assets/OxyBoost/OxyBoostItem.asset");

            mls.LogInfo($"Assets are loaded!");

            OxygenConfig = new(Config);
            mls.LogInfo($"Config is loaded.");

            harmony.PatchAll(typeof(HUDPatch));
            harmony.PatchAll(typeof(OxygenConfig));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            harmony.PatchAll(typeof(GameNetworkManagerParch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            
            RegisterItems();

            mls.LogInfo($"{modName} is loaded. Don't forget to refill oxygen canisters!");
        }

        private void NetworkPatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private bool CheckForDependency(string guid)
        {
            mls.LogDebug($"Checking for {guid}...");
            if (Chainloader.PluginInfos.ContainsKey(guid))
            {
                Chainloader.PluginInfos.TryGetValue(guid, out PluginInfo value);
                if (value != null)
                {
                    mls.LogDebug($"{guid}:{value.Metadata.Version} is present!");
                    return true;
                }
                else mls.LogError($"Detected {guid}, but could not get plugin info...");
            }

            return false;
        }

        private void RegisterItems()
        {
            LL.NetworkPrefabs.RegisterNetworkPrefab(oxyBoost.spawnPrefab);
            LL.Utilities.FixMixerGroups(oxyBoost.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "Limited air supply, useful in situations when you cannot quickly refill oxygen canisters!";

            LL.Items.RegisterShopItem(oxyBoost, null, null, node, OxygenConfig.oxyBoost_price.Value);

            mls.LogInfo("Custom items are loaded!");
        }
    }
}