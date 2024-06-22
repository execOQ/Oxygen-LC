using BepInEx.Logging;
using HarmonyLib;
using Oxygen.Configuration;
using Oxygen.Extras;

namespace Oxygen.Patches
{
    internal class RoundManagerPatch
    {
        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > RoundManagerPatch");

        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        private static void RoundManagerPatch_Postfix()
        {
            MoonsDicts.UpdateMoonsValues();
        }

        [HarmonyPriority(0)]
        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        private static void RoundManagerPatch_Postfix2(RoundManager __instance)
        {
            string msg = "\nUse these moons names in the config:\n\n";
            foreach (SelectableLevel level in __instance.playersManager.levels)
            {
                msg += Utilities.NumberLessPlanetName(level.PlanetName) + "\n";
            }

            mls.LogInfo(msg);
        }

    }
}
