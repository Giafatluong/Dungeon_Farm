using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TreeSorter : MonoBehaviour
{
    private struct TreeData
    {
        public SpriteRenderer sr;
        public BoxCollider2D box;
        public float trunkX;
        public float trunkY;
        public float halfWidth;
        public float topY;
    }

    private static TreeData[] sceneTrees;
    private static Transform playerTransform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sceneTrees = null;
        playerTransform = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndCacheTrees();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = null;
        FindAndCacheTrees();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void FindAndCacheTrees()
    {
        GameObject treeParent = GameObject.Find("Tree");
        if (treeParent != null)
        {
            SpriteRenderer[] renderers = treeParent.GetComponentsInChildren<SpriteRenderer>(true);
            List<TreeData> list = new List<TreeData>();
            foreach (var sr in renderers)
            {
                if (sr.gameObject == treeParent) continue;
                sr.sortingOrder = 3;
                BoxCollider2D box = sr.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    TreeData data = new TreeData
                    {
                        sr = sr,
                        box = box,
                        trunkX = sr.transform.position.x + box.offset.x,
                        trunkY = sr.transform.position.y + box.offset.y,
                        halfWidth = box.size.x * 0.5f + 0.8f,
                        topY = sr.transform.position.y + 3.0f
                    };
                    list.Add(data);
                }
            }
            sceneTrees = list.ToArray();

            if (treeParent.GetComponent<TreeSorter>() == null)
            {
                treeParent.AddComponent<TreeSorter>();
            }
        }
    }

    private void Update()
    {
        if (sceneTrees == null || sceneTrees.Length == 0) return;

        if (playerTransform == null)
        {
            if (PlayerMovement.Instance != null)
                playerTransform = PlayerMovement.Instance.transform;
            else
                return;
        }

        Vector3 pPos = playerTransform.position;
        float feetY = pPos.y - 0.42f;

        for (int i = 0; i < sceneTrees.Length; i++)
        {
            var tree = sceneTrees[i];
            if (tree.sr == null) continue;

            int targetOrder = 3;
            if (Mathf.Abs(pPos.x - tree.trunkX) <= tree.halfWidth && feetY >= tree.trunkY && pPos.y <= tree.topY)
            {
                targetOrder = 4;
            }

            if (tree.sr.sortingOrder != targetOrder)
            {
                tree.sr.sortingOrder = targetOrder;
            }
        }
    }
}
