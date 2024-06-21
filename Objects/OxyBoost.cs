using BepInEx.Logging;
using System.Collections;
using UnityEngine;

namespace Oxygen.Items
{
    class OxyBoost : TetraChemicalItem
    {
        private readonly static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource(OxygenBase.modName + " > OxyBoost");

        public new IEnumerator UseTZPAnimation()
        {
            thisAudioSource.PlayOneShot(holdCanSFX);
            WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
            yield return new WaitForSeconds(0.75f);
            emittingGas = true;

            if (IsOwner)
            {
                localHelmetSFX.Play();
                localHelmetSFX.PlayOneShot(twistCanSFX);
            }
            else
            {
                thisAudioSource.clip = releaseGasSFX;
                thisAudioSource.Play();
                thisAudioSource.PlayOneShot(twistCanSFX);
            }
        }

        public override void Update()
        {
            if (previousPlayerHeldBy != null)
            {
                float drunknessInertiaStorage = previousPlayerHeldBy.drunknessInertia;

                base.Update();

                previousPlayerHeldBy.drunknessInertia = drunknessInertiaStorage;
                previousPlayerHeldBy.increasingDrunknessThisFrame = false;
            }
            else
            {
                base.Update();
            }
            if (emittingGas)
            {
                if (previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    float previousOxyAmount = OxygenInit.Percent;

                    OxygenInit.Percent = Mathf.Clamp(OxygenInit.Percent + OxygenBase.OxygenConfig.oxyBoost_increasingValue.Value, 0f, 1f);

                    mls.LogDebug($"Player's oxygen: {OxygenInit.Percent}");
                    mls.LogDebug($"OxyBoost fuel: {fuel}");

                    if (previousOxyAmount <= 0.3f && StartOfRound.Instance.drowningTimer > 0.3f)
                    {
                        StartOfRound.Instance.playedDrowningSFX = false;
                    }
                }
            }
        }
    }
}
