using GameNetcodeStuff;
using Oxygen.General;
using UnityEngine;

namespace Oxygen.Extras
{
    internal static class LowOxygenAlarm
    {
        private static AudioSource source;
        private static AudioClip beep;
        private static int previousStage;
        private static float nextBeepTime;

        // Called for the local player independently of HUD visibility and notification settings.
        internal static void Update(PlayerControllerB player)
        {
            if (player.isPlayerDead || !OxygenInit.IsOxygenHUDInitialized ||
                !OxygenBase.OxygenConfig.enableLowOxygenAlarm.Value)
            {
                Reset();
                return;
            }

            float oxygen = OxygenInit.Percent;
            int stage = oxygen < OxygenLogic.critical_OxygenAmount ? 2 :
                oxygen < OxygenLogic.low_OxygenAmount ? 1 : 0;
            if (stage == 0)
            {
                Reset();
                return;
            }

            if (source == null)
            {
                GameObject alarm = new GameObject("LowOxygenAlarm");
                alarm.transform.SetParent(player.transform, false);
                source = alarm.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                previousStage = 0;
            }

            source.volume = OxygenBase.OxygenConfig.lowOxygenAlarm_volume.Value;
            if (stage != previousStage || Time.time >= nextBeepTime)
            {
                if (beep == null) beep = CreateBeep();
                source.pitch = stage == 2 ? 1.2f : 1f;
                source.PlayOneShot(beep);
                nextBeepTime = Time.time + (stage == 2 ? 2f : 5f);
            }
            previousStage = stage;
        }

        internal static void Reset()
        {
            if (source != null) source.Stop();
            previousStage = 0;
            nextBeepTime = 0f;
        }

        private static AudioClip CreateBeep()
        {
            // A short double beep with fades to avoid clicks; no asset bundle rebuild required.
            const int sampleRate = 44100;
            const float duration = 0.45f;
            float[] samples = new float[(int)(sampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / sampleRate;
                float pulseTime = time < 0.25f ? time : time - 0.25f;
                if (pulseTime >= 0.15f) continue;
                float envelope = Mathf.Clamp01(pulseTime / 0.01f) *
                    Mathf.Clamp01((0.15f - pulseTime) / 0.02f);
                samples[i] = 0.4f * envelope * Mathf.Sin(2f * Mathf.PI * 880f * time);
            }
            AudioClip clip = AudioClip.Create("OxygenLowAlarm", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
