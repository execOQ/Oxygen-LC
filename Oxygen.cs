using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Patches;
using Oxygen.Configuration;
using UnityEngine;
using BepInEx.Bootstrap;
using LL = LethalLib.Modules;
using Oxygen.Extras;
using System.Collections.Generic;
using System.Linq;

namespace Oxygen
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("com.sigurd.csync")]
    [BepInDependency("evaisa.lethallib")]
    [BepInDependency(shyHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LCAPIGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(EladsHUDGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class OxygenBase : BaseUnityPlugin
    {
        public static OxygenBase Instance { get; private set; }

        public const string modName = "Oxygen";
        public const string modGUID = "consequential.Oxygen";
        public const string modVersion = "1.4.0";

        private const string shyHUDGUID = "ShyHUD";
        private const string LCAPIGUID = "LC_API";
        private const string EladsHUDGUID = "me.eladnlg.customhud";

        public bool IsShyHUDFound { get; private set; } = false;
        public bool IsLCAPIFound { get; private set; } = false;
        public bool IsEladsHUDFound { get; private set; } = false;

        private readonly Harmony harmony = new(modGUID);
        public ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(modName);

        internal AudioClip[] inhalesSFX = [];
        internal AudioClip[] heavyInhalesSFX = [];

        internal GameObject oxyAudioExample;

        internal GameObject oxyCharger;
        internal AudioClip[] oxyChargerSFX;

        internal Item oxyBoost;

        public static OxygenConfig OxygenConfig { get; private set; }

        internal static void UpdateCustomItemPrice(Item item, int price) => LL.Items.UpdateShopItemPrice(item, price);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            mls.LogInfo($"{modName} is loading...");

            CheckForDependencies();

            AssetBundle oxyAudioBundle = Utilities.LoadAssetFromStream("Oxygen.Assets.oxygenaudio");
            if (oxyAudioBundle == null) return;
            oxyAudioExample = oxyAudioBundle.LoadAsset<GameObject>("Assets/OxygenAudio/OxygenAudio.prefab");
            inhalesSFX = oxyAudioBundle.LoadAllAssets<AudioClip>();

            AssetBundle oxyHeavyInhaleBundle = Utilities.LoadAssetFromStream("Oxygen.Assets.heavyinhalesfx");
            if (oxyHeavyInhaleBundle == null) return;
            heavyInhalesSFX = oxyHeavyInhaleBundle.LoadAllAssets<AudioClip>();

            AssetBundle oxyChargerBundle = Utilities.LoadAssetFromStream("Oxygen.Assets.oxycharger");
            if (oxyChargerBundle == null) return;
            oxyCharger = oxyChargerBundle.LoadAsset<GameObject>("Assets/OxyCharger/OxyCharger.prefab");
            oxyChargerSFX = oxyChargerBundle.LoadAllAssets<AudioClip>();

            mls.LogInfo($"Assets are loaded (～￣▽￣)～");

            OxygenConfig = new(Config);
            mls.LogInfo($"Config is loaded.");

            harmony.PatchAll(typeof(HUDPatch));
            harmony.PatchAll(typeof(OxygenHUD));
            harmony.PatchAll(typeof(KillPlayerPatch));
            harmony.PatchAll(typeof(OxygenConfig));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(RoundManagerPatch));
            //harmony.PatchAll(typeof(WritePlayerNotesPatch));


            if (!OxygenConfig.MakeItVanilla.Value)
            {
                AssetBundle oxy99 = Utilities.LoadAssetFromStream("Oxygen.Assets.oxy99");
                if (oxy99 == null) return;

                oxyBoost = oxy99.LoadAsset<Item>("Assets/Oxy99/Oxy99Item.asset");
                oxyBoost.itemName = "OxyBoost";

                LL.NetworkPrefabs.RegisterNetworkPrefab(oxyBoost.spawnPrefab);
                LL.Utilities.FixMixerGroups(oxyBoost.spawnPrefab);

                TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
                node.clearPreviousText = true;
                node.displayText = "Limited air supply, useful in situations when you cannot quickly refill oxygen canisters!";

                LL.Items.RegisterShopItem(oxyBoost, null, null, node, OxygenConfig.Instance.oxyBoost_price.Value);

                mls.LogInfo("Custom items are loaded!");
            }

            mls.LogInfo($"{modName} loaded! Yay! Don't forget to refill oxygen canisters!");

            //DeathBroadcaster.Initialize();
        }

        private void CheckForDependencies()
        {
            mls.LogInfo("Checking for soft dependencies...");
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
                    IsLCAPIFound = true;
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
                    IsShyHUDFound = true;
                }
            }
            if (Chainloader.PluginInfos.ContainsKey(EladsHUDGUID))
            {
                Chainloader.PluginInfos.TryGetValue(EladsHUDGUID, out var value);
                if (value == null)
                {
                    mls.LogError("Detected EladsHUD, but could not get plugin info!");
                }
                else
                {
                    mls.LogInfo("EladsHUD is present! " + value.Metadata.GUID + ":" + value.Metadata.Version);
                    IsEladsHUDFound = true;
                }
            }
        }
    }
}