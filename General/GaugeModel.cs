using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oxygen.General
{
    // Small, opaque, low-poly hardware; every generated asset belongs to its gauge.
    internal sealed class GaugeModel
    {
        internal const float ArcDepth = -0.0225f, TickDepth = -0.0232f;
        private readonly Transform root;
        private readonly Shader gameShader;
        private readonly List<Object> assets = new();
        private readonly Material steel, casing, brass, rubber, face;
        internal readonly Material Blue, Marks;

        internal GaugeModel(Transform root, Shader gameShader = null)
        {
            this.root = root;
            this.gameShader = gameShader;
            steel = WornMaterial("Worn steel", new Color(0.38f, 0.40f, 0.39f), 0.55f, true);
            casing = WornMaterial("Chipped charcoal case", new Color(0.13f, 0.15f, 0.15f), 0.35f, true);
            brass = WornMaterial("Tarnished fitting", new Color(0.36f, 0.29f, 0.16f), 0.55f, true);
            rubber = WornMaterial("Old rubber gasket", new Color(0.055f, 0.06f, 0.052f), 0, false);
            face = WornMaterial("Aged enamel dial", new Color(0.76f, 0.74f, 0.65f), 0, false);
            Blue = WornMaterial("Worn blue paint", new Color(0.08f, 0.29f, 0.66f), 0, false);
            Marks = WornMaterial("Printed level marks", new Color(0.08f, 0.09f, 0.075f), 0, false);
        }

        internal void Build()
        {
            Disk("Pressure gauge case", 0.083f, 0.036f, 0.008f, casing);
            Disk("Recessed enamel dial", 0.073f, 0.006f, -0.018f, face);
            Bezel();
            // A threaded pressure fitting seats the case on a saddle and tank band.
            Cylinder("Threaded pressure stem", Vector3.forward * 0.026f, Vector3.forward * 0.09f, 0.012f, brass, 12);
            Cylinder("Hex union", Vector3.forward * 0.029f, Vector3.forward * 0.048f, 0.022f, brass, 6);
            Cylinder("Fitting seal", Vector3.forward * 0.049f, Vector3.forward * 0.056f, 0.018f, rubber, 12);
            // Seat the back of the saddle into the tank surface, not in free space.
            Box("Steel mounting saddle", new Vector3(0, 0, 0.11f), new Vector3(0.105f, 0.12f, 0.06f), steel);
            Band();
            for (int side = -1; side <= 1; side += 2)
            {
                var at = new Vector3(side * 0.039f, 0, 0.075f);
                Cylinder("Saddle bolt", at, at + Vector3.forward * 0.009f, 0.0055f, steel, 6);
                Box("Screw slot", at - Vector3.forward * 0.001f, new Vector3(0.006f, 0.0012f, 0.001f), rubber);
            }
        }

        private Material WornMaterial(string name, Color color, float metallic, bool chipped)
        {
            var shader = gameShader ?? Shader.Find("HDRP/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { name = "Oxygen " + name, color = Color.white };
            assets.Add(material);
            const int size = 128;
            var pixels = new Color[size * size];
            // The same planar grain underlies face, fill and printing. No global RNG.
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.19f + 7.1f, y * 0.19f + 11.4f);
                    float grime = Mathf.PerlinNoise(x * 0.057f + 41, y * 0.057f + 19);
                    float edge = Mathf.Clamp01((new Vector2(x - 63.5f, y - 63.5f).magnitude - 43) / 25);
                    // The game's edge filter exaggerates fine colour noise. Keep
                    // painted printing/enamel quiet enough for the scale to read.
                    float shade = chipped
                        ? 0.79f + grain * 0.25f - grime * 0.13f - edge * 0.12f
                        : 0.94f + grain * 0.015f - grime * 0.025f - edge * 0.035f;
                    pixels[y * size + x] = color * shade;
                    pixels[y * size + x].a = 1;
                }
            var random = new System.Random(7183);
            for (int stroke = 0; stroke < (chipped ? 95 : 12); stroke++)
            {
                int x = random.Next(size), y = random.Next(size);
                int length = random.Next(2, chipped ? 14 : 8);
                int dx = random.Next(2) == 0 ? -1 : 1;
                for (int p = 0; p < length; p++)
                {
                    int px = x + dx * p, py = y + p / 3;
                    if (px < 0 || px >= size || py >= size) continue;
                    var worn = chipped ? new Color(0.46f, 0.45f, 0.39f) : color * 0.57f;
                    pixels[py * size + px] = Color.Lerp(pixels[py * size + px], worn, chipped ? 0.65f : 0.12f);
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            { name = "Oxygen " + name + " wear", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(true, true); assets.Add(texture);
            material.mainTexture = texture;
            if (material.HasProperty("_BaseColorMap")) material.SetTexture("_BaseColorMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            // Match the rough stock suit/tank finish, rather than polished UI chrome.
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.075f);
            return material;
        }

        internal Material Paint(string name, Color color) => WornMaterial(name, color, 0, false);

        private void Renderer(GameObject part, Material material)
        {
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        private GameObject Primitive(string name, PrimitiveType type, Material material)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name;
            var collider = part.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
            part.transform.SetParent(root, false); Renderer(part, material); return part;
        }

        private void Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var box = Primitive(name, PrimitiveType.Cube, material);
            box.transform.localPosition = position; box.transform.localScale = size;
        }

        private void Disk(string name, float radius, float depth, float z, Material material)
        {
            var disk = Primitive(name, PrimitiveType.Cylinder, material);
            disk.transform.localPosition = Vector3.forward * z;
            disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
            disk.transform.localScale = new Vector3(radius * 2, depth / 2, radius * 2);
        }

        private void Part(string name, List<Vector3> vertices, List<int> triangles, Material material)
        {
            var mesh = new Mesh { name = "Oxygen " + name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            var uv = new Vector2[vertices.Count];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(vertices[i].x / 0.17f + 0.5f, vertices[i].y / 0.17f + 0.5f);
            mesh.uv = uv; mesh.RecalculateNormals(); assets.Add(mesh);
            var part = new GameObject(name); part.transform.SetParent(root, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>(); Renderer(part, material);
        }

        private static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int at = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.AddRange(new[] { at, at + 1, at + 2, at, at + 2, at + 3 });
        }

        private void Bezel()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var radii = new[] { 0.083f, 0.085f, 0.079f, 0.073f };
            var depths = new[] { 0.008f, -0.020f, -0.027f, -0.021f };
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20, b = (i + 1) * Mathf.PI * 2 / 20;
                for (int j = 0; j < 3; j++)
                    Quad(vertices, triangles,
                        new Vector3(Mathf.Cos(a) * radii[j], Mathf.Sin(a) * radii[j], depths[j]),
                        new Vector3(Mathf.Cos(a) * radii[j + 1], Mathf.Sin(a) * radii[j + 1], depths[j + 1]),
                        new Vector3(Mathf.Cos(b) * radii[j + 1], Mathf.Sin(b) * radii[j + 1], depths[j + 1]),
                        new Vector3(Mathf.Cos(b) * radii[j], Mathf.Sin(b) * radii[j], depths[j]));
            }
            Part("Chipped bevelled bezel", vertices, triangles, steel);
        }

        private void Cylinder(string name, Vector3 from, Vector3 to, float radius, Material material, int sides)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var axis = (to - from).normalized;
            var right = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized * radius;
            var up = Vector3.Cross(axis, right).normalized * radius;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                var x = right * Mathf.Cos(a) + up * Mathf.Sin(a);
                var y = right * Mathf.Cos(b) + up * Mathf.Sin(b);
                Quad(vertices, triangles, from + x, from + y, to + y, to + x);
                Quad(vertices, triangles, from, from + y, from + x, from);
                Quad(vertices, triangles, to, to + x, to + y, to);
            }
            Part(name, vertices, triangles, material);
        }

        private void Band()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < 12; i++)
            {
                float a = (-80 + i * 160f / 12) * Mathf.Deg2Rad;
                float b = (-80 + (i + 1) * 160f / 12) * Mathf.Deg2Rad;
                var x = new Vector3(Mathf.Sin(a) * 0.112f, 0, 0.24f - Mathf.Cos(a) * 0.112f);
                var y = new Vector3(Mathf.Sin(b) * 0.112f, 0, 0.24f - Mathf.Cos(b) * 0.112f);
                Quad(vertices, triangles, x + Vector3.up * 0.013f, y + Vector3.up * 0.013f,
                    y - Vector3.up * 0.013f, x - Vector3.up * 0.013f);
            }
            Part("Curved tank clamp", vertices, triangles, steel);
        }

        internal void Destroy()
        {
            foreach (var asset in assets) if (asset != null) Object.Destroy(asset);
            assets.Clear();
        }
    }
}
