using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using Oxygen.Extras;
using Oxygen.General;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private readonly static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > RoundManagerPatch");

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Awake_Postfix()
        {
            MoonsDicts.UpdateMoonsValues();
        }

        [HarmonyPostfix]
        [HarmonyPriority(0)]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(RoundManager __instance)
        {
            string msg = "\nUse these moons names in the config:\n\n";
            foreach (SelectableLevel level in __instance.playersManager.levels)
            {
                msg += Utilities.GetNumberlessPlanetName(level.PlanetName) + "\n";
            }

            mls.LogInfo(msg);
        }

        [HarmonyPostfix]
        [HarmonyPatch("FinishGeneratingLevel")]
        private static void FinishGeneratingLevel_Postfix(RoundManager __instance)
        {
            WeatherHandler.UpdateWeatherType(__instance.currentLevel);
        }

    }
}
