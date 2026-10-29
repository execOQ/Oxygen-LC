using UnityEngine;
using UnityEngine.Rendering;

namespace Oxygen.General
{
    // Shared physical scale for backpack and station gauges.
    internal sealed class GaugeDial
    {
        private readonly Transform transform;
        private readonly GaugeModel model;
        private Mesh arc, ticks;
        private float previous = -1;

        internal GaugeDial(Transform transform, GaugeModel model)
        {
            this.transform = transform;
            this.model = model;
            var fill = new GameObject("Blue oxygen arc");
            fill.transform.SetParent(transform, false);
            fill.transform.localPosition = new Vector3(0, 0, GaugeModel.ArcDepth);
            arc = new Mesh { name = "Oxygen gauge fill" };
            fill.AddComponent<MeshFilter>().sharedMesh = arc;
            fill.AddComponent<MeshRenderer>().sharedMaterial = model.Blue;
            BuildTicks();
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

        internal void SetAmount(float amount)
        {
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

        internal void Destroy()
        {
            Object.Destroy(arc); Object.Destroy(ticks);
        }
    }
}
