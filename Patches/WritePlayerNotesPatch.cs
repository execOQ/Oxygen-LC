using BepInEx.Logging;
using HarmonyLib;

namespace Oxygen.Patches
{
    [HarmonyPatch]
    internal class WritePlayerNotesPatch
    {
        public static ManualLogSource mls = Logger.CreateLogSource(OxygenBase.modName + " > WritePlayerNotesPatch");

        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPostfix]
        public static void WritePlayerNotes_patch(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
            {
                if (__instance.gameStats.allPlayerStats[i].isActivePlayer)
                {
                    if (OxygenInit.diedBecauseOfOxygen)
                    {
                        mls.LogError("diedBecauseOfOxygen");

                        //DeathBroadcaster.BroadcastCauseOfDeath(i, "Forgets about oxygen");
                        __instance.gameStats.allPlayerStats[i].playerNotes.Add("Forgets about oxygen.");
                    }
                }
            }
        }

        public static void AddCauseOfDeath(int playerindex, string causeOfDeath)
        {
            StartOfRound __instance = StartOfRound.Instance;

            mls.LogError("AddCauseOfDeath");
            __instance.gameStats.allPlayerStats[playerindex].playerNotes.Add(causeOfDeath);
        }
    }
}
