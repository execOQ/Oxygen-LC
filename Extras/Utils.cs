using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;
using System.Linq;

namespace Oxygen.Extras
{
    internal static class Utilities
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource($"{OxygenBase.modName} > Utils");

        public static T GetPrivateField<T>(this object obj, string field)
        {
            return (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        public static string NumberLessPlanetName(string moon)
        {
            if (moon != null)
            {
                return new string(moon.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
            }
            return string.Empty;
        }

        internal static AssetBundle LoadAssetFromStream(string path)
        {
            try
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                if (stream == null)
                {
                    mls.LogError("Failed to get stream of resources...");
                    return null;
                }

                AssetBundle bundle = AssetBundle.LoadFromStream(stream);
                if (bundle == null)
                {
                    mls.LogError("Failed to load assets...");
                    return null;
                }

                return bundle;
            }
            catch (Exception ex)
            {
                mls.LogError($"Exception: {ex.Message}");

                return null;
            }
        }

        internal static AudioClip[] LoadAudioClips(AssetBundle bundle, params string[] paths)
        {
            return paths.Select(path => bundle.LoadAsset<AudioClip>(path))
                        .Where(clip => clip != null)
                        .ToArray();
        }
    }
}
