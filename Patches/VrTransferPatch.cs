using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.General;

namespace Oxygen.Patches
{
    // Patch LCVR's own hover pass, after it clears/replaces the vanilla tip.
    // Resolve the optional type at runtime so desktop players need no LCVR DLL.
    [HarmonyPatch]
    internal static class VrTransferHoverPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            if (Chainloader.PluginInfos.TryGetValue("io.daxcess.lcvr", out var plugin))
            {
                var type = plugin.Instance.GetType().Assembly.GetType("LCVR.Player.VRController");
                var method = AccessTools.Method(type, "LateUpdate");
                if (method != null) yield return method;
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Refresh(object __instance)
        {
            var local = GameNetworkManager.Instance?.localPlayerController;
            var transfers = OxygenBase.Instance?.Transfers;
            if (local == null || transfers == null || !VrCompatibility.Active) return;
            transfers.Hover(local);
            if (transfers.HoverPoint is { } point) VrCompatibility.PlacePrompt(__instance, point);
        }
    }
}
