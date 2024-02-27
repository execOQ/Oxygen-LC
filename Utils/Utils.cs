using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;

namespace Oxygen.Patches
{
    internal static class Utilities
    {
        public static ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource($"{OxygenBase.modName} > Utils");

        public static T GetPrivateField<T>(this object obj, string field)
        {
            return (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }

        internal static AssetBundle LoadAssetFromStream(string path)
        {
            try
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                if (stream == null)
                {
                    string errorMessage = "Failed to get stream of resources...";
                    mls.LogError(errorMessage);
                    return null; 
                }

                AssetBundle bundle = AssetBundle.LoadFromStream(stream);
                if (bundle == null)
                {
                    string errorMessage = "Failed to load audio assets...";
                    mls.LogError(errorMessage);
                    return null;
                }

                return bundle;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Exception: {ex.Message}";
                mls.LogError(errorMessage);

                return null; 
            }
        }
    }
}
