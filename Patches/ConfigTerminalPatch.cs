using HarmonyLib;
using Oxygen.Configuration;
using Oxygen.General;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal static class ConfigTerminalPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        private static void BeforeAwake()
        {
            OxyBoostShop.Reset();
            ChargerUnlockable.Reset();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void AfterAwake() => ChargerUnlockable.Prepare(StartOfRound.Instance);

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void AfterStart() => OxygenBase.Instance.UpdateOxyBoostShop();
    }
}
