using System.Collections.Generic;
using UnityEngine;

namespace ARFishApp.Data
{
    public static class FishJsonDatabase
    {
        private const string ResourceFolder = "FishJson/";
        private static readonly Dictionary<string, FishJsonData> Cache = new Dictionary<string, FishJsonData>();

        public static FishJsonData Load(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return null;
            }

            string normalizedId = fishId.Trim().ToLowerInvariant();
            if (Cache.TryGetValue(normalizedId, out FishJsonData cachedData))
            {
                return cachedData;
            }

            TextAsset jsonAsset = Resources.Load<TextAsset>(ResourceFolder + normalizedId);
            if (jsonAsset == null)
            {
                Debug.LogWarning($"[FishJsonDatabase] JSON bulunamadi: Resources/{ResourceFolder}{normalizedId}.json");
                return null;
            }

            FishJsonData data = JsonUtility.FromJson<FishJsonData>(jsonAsset.text);
            if (data == null)
            {
                Debug.LogWarning($"[FishJsonDatabase] JSON okunamadi: {normalizedId}");
                return null;
            }

            Cache[normalizedId] = data;
            return data;
        }
    }
}
