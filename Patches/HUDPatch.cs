using BepInEx.Logging;
using HarmonyLib;
using Oxygen.General;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDPatch
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > HUDPatch");

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void BuildHUD()
        {
            mls.LogInfo("Initializing HUD");
            OxygenInit.Init();
            DieEarly.Init();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePatch()
        {
            OxygenLogic.RunLogic();
        }

        /* private static IEnumerator AwaitPlayerController()
        {
            yield return new WaitUntil(() => (Object)(object)GameNetworkManager.Instance.localPlayerController != null);
        } */
    }
}
