using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oxygen.General
{
    // Built at runtime so existing suits and the shipped tank asset remain usable.
    internal sealed class BackpackGauge : MonoBehaviour
    {
        private PlayerControllerB owner;
        private Mesh arc, ticks;
        private GaugeModel model;
        private AudioSource pump;
        private float previous = -1;
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
            var fill = new GameObject("Blue oxygen arc");
            fill.transform.SetParent(transform, false);
            fill.transform.localPosition = new Vector3(0, 0, GaugeModel.ArcDepth);
            arc = new Mesh { name = "Oxygen gauge fill" };
            fill.AddComponent<MeshFilter>().sharedMesh = arc;
            fill.AddComponent<MeshRenderer>().sharedMaterial = model.Blue;
            BuildTicks();
            pump = gameObject.AddComponent<AudioSource>();
            pump.spatialBlend = 1; pump.loop = true; pump.volume = 0.08f;
            pump.minDistance = 0.5f; pump.maxDistance = 4;
            pump.rolloffMode = AudioRolloffMode.Linear;
            var clips = OxygenBase.Instance.oxyChargerSFX;
            if (clips != null && clips.Length > 0) pump.clip = clips[0];
        }

        private void BuildTicks()
        {
            var vertices = new Vector3[20];
            var triangles = new int[30];
            for (int i = 0; i < 5; i++)
            {
                bool major = i % 2 == 0;
                float angle = (225 - 270 * i / 4f) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var side = new Vector3(-direction.y, direction.x, 0) * (major ? 0.001f : 0.00065f);
                var inner = direction * (major ? 0.040f : 0.059f);
                var outer = direction * 0.071f;
                int v = i * 4, t = i * 6;
                vertices[v] = inner - side; vertices[v + 1] = inner + side;
                vertices[v + 2] = outer - side; vertices[v + 3] = outer + side;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            ticks = new Mesh { name = "Oxygen gauge level ticks" };
            ticks.vertices = vertices; ticks.triangles = triangles; ticks.RecalculateNormals();
            var marks = new GameObject("Level ticks 0 25 50 75 100");
            marks.transform.SetParent(transform, false);
            // Keep marks in front of the fill at every oxygen level.
            marks.transform.localPosition = new Vector3(0, 0, GaugeModel.TickDepth);
            marks.AddComponent<MeshFilter>().sharedMesh = ticks;
            var renderer = marks.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = model.Marks;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
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
            amount = Mathf.Clamp01(amount);
            if (Mathf.Abs(amount - previous) < 0.001f) return;
            previous = amount;
            const int segments = 48;
            var vertices = new Vector3[(segments + 1) * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                // Unity's camera sees local -X on the left when facing this -Z dial.
                float angle = (225 - 270 * amount * i / segments) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                vertices[i * 2] = direction * 0.043f;
                vertices[i * 2 + 1] = direction * 0.066f;
                uv[i * 2] = new Vector2(vertices[i * 2].x / 0.146f + 0.5f, vertices[i * 2].y / 0.146f + 0.5f);
                uv[i * 2 + 1] = new Vector2(vertices[i * 2 + 1].x / 0.146f + 0.5f, vertices[i * 2 + 1].y / 0.146f + 0.5f);
                if (i == segments) continue;
                int t = i * 6, v = i * 2;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
            arc.Clear(); arc.vertices = vertices; arc.uv = uv; arc.triangles = triangles; arc.RecalculateNormals();
        }

        private void OnDestroy()
        {
            Destroy(arc); Destroy(ticks); model?.Destroy();
        }
    }
}
