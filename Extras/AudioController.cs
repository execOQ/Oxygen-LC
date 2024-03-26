using BepInEx.Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxygen.Extras
{
    internal class AudioController
    {
        public enum State {
            standing = 0,
            walking = 5,
            running = 4,
            exhausted = 3,
            scared = 2,
        }
        
        internal static AudioClip[] InhalesSFX => OxygenBase.Instance.inhalesSFX;
        internal static AudioClip[] HeavyInhalesSFX => OxygenBase.Instance.heavyInhalesSFX;

        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > AudioController");

        internal static AudioClip FindSFX(State stage, bool isEndOfFearOrExhausted = false)
        {
            mls.LogInfo($"trying to find SFX");
            AudioClip clip = stage switch
            {
                State.standing => null,
                State.walking => InhalesSFX[Random.Range(0, InhalesSFX.Length - 1)],
                State.running => InhalesSFX[Random.Range(0, InhalesSFX.Length - 1)],
                State.exhausted => isEndOfFearOrExhausted ? HeavyInhalesSFX[^1] : HeavyInhalesSFX[Random.Range(0, HeavyInhalesSFX.Length - 2)],
                State.scared => isEndOfFearOrExhausted ? HeavyInhalesSFX[^1] : HeavyInhalesSFX[Random.Range(0, HeavyInhalesSFX.Length - 2)],
                _ => null,
            };

            return clip;
        }

        /* internal static void PlayLeakSFX(AudioClip clip)
        {
            AudioSource source = OxygenHUD.oxygenLeak;
            if (source.isPlaying) return;

            mls.LogInfo($"playing SFX");
            source.PlayOneShot(clip, Random.Range(Mathf.Clamp01(Volume) - 0.18f, Mathf.Clamp01(Volume)));
        } */

        internal static void PlaySFX(AudioClip clip, float volume)
        {
            AudioSource source = OxygenHUD.oxygenDefault;
            if (source.isPlaying) return;

            mls.LogInfo($"playing SFX");
            source.PlayOneShot(clip, Random.Range(Mathf.Clamp01(volume) - 0.18f, Mathf.Clamp01(volume)));
        }
    }
}
