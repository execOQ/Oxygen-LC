using BepInEx.Logging;
using Oxygen.Configuration;
using System.Collections;
using UnityEngine;

namespace Oxygen.Items
{
    class Oxy99 : TetraChemicalItem
    {
        //public static ManualLogSource mls = OxygenBase.Instance.mls;

        public new IEnumerator UseTZPAnimation()
        {
            thisAudioSource.PlayOneShot(holdCanSFX);
            WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
            yield return new WaitForSeconds(0.75f);
            emittingGas = true;

            if (base.IsOwner)
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
                    float previousOxyAmount = OxygenInit.oxygenUI.fillAmount;

                    OxygenInit.oxygenUI.fillAmount = Mathf.Clamp(OxygenInit.oxygenUI.fillAmount + OxygenConfig.Instance.oxyBoost_increasingValue.Value, 0f, 1f);

                    //mls.LogInfo($"oxygen: {OxygenHUD.oxygenUI.fillAmount}");
                    //mls.LogInfo($"fuel: {fuel}");

                    if (previousOxyAmount <= 0.3f && StartOfRound.Instance.drowningTimer > 0.3f)
                    {
                        StartOfRound.Instance.playedDrowningSFX = false;
                    }
                }
            }
        }
    }
}
