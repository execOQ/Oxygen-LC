using GameNetcodeStuff;
using HarmonyLib;
using Oxygen.Extras;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oxygen.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch("ConnectClientToPlayerObject")]
        public static void AddAudioSource(PlayerControllerB __instance)
        {
            AudioController.Init_AudioSource(__instance.playersManager.thisClientPlayerId);
        }
    }
}
