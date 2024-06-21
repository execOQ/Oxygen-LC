using BepInEx.Logging;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxygen.Extras
{
    internal class AudioController : MonoBehaviour
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > AudioController");

        // the value of the state is the amount of seconds after which the audio will be played each time
        public enum State {
            standing = 0,
            walking = 5,
            running = 4,
            exhausted = 3,
            scared = 2,
        }
        
        private static AudioClip[] InhalesSFX => OxygenBase.Instance.inhalesSFX;
        private static AudioClip[] HeavyInhalesSFX => OxygenBase.Instance.heavyInhalesSFX;

        // Sounds
        private static AudioSource oxygenDefault;
        //private static AudioSource oxygenLeak;

        internal static void Init_AudioSource(int id)
        {
            mls.LogWarning($"Client id: {id}");

            string path = (id == 0) ? "PlayersContainer/Player/Audios/" : $"PlayersContainer/Player ({id})/Audios/";
            GameObject pc = GameObject.Find(path);

            GameObject _oxygenDefault = Instantiate(OxygenBase.Instance.oxyAudioExample, pc.transform);
            _oxygenDefault.name = "OxygenDefault";

            //GameObject _oxygenLeak = Instantiate(OxygenBase.Instance.oxyAudioExample, pc.transform);
            //_oxygenLeak.name = "OxygenLeak";

            oxygenDefault = _oxygenDefault.GetComponent<AudioSource>();
            //oxygenLeak = _oxygenLeak.GetComponent<AudioSource>();

            mls.LogWarning($"Oxygen audio sources are created!");
        }

        internal static void PlaySFX(State stage, float volume, bool isEndOfFearOrExhausted = false)
        {
            AudioSource source = oxygenDefault;
            if (source.isPlaying) return;

            AudioClip clip = FindSFX(stage, isEndOfFearOrExhausted);
            mls.LogDebug($"Playing SFX");
            source.PlayOneShot(clip, Random.Range(volume - 0.18f, volume));
        }

        /* internal static void PlayLeakSFX(AudioClip clip)
        {
            AudioSource source = OxygenHUD.oxygenLeak;
            if (source.isPlaying) return;

            mls.LogInfo($"playing SFX");
            source.PlayOneShot(clip, Random.Range(Mathf.Clamp01(Volume) - 0.18f, Mathf.Clamp01(Volume)));
        } */

        private static AudioClip FindSFX(State stage, bool isEndOfFearOrExhausted)
        {
            mls.LogDebug($"Trying to find SFX for {stage}");
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
    }
}
