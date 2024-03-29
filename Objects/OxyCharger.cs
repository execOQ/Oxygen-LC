﻿using BepInEx.Logging;
using UnityEngine;

namespace Oxygen.Items
{
    internal class OxyCharger : MonoBehaviour
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxyCharger");

        public static float Volume => OxygenBase.OxygenConfig.oxyCharger_SFXVolume.Value;

        public static AudioClip[] OxyChargerSFX => OxygenBase.Instance.oxyChargerSFX;

        public static AudioSource audioSource;

        public static void FillOxygen()
        {
            OxygenInit.oxygenUI.fillAmount = 1f;

            PlaySFX(OxyChargerSFX[Random.Range(0, OxyChargerSFX.Length)]);

            mls.LogInfo("Oxygen was recovered");
        }

        public static void PlaySFX(AudioClip clip)
        {
            if (audioSource == null)
            {
                mls.LogError("audio source was not found");
            }

            if (audioSource.isPlaying) audioSource.Stop();

            audioSource.PlayOneShot(clip, Volume);
        }
    }
}
