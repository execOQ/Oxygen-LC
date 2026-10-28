using System.Collections.Generic;
using UnityEngine;

namespace Oxygen.General
{
    // The bundle's HDRP/Lit was built without stereo variants. Use the game's
    // shader object, rather than Shader.Find which can return the bundled copy.
    internal sealed class ChargerVisuals : MonoBehaviour
    {
        private readonly List<Material> materials = new();

        private void Start()
        {
            var suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
            var cylinders = suitParts != null ? suitParts.transform.Find("Circle.002") : null;
            var stockRenderer = cylinders != null ? cylinders.GetComponentInChildren<Renderer>(true) : null;
            var shader = stockRenderer != null ? stockRenderer.sharedMaterial?.shader : null;
            if (shader == null)
            {
                Configuration.OxygenConfig.mls.LogWarning("OxyCharger: stock canister shader unavailable; keeping bundled materials.");
                return;
            }
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var original = slots[i];
                    // Only exchange compatible shader implementations. Cloning
                    // keeps the charger's maps, colours, wear and surface values.
                    if (original == null || original.shader == shader || original.shader.name != shader.name) continue;
                    var material = new Material(original)
                    {
                        name = original.name + " (Oxygen game shader)",
                        shader = shader
                    };
                    materials.Add(material);
                    slots[i] = material;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materials) if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}
