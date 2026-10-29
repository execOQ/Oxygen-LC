using GameNetcodeStuff;
using UnityEngine;

namespace Oxygen.General
{
    // Built at runtime so existing suits and the shipped tank asset remain usable.
    internal sealed class BackpackGauge : MonoBehaviour
    {
        private PlayerControllerB owner;
        private GaugeDial dial;
        private GaugeModel model;
        private AudioSource pump;
        private static readonly Vector3 MountOffset = new(-0.12f, 0.04f, -0.43f);

        internal static Bounds InteractionBounds(PlayerControllerB player)
        {
            var gauge = player.GetComponentInChildren<BackpackGauge>(true);
            // Follow the same animated mount that carries the visible dial.
            // A fixed player-root height misses upright tanks and VR IK poses.
            var center = gauge != null ? gauge.transform.position :
                player.transform.TransformPoint(MountOffset + Vector3.up * 1.71f);
            return new Bounds(center + player.transform.forward * 0.1f, new Vector3(0.8f, 1.0f, 0.65f));
        }

        internal static BackpackGauge Create(PlayerControllerB player)
        {
            var root = new GameObject("Oxygen backpack gauge");
            root.transform.SetParent(player.transform, false);
            root.transform.localPosition = MountOffset + Vector3.up * 1.71f;
            // Prefer an animated spine; keep a root fallback for replacement rigs.
            Transform spine = null;
            foreach (var child in player.GetComponentsInChildren<Transform>(true))
                if (child.name == "spine.003" || child.name == "spine_003") { spine = child; break; }
            if (spine != null) root.transform.SetParent(spine, true);
            var gauge = root.AddComponent<BackpackGauge>();
            gauge.owner = player;
            var frame = player.transform.localToWorldMatrix;
            if (spine != null)
            {
                // A bind pose describes the rig before animation. Calibrating
                // against the spawn animation instead makes crouch/spawn poses
                // change the mounting angle permanently.
                foreach (var skin in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (skin.sharedMesh == null) continue;
                    int index = System.Array.IndexOf(skin.bones, spine);
                    var bindPoses = skin.sharedMesh.bindposes;
                    if (index < 0 || index >= bindPoses.Length) continue;
                    frame = spine.localToWorldMatrix * bindPoses[index] * skin.transform.worldToLocalMatrix * player.transform.localToWorldMatrix;
                    break;
                }
            }
            // Imported skin bind poses also contain the model's unit scale.
            // The hardware offsets are metres; use only the animated orientation.
            var rotation = Quaternion.LookRotation(frame.MultiplyVector(Vector3.forward), frame.MultiplyVector(Vector3.up));
            root.transform.SetPositionAndRotation(spine != null
                ? spine.position + rotation * MountOffset
                : player.transform.TransformPoint(MountOffset + Vector3.up * 1.71f), rotation);
            // Keep this local pose fixed. Unity then applies every bone update to
            // both tank and hardware, including animation/IK after LateUpdate.
            // Recomputing a world pose each frame can counteract later bone motion.
            gauge.Build();
            return gauge;
        }

        private void Build()
        {
            model = new GaugeModel(transform);
            model.Build();
            dial = new GaugeDial(transform, model);
            pump = gameObject.AddComponent<AudioSource>();
            pump.spatialBlend = 1; pump.loop = true; pump.volume = 0.08f;
            pump.minDistance = 0.5f; pump.maxDistance = 4;
            pump.rolloffMode = AudioRolloffMode.Linear;
            var clips = OxygenBase.Instance.oxyChargerSFX;
            if (clips != null && clips.Length > 0) pump.clip = clips[0];
        }

        internal void SetState(float amount, bool transferring)
        {
            bool visible = owner != null && owner.isPlayerControlled && !owner.isPlayerDead;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
            if (visible && transferring && pump.clip != null)
            {
                if (!pump.isPlaying) pump.Play();
            }
            else if (pump.isPlaying) pump.Stop();
            dial.SetAmount(amount);
        }

        private void OnDestroy()
        {
            dial?.Destroy(); model?.Destroy();
        }
    }
}
