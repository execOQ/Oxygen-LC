using GameNetcodeStuff;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxygen.Extras
{
    internal class AudioController
    {
        public enum Stage {
            standing,
            walking,
            running,
            exhausted,
            scared
        }

        internal Stage CurrentStage = Stage.standing;

        public static AudioController Instance { get; internal set; }

        private AudioClip[] inhaleSFX = OxygenBase.Instance.inhaleSFX;
        private AudioClip[] runningInhaleSFX = OxygenBase.Instance.inhaleSFX;
        private AudioClip[] exhaustedInhaleSFX = OxygenBase.Instance.inhaleSFX;
        private AudioClip[] scaredInhaleSFX = OxygenBase.Instance.inhaleSFX;

        public static float Volume => OxygenBase.OxygenConfig.SFXvolume.Value;

        internal static AudioClip FindSFX(Stage stage)
        {
            AudioClip clip = stage switch
            {
                Stage.standing => null,
                Stage.walking => Instance.inhaleSFX[Random.Range(0, Instance.inhaleSFX.Length - 1)],
                Stage.running => Instance.inhaleSFX[Random.Range(0, Instance.inhaleSFX.Length - 1)],
                Stage.exhausted => Instance.inhaleSFX[Random.Range(0, Instance.inhaleSFX.Length - 1)],
                Stage.scared => Instance.inhaleSFX[Random.Range(0, Instance.inhaleSFX.Length - 1)],
                _ => null,
            };

            return clip;
        }

        internal static void PlaySFX(PlayerControllerB pc, AudioClip clip)
        {
            AudioSource audio = pc.waterBubblesAudio;
            if (audio.isPlaying) return;

            audio.PlayOneShot(clip, Random.Range(Volume - 0.18f, Volume));
        }
    }
}
