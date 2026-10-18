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

        private readonly NetworkVariable<float> _remainedOxygenAmount = new();
        private float? _queuedServerRemainedOxygenAmount;

        public float RemainedOxygenAmount
        {
            get
            {
                if (!IsSpawned)
                {
                    return _queuedServerRemainedOxygenAmount ?? 0f;
                }

                return _remainedOxygenAmount.Value;
            }
            internal set
            {
                // Only host initialization/allocation may change the shared reserve.
                if (!IsServer || float.IsNaN(value) || float.IsInfinity(value)) return;
                float amount = Mathf.Max(0f, value);
                if (IsSpawned) _remainedOxygenAmount.Value = amount;
                else _queuedServerRemainedOxygenAmount = amount;
            }
        }
        public static OxyCharger Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                mls.LogError("Audio source was not found");
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OxygenInit.oxyCharger = gameObject;

            if (IsServer)
            {
                float startValue = _queuedServerRemainedOxygenAmount ?? _remainedOxygenAmount.Value;
                _remainedOxygenAmount.Value = Mathf.Max(0f, startValue);
                _queuedServerRemainedOxygenAmount = null;
                mls.LogDebug($"OnNetworkSpawn initialized RemainedOxygenAmount={_remainedOxygenAmount.Value}");
            }

            VanillaCanisters.SetVisible(false);
            WeatherHandler.RefreshLimit();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
            {
                Instance = null;
                // IsSpawned may already be false here; read the variable directly, not the pre-spawn getter.
                WeatherHandler.ChargerDespawned(_remainedOxygenAmount.Value);
            }
            if (OxygenInit.oxyCharger == gameObject) OxygenInit.oxyCharger = null;
            VanillaCanisters.SetVisible(true);
            base.OnNetworkDespawn();
        }

        public void FillOxygen(bool playSound = true)
        {
            OxygenBase.Instance.Refills.Request(1f - OxygenInit.Percent, false, playSound);
        }

        public void AutoRefillOxygen()
        {
            OxygenBase.Instance.Refills.Request(1f - OxygenInit.Percent, true, false);
        }

        internal float AllocateOxygen(float requested, bool automatic, int ticks)
        {
            if (!IsServer || !IsSpawned || !OxygenBase.OxygenConfig.oxyCharger_Enabled.Value ||
                float.IsNaN(requested) || float.IsInfinity(requested) || requested <= 0f) return 0f;
            float amount = Mathf.Min(1f, requested);
            if (automatic)
            {
                var mode = OxygenBase.OxygenConfig.autoFillingOnShip.Value;
                if (mode == Configuration.OxygenConfig.AutoFillingOnShip.Off ||
                    (mode == Configuration.OxygenConfig.AutoFillingOnShip.WhenDoorsClosed &&
                    !StartOfRound.Instance.hangarDoorsClosed)) return 0f;
                amount = Mathf.Min(amount, Mathf.Max(0f, IncreasingOxygen) * ticks);
            }
            if (WeatherHandler.IsOxygenOnShipLimited)
            {
                amount = Mathf.Min(amount, _remainedOxygenAmount.Value);
                _remainedOxygenAmount.Value -= amount;
            }
            return amount;
        }

        internal void PlayGrantedSFX()
        {
            if (IsServer && IsSpawned) PlaySFXClientRpc();
        }
        [ClientRpc]
        private void PlaySFXClientRpc()
        {
            if (audioSource == null)
            {
                mls.LogError("Audio source was not found");
                return;
            }

            if (OxyChargerSFX == null || OxyChargerSFX.Length == 0)
            {
                mls.LogError("OxyCharger SFX references are missing.");
                return;
            }

            if (audioSource.isPlaying) audioSource.Stop();

            AudioClip clip = OxyChargerSFX[Random.Range(0, OxyChargerSFX.Length)];
            if (clip == null)
            {
                mls.LogError("Selected OxyCharger clip is null.");
                return;
            }

            audioSource.PlayOneShot(clip, Volume);
            mls.LogDebug("Playing sound");
        }

    }
}
