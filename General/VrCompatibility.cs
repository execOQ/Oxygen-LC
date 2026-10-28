using System.Reflection;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using UnityEngine;

namespace Oxygen.General
{
    // LCVR moved VRSession from Player to Managers. Keep the optional
    // dependency behind reflection so either namespace, or no LCVR, can load.
    internal static class VrCompatibility
    {
        private static PropertyInfo inVr, session;
        private static bool resolved;

        private static void Resolve()
        {
            if (resolved || !Chainloader.PluginInfos.TryGetValue("io.daxcess.lcvr", out var plugin)) return;
            var assembly = plugin.Instance.GetType().Assembly;
            var type = assembly.GetType("LCVR.Managers.VRSession") ?? assembly.GetType("LCVR.Player.VRSession");
            inVr = type?.GetProperty("InVR", BindingFlags.Public | BindingFlags.Static);
            session = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            resolved = true;
        }

        internal static bool Active { get { Resolve(); return inVr?.GetValue(null) is true; } }
        internal static bool Ready { get { Resolve(); return session?.GetValue(null) != null; } }
        internal static Ray Aim(PlayerControllerB player)
        {
            Transform origin = player.gameplayCamera.transform;
            if (player.IsOwner && Active && Ready)
            {
                var current = session.GetValue(null);
                var local = current.GetType().GetProperty("LocalPlayer")?.GetValue(current);
                var controller = local?.GetType().GetProperty("PrimaryController")?.GetValue(local);
                var hand = controller?.GetType().GetProperty("InteractOrigin")?.GetValue(controller) as Transform;
                if (hand != null) origin = hand;
            }
            return new Ray(origin.position, origin.forward);
        }
        internal static void PlacePrompt(object controller, Vector3 point)
        {
            controller.GetType().GetProperty("IsHovering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(controller, true);
            var current = session?.GetValue(null);
            var hud = current?.GetType().GetProperty("HUD")?.GetValue(current);
            hud?.GetType().GetMethod("UpdateInteractCanvasPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(hud, new object[] { point });
        }
        internal static bool DisableArmHud
        {
            get
            {
                if (!Chainloader.PluginInfos.TryGetValue("io.daxcess.lcvr", out var plugin)) return false;
                var config = plugin.Instance.GetType().GetProperty("Config", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var entry = config?.GetType().GetProperty("DisableArmHUD")?.GetValue(config);
                return entry?.GetType().GetProperty("Value")?.GetValue(entry) is true;
            }
        }
    }
}
