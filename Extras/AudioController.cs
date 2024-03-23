using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxygen.Extras
{
    internal class AudioController
    {
        public enum Stage {
            standing = 0,
            walking = 5,
            running = 4,
            exhausted = 3,
            scared = 2
        }

        internal static AudioClip[] InhaleSFX => OxygenBase.Instance.inhaleSFX;
        internal static AudioClip[] RunningInhaleSFX => OxygenBase.Instance.inhaleSFX;
        internal static AudioClip[] ExhaustedInhaleSFX => OxygenBase.Instance.inhaleSFX;
        internal static AudioClip[] ScaredInhaleSFX => OxygenBase.Instance.inhaleSFX;

        public static float Volume => OxygenBase.OxygenConfig.SFXvolume.Value;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > AudioController");

        internal static AudioClip FindSFX(Stage stage)
        {
            mls.LogWarning($"trying to find SFX");
            AudioClip clip = stage switch
            {
                Stage.standing => null,
                Stage.walking => InhaleSFX[Random.Range(0, InhaleSFX.Length - 1)],
                Stage.running => RunningInhaleSFX[Random.Range(0, RunningInhaleSFX.Length - 1)],
                Stage.exhausted => ExhaustedInhaleSFX[Random.Range(0, ExhaustedInhaleSFX.Length - 1)],
                Stage.scared => ScaredInhaleSFX[Random.Range(0, ScaredInhaleSFX.Length - 1)],
                _ => null,
            };

            return clip;
        }

        internal static void PlaySFX(PlayerControllerB pc, AudioClip clip)
        {
            AudioSource audio = pc.waterBubblesAudio;
            if (audio.isPlaying) return;

            mls.LogWarning($"playing SFX");
            audio.PlayOneShot(clip, Random.Range(Mathf.Clamp01(Volume) - 0.18f, Mathf.Clamp01(Volume)));
        }

    }
}
