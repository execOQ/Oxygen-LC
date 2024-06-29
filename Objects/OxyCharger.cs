using BepInEx.Logging;
using Oxygen.General;
using Unity.Netcode;
using UnityEngine;

namespace Oxygen.Items
{
    internal class OxyCharger : NetworkBehaviour
    {
        private readonly ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxyCharger");

        private float IncreasingOxygen => OxygenBase.OxygenConfig.autoFillingOnShip_increasingOxygen.Value;
        private float Volume => OxygenBase.OxygenConfig.oxyCharger_SFXVolume.Value;
        private AudioClip[] OxyChargerSFX => OxygenBase.Instance.oxyChargerSFX;

        private AudioSource audioSource;

        private NetworkVariable<float> _remainedOxygenAmount = new();

        public float RemainedOxygenAmount
        {
            get
            {
                if (_remainedOxygenAmount == null)
                {
                    return 0f;
                }
                return _remainedOxygenAmount.Value;
            }
            internal set
            {
                if (_remainedOxygenAmount == null)
                {
                    return;
                }

                mls.LogDebug("RemainedOxygenAmount before: " + RemainedOxygenAmount);

                float newValue = value < 0 ? 0 : value;

                if (_remainedOxygenAmount.Value != newValue)
                {
                    if (IsHost || IsServer)
                    {
                        _remainedOxygenAmount.Value = newValue;
                    }
                    else
                    {
                        UpdateRemainedOxygenAmountServerRpc(newValue);
                    }
                }

                mls.LogDebug("RemainedOxygenAmount after: " + RemainedOxygenAmount);

                if (IsClient && !(IsHost || IsServer))
                {
                    mls.LogDebug("RemainedOxygenAmount (after) value will be displayed without updating on the client-side, but actually it should be updated");
                }
            }
        }

        public static OxyCharger Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            audioSource = GetComponent<AudioSource>();

            mls.LogInfo("Oxygen cylinders were replaced");
        }

        public void FillOxygen(bool playSound = true) // "playSound" variable is for the OxyCharger object
        {
            if (OxygenInit.Percent < 1)
            {
                if (WeatherHandler.IsOxygenOnShipLimited)
                {
                    mls.LogDebug("Oxygen is limited.");
                    if (RemainedOxygenAmount <= 0f) return;

                    float amountToFill = 1 - OxygenInit.Percent;

                    if (RemainedOxygenAmount >= amountToFill)
                    {
                        OxygenInit.Percent = 1f;
                        RemainedOxygenAmount -= amountToFill;
                    } else
                    {
                        OxygenInit.Percent += RemainedOxygenAmount;
                        RemainedOxygenAmount = 0f;
                    }
                } else 
                {
                    OxygenInit.Percent = 1f;
                }
            }

            //RemainedOxygenAmount += 1f;
            
            if (playSound) PlaySFX();
            
            mls.LogInfo("Oxygen was recovered");
        }

        public void AutoRefillOxygen()
        {
            if (OxygenInit.Percent < 1)
            {
                if (WeatherHandler.IsOxygenOnShipLimited)
                {
                    mls.LogDebug("Oxygen is limited.");
                    if (RemainedOxygenAmount <= 0f) return;

                    if (RemainedOxygenAmount >= IncreasingOxygen)
                    {
                        OxygenInit.Percent += IncreasingOxygen;
                        RemainedOxygenAmount -= IncreasingOxygen;
                    }
                    else
                    {
                        OxygenInit.Percent += RemainedOxygenAmount;
                        RemainedOxygenAmount = 0f;
                    }
                }
                else
                {
                    OxygenInit.Percent += IncreasingOxygen;
                }
            }
        }

        private void PlaySFX()
        {
            if (IsHost || IsServer)
            {
                PlaySFXClientRpc();
            }
            else
            {
                PlaySFXServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaySFXServerRpc()
        {
            PlaySFXClientRpc();
        }

        [ClientRpc]
        private void PlaySFXClientRpc()
        {
            if (audioSource == null)
            {
                mls.LogError("Audio source was not found");
            }

            if (audioSource.isPlaying) audioSource.Stop();

            AudioClip clip = OxyChargerSFX[Random.Range(0, OxyChargerSFX.Length)];

            audioSource.PlayOneShot(clip, Volume);
            mls.LogDebug("Playing sound");
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateRemainedOxygenAmountServerRpc(float newValue)
        {
            RemainedOxygenAmount = newValue;
        }
    }
}
