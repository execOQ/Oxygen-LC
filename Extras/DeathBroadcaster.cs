using BepInEx.Logging;
using LC_API.ServerAPI;
using Oxygen.Patches;
using System;

namespace Oxygen.Extras
{
    internal class DeathBroadcaster
    {
        public static ManualLogSource mls = OxygenBase.Instance.mls;

        private const string SIGNATURE_DEATH = $"{OxygenBase.modGUID}.death";

        public static void Initialize()
        {
            mls.LogInfo("Initializing DeathBroadcaster...");
            if (OxygenBase.Instance.isLCAPIFound)
            {
                mls.LogInfo("LC_API is present! Registering signature...");
                Networking.GetString = (Action<string, string>)Delegate.Combine(Networking.GetString, new Action<string, string>(OnBroadcastString));
            }
            else
            {
                mls.LogError("LC_API is not present! Why did you try to register the DeathBroadcaster?");
            }
        }

        public static void BroadcastCauseOfDeath(int playerId, string causeOfDeath)
        {
            OxygenBase.Instance.mls.LogError("BroadcastCauseOfDeath");

            AttemptBroadcast(BuildDataCauseOfDeath(playerId, causeOfDeath), SIGNATURE_DEATH);
        }

        private static string BuildDataCauseOfDeath(int playerId, string causeOfDeath)
        {
            string id = playerId.ToString();
            return id + "|" + causeOfDeath;
        }

        public static void AttemptBroadcast(string data, string signature)
        {
            if (OxygenBase.Instance.isLCAPIFound)
            {
                mls.LogInfo("LC_API is present! Broadcasting...");
                //LC_API.Networking.Network.Broadcast(data, signature);
                Networking.Broadcast(data, signature);
            }
            else
            {
                mls.LogInfo("LC_API is not present! Skipping broadcast...");
            }
        }

        private static void OnBroadcastString(string data, string signature)
        {
            OxygenBase.Instance.mls.LogError("OnBroadcastString");
            if (signature == SIGNATURE_DEATH)
            {
                mls.LogInfo("Broadcast has been received from LC_API!");
                string[] array = data.Split('|');
                int playerIndex = int.Parse(array[0]);
                string causeOfDeath = array[1];

                mls.LogInfo("Player " + playerIndex + " died of " + causeOfDeath);

                WritePlayerNotesPatch.AddCauseOfDeath(playerIndex, causeOfDeath);
            }
        }
    }
}
