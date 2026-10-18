using UnityEngine;

namespace Oxygen.General
{
    internal static class VanillaCanisters
    {
        private static GameObject cached;

        internal static void SetVisible(bool visible)
        {
            if (cached == null)
            {
                var suitParts = GameObject.Find("Environment/HangarShip/ScavengerModelSuitParts");
                // Transform.Find includes inactive children; GameObject.Find does not.
                cached = suitParts != null ? suitParts.transform.Find("Circle.002")?.gameObject : null;
            }
            if (cached != null) cached.SetActive(visible);
        }

        internal static void Reset() => cached = null;
    }
}
