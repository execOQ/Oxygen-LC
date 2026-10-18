using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal static class ConfigNetworkManagerPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(NetworkManager), nameof(NetworkManager.StartHost));
            yield return AccessTools.Method(typeof(NetworkManager), nameof(NetworkManager.StartServer));
            yield return AccessTools.Method(typeof(NetworkManager), nameof(NetworkManager.StartClient));
        }

        [HarmonyPostfix]
        private static void Started(NetworkManager __instance, bool __result)
        {
            if (__result) OxygenBase.OxygenConfig.Sync.StartSession(__instance);
        }
    }
}
