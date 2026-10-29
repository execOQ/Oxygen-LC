using Oxygen.Items;
using UnityEngine;

namespace Oxygen.General
{
    internal sealed class ChargerGauge : MonoBehaviour
    {
        private OxyCharger charger;
        private GaugeModel model;
        private GaugeDial dial;
        private GameObject unlimited;
        private Mesh infinityMesh;

        private void Start()
        {
            charger = GetComponent<OxyCharger>();
            var root = new GameObject("Shared ship oxygen reserve gauge");
            root.transform.SetParent(transform, false);
            // Source cylinders run along local Z. Mount on the broad front (-Y),
            // like the player tanks, with the scale upright along the cylinder.
            root.transform.localPosition = new Vector3(0.85f, -0.22f, 0.5f);
            root.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            root.transform.localScale = Vector3.one * (1.35f / 0.2077057f);

            var suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
            var stock = suitParts != null ? suitParts.transform.Find("Circle.002") : null;
            var renderer = stock != null ? stock.GetComponentInChildren<Renderer>(true) : null;
            model = new GaugeModel(root.transform, renderer != null ? renderer.sharedMaterial?.shader : null);
            model.Build();
            dial = new GaugeDial(root.transform, model);

            unlimited = new GameObject("Unlimited reserve infinity mark");
            unlimited.transform.SetParent(root.transform, false);
            unlimited.transform.localPosition = new Vector3(0, 0, GaugeModel.TickDepth);
            infinityMesh = Infinity();
            unlimited.AddComponent<MeshFilter>().sharedMesh = infinityMesh;
            unlimited.AddComponent<MeshRenderer>().sharedMaterial = model.Marks;

            LateUpdate();
        }

        private void LateUpdate()
        {
            if (dial == null || charger == null) return;
            // No local oxygen/configuration reads: all three values are server replicas.
            bool limited = charger.ReserveLimited;
            dial.SetAmount(limited ? (charger.ReserveEmpty ? 0f : charger.ReserveFraction) : 1f);
            unlimited.SetActive(!limited);
        }

        private static Mesh Infinity()
        {
            const int segments = 64;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float t = i * Mathf.PI * 2 / segments;
                var point = new Vector3(0.023f * Mathf.Cos(t), 0.012f * Mathf.Sin(2 * t), 0);
                var tangent = new Vector3(-0.023f * Mathf.Sin(t), 0.024f * Mathf.Cos(2 * t), 0).normalized;
                var side = new Vector3(-tangent.y, tangent.x, 0) * 0.0015f;
                vertices[i * 2] = point - side;
                vertices[i * 2 + 1] = point + side;
                if (i == segments) continue;
                int v = i * 2, at = i * 6;
                // Clockwise when viewed from the dial's local -Z side.
                triangles[at] = v; triangles[at + 1] = v + 1; triangles[at + 2] = v + 2;
                triangles[at + 3] = v + 1; triangles[at + 4] = v + 3; triangles[at + 5] = v + 2;
            }
            var mesh = new Mesh { name = "Oxygen unlimited reserve symbol" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            dial?.Destroy();
            model?.Destroy();
            if (infinityMesh != null) Destroy(infinityMesh);
        }
    }
}
