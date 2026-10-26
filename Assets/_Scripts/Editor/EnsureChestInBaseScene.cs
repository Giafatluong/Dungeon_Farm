using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EnsureChestInBaseScene
{

    [MenuItem("Tools/Place ChestCanvas in Scene (Unpacked)")]
    public static void PlaceChestCanvas()
    {
        EnsureChest();
    }

    private static void EnsureChest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Base" && scene.path != "Assets/_Scenes/Base.unity")
        {
            scene = EditorSceneManager.OpenScene("Assets/_Scenes/Base.unity", OpenSceneMode.Single);
        }

        GameObject existing = GameObject.Find("ChestCanvas");
        if (existing != null)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(existing))
            {
                PrefabUtility.UnpackPrefabInstance(existing, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Transform overlay = existing.transform.Find("ChestOverlay");
                if (overlay != null) overlay.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[EnsureChestInBaseScene] Unpacked existing ChestCanvas completely into scene GameObjects!");
            }
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/ChestCanvas.prefab");
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "ChestCanvas";
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform overlay = instance.transform.Find("ChestOverlay");
            if (overlay != null) overlay.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EnsureChestInBaseScene] Placed ChestCanvas into Base.unity and unpacked completely!");
        }
        else
        {
            GameObject built = ChestCanvasBuilder.BuildChestCanvas();
            if (built != null && PrefabUtility.IsPartOfPrefabInstance(built))
            {
                PrefabUtility.UnpackPrefabInstance(built, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EnsureChestInBaseScene] Built ChestCanvas, unpacked, and saved scene!");
        }
    }
}
