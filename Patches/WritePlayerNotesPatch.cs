using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class WritePlayerNotesPatch
    {
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPostfix]
        public static void WritePlayerNotes_patch(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
            {
                if (__instance.gameStats.allPlayerStats[i].isActivePlayer)
                {
                    if (OxygenHUD.diedBecauseOfOxygen)
                    {
                        OxygenBase.Instance.mls.LogError("diedBecauseOfOxygen");

                        //DeathBroadcaster.BroadcastCauseOfDeath(i, "Forgets about oxygen");
                        __instance.gameStats.allPlayerStats[i].playerNotes.Add("Forgets about oxygen.");
                    }
                }
            }
        }

        public static void AddCauseOfDeath(int playerindex, string causeOfDeath)
        {
            StartOfRound __instance = StartOfRound.Instance;

            OxygenBase.Instance.mls.LogError("AddCauseOfDeath");
            __instance.gameStats.allPlayerStats[playerindex].playerNotes.Add(causeOfDeath);
        }
    }
}
