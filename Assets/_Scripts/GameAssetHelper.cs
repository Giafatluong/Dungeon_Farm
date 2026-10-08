using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Centralized asset helper that reliably discovers and loads ScriptableObject assets
/// (ItemData, CropData, RecipeData, FloorData, StatEffectData, etc.) both in the Unity Editor
/// (via AssetDatabase) and at runtime (via Resources/cache/memory fallbacks).
/// Solves the systemic issue where Resources.FindObjectsOfTypeAll returned empty arrays
/// when assets were not yet referenced or loaded into memory.
/// </summary>
public static class GameAssetHelper
{
    private static readonly Dictionary<System.Type, object> _cache = new();

    public static T[] LoadAll<T>() where T : UnityEngine.Object
    {
        System.Type type = typeof(T);

        // Check valid cache
        if (_cache.TryGetValue(type, out object cachedObj) && cachedObj is T[] cachedArray && cachedArray.Length > 0)
        {
            bool allValid = true;
            for (int i = 0; i < cachedArray.Length; i++)
            {
                if (cachedArray[i] == null) { allValid = false; break; }
            }
            if (allValid) return cachedArray;
        }

        List<T> resultList = new();

        // 1. Try Resources.LoadAll if present
        T[] resLoaded = Resources.LoadAll<T>("");
        if (resLoaded != null && resLoaded.Length > 0)
        {
            for (int i = 0; i < resLoaded.Length; i++)
            {
                if (resLoaded[i] != null && !resultList.Contains(resLoaded[i]))
                {
                    resultList.Add(resLoaded[i]);
                }
            }
        }

        // 2. In Unity Editor, search through AssetDatabase directly on disk
#if UNITY_EDITOR
        string typeFilter = typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeFilter}");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && !resultList.Contains(asset))
            {
                resultList.Add(asset);
            }
        }
#endif

        // 3. Fallback: currently loaded objects in memory
        T[] memLoaded = Resources.FindObjectsOfTypeAll<T>();
        if (memLoaded != null)
        {
            for (int i = 0; i < memLoaded.Length; i++)
            {
                if (memLoaded[i] != null && !resultList.Contains(memLoaded[i]))
                {
                    resultList.Add(memLoaded[i]);
                }
            }
        }

        T[] finalArray = resultList.ToArray();
        _cache[type] = finalArray;
        return finalArray;
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }
}
