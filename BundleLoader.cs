using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetBundleLoader
{
    public static class BundleLoader
    {
        private static Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();

        public static AssetBundle LoadAssetBundle(string bundlePath)
        {
            string key = System.IO.Path.GetFileName(bundlePath);

            if (loadedAssetBundles.ContainsKey(key))
            {
                return loadedAssetBundles[key];
            }

            AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
            if (assetBundle == null)
            {
                Plugin.Log.LogError("Failed to load AssetBundle at path: " + bundlePath);
                return null;
            }

            loadedAssetBundles.Add(key, assetBundle);
            return assetBundle;
        }

        public static string GetDefaultModAssetBundlePath(string filename)
        {
            string gameDirectory = Path.GetDirectoryName(Application.dataPath);
            string relativePath = Path.Combine("BepInEx", "plugins", "tarkin", "bundles", filename);
            string fullPath = Path.Combine(gameDirectory, relativePath);

            return fullPath;
        }
    }
}
