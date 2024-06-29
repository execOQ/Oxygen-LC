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

        internal static T GetPrivateField<T>(this object obj, string field)
        {
            return (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        internal static string GetLLLNameOfLevel(string levelName)
        {
            string text = StripSpecialCharacters(GetNumberlessPlanetName(levelName));
            if (!text.EndsWith("Level"))
            {
                text += "Level";
            }
            return text;
        }

        internal static string GetNumberlessPlanetName(string planetName)
        {
            if (planetName != null)
            {
                return new string(planetName.SkipWhile((char c) => !char.IsLetter(c)).ToArray());
            }
            return string.Empty;
        }

        private static string StripSpecialCharacters(string input)
        {
            string text = string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if ((!".,?!@#$%^&*()_+-=';:'\"".ToCharArray().Contains(c) && char.IsLetterOrDigit(c)) || c.ToString() == " ")
                {
                    text += c;
                }
            }
            return text;
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
