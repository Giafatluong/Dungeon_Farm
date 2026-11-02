using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TreeSorter : MonoBehaviour
{
    private struct ObstacleData
    {
        public Renderer renderer;
        public float baseX;
        public float baseY;
        public float halfWidth;
        public float topY;
    }

    private static ObstacleData[] sceneObstacles;
    private static Transform playerTransform;
    private static Vector3 lastPlayerPos = new Vector3(float.MinValue, float.MinValue, 0f);
    private static float nextPlayerSearchTime = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sceneObstacles = null;
        playerTransform = null;
        lastPlayerPos = new Vector3(float.MinValue, float.MinValue, 0f);
        nextPlayerSearchTime = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndCacheWorldObjects();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = null;
        lastPlayerPos = new Vector3(float.MinValue, float.MinValue, 0f);
        nextPlayerSearchTime = 0f;
        FindAndCacheWorldObjects();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Kept for backward compatibility
    public static void FindAndCacheTrees()
    {
        FindAndCacheWorldObjects();
    }

    public static void FindAndCacheWorldObjects()
    {
        List<ObstacleData> list = new List<ObstacleData>();

        // 1. Goddess Statue
        GameObject statueObj = GameObject.Find("Statue");
        if (statueObj != null)
        {
            Renderer sr = statueObj.GetComponent<Renderer>();
            if (sr != null)
            {
                // Statue base is at (40.5, 10.88), width ~2.2, height ~3.0
                sr.sortingOrder = 2;
                list.Add(new ObstacleData
                {
                    renderer = sr,
                    baseX = 40.5f,
                    baseY = 10.88f,
                    halfWidth = 1.6f,
                    topY = 14.0f
                });
            }
        }

        // 2. Well
        GameObject wellObj = GameObject.Find("Well");
        if (wellObj != null)
        {
            // Ensure collider
            if (wellObj.GetComponent<Collider2D>() == null)
            {
                var col = wellObj.AddComponent<BoxCollider2D>();
                col.offset = new Vector2(5.5f, 0.4f);
                col.size = new Vector2(1.1f, 0.7f);
            }

            Renderer wr = wellObj.GetComponent<Renderer>();
            if (wr != null)
            {
                wr.sortingOrder = 2;
                list.Add(new ObstacleData
                {
                    renderer = wr,
                    baseX = 5.5f,
                    baseY = 0.4f,
                    halfWidth = 1.2f,
                    topY = 2.0f
                });
            }
        }

        // 3. Campfire
        GameObject campObj = GameObject.Find("Campfire_Anim_1");
        if (campObj == null) campObj = GameObject.Find("Campfire");
        if (campObj != null)
        {
            if (campObj.GetComponent<Collider2D>() == null)
            {
                var col = campObj.AddComponent<CircleCollider2D>();
                col.offset = new Vector2(0f, -0.2f);
                col.radius = 0.45f;
            }

            Renderer cr = campObj.GetComponent<Renderer>();
            if (cr != null)
            {
                cr.sortingOrder = 2;
                list.Add(new ObstacleData
                {
                    renderer = cr,
                    baseX = campObj.transform.position.x,
                    baseY = campObj.transform.position.y - 0.2f,
                    halfWidth = 0.8f,
                    topY = campObj.transform.position.y + 0.8f
                });
            }
        }

        // 4. House
        GameObject houseObj = GameObject.Find("House");
        if (houseObj != null)
        {
            // Ensure a solid blocking collider exists (in addition to the interaction trigger)
            Collider2D[] cols = houseObj.GetComponents<Collider2D>();
            bool hasSolidCollider = false;
            foreach (var c in cols)
            {
                if (!c.isTrigger) { hasSolidCollider = true; break; }
            }
            if (!hasSolidCollider)
            {
                var solidCol = houseObj.AddComponent<BoxCollider2D>();
                solidCol.isTrigger = false;
                solidCol.offset = new Vector2(12.0f, 2.3f);
                solidCol.size = new Vector2(4.6f, 1.8f);
            }

            Renderer hr = houseObj.GetComponent<Renderer>();
            if (hr != null)
            {
                hr.sortingOrder = 2;
                list.Add(new ObstacleData
                {
                    renderer = hr,
                    baseX = 12.0f,
                    baseY = 1.4f,
                    halfWidth = 2.8f,
                    topY = 4.8f
                });
            }
        }

        // 5. HomeChest
        GameObject chestObj = GameObject.Find("HomeChest");
        if (chestObj != null)
        {
            Renderer chr = chestObj.GetComponent<Renderer>();
            if (chr != null)
            {
                chr.sortingOrder = 2;
                list.Add(new ObstacleData
                {
                    renderer = chr,
                    baseX = chestObj.transform.position.x,
                    baseY = chestObj.transform.position.y - 0.15f,
                    halfWidth = 0.7f,
                    topY = chestObj.transform.position.y + 0.6f
                });
            }
        }

        // 6. Trees
        GameObject treeParent = GameObject.Find("Tree");
        if (treeParent != null)
        {
            Renderer[] renderers = treeParent.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.gameObject == treeParent) continue;
                if (ShouldSkipObject(r.gameObject.name)) continue;

                r.sortingOrder = 2;
                BoxCollider2D box = r.GetComponent<BoxCollider2D>();
                if (box == null)
                {
                    box = r.gameObject.AddComponent<BoxCollider2D>();
                    box.offset = new Vector2(0f, -0.2f);
                    box.size = new Vector2(0.8f, 0.4f);
                }

                float bx = r.transform.position.x + box.offset.x;
                float by = r.transform.position.y + box.offset.y - (box.size.y * 0.5f);
                float hw = Mathf.Max(box.size.x * 0.5f + 0.6f, r.bounds.extents.x * 0.9f);
                float ty = r.bounds.max.y > by ? r.bounds.max.y : r.transform.position.y + 3.0f;

                list.Add(new ObstacleData
                {
                    renderer = r,
                    baseX = bx,
                    baseY = by,
                    halfWidth = hw,
                    topY = ty
                });
            }

            if (treeParent.GetComponent<TreeSorter>() == null)
            {
                treeParent.AddComponent<TreeSorter>();
            }
        }

        // 7. Decor Props
        GameObject decorParent = GameObject.Find("Decor");
        if (decorParent != null)
        {
            Renderer[] renderers = decorParent.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.gameObject == decorParent) continue;
                if (ShouldSkipObject(r.gameObject.name)) continue;

                r.sortingOrder = 2;
                Collider2D col = r.GetComponent<Collider2D>();
                if (col == null)
                {
                    var box = r.gameObject.AddComponent<BoxCollider2D>();
                    box.offset = new Vector2(0f, -0.2f);
                    box.size = new Vector2(0.8f, 0.4f);
                    col = box;
                }

                float bx = col.bounds.center.x;
                float by = col.bounds.min.y;
                float hw = Mathf.Max(col.bounds.extents.x + 0.4f, r.bounds.extents.x * 0.85f);
                float ty = r.bounds.max.y > by ? r.bounds.max.y : r.transform.position.y + 1.5f;

                list.Add(new ObstacleData
                {
                    renderer = r,
                    baseX = bx,
                    baseY = by,
                    halfWidth = hw,
                    topY = ty
                });
            }

            if (treeParent == null && decorParent.GetComponent<TreeSorter>() == null)
            {
                decorParent.AddComponent<TreeSorter>();
            }
        }

        sceneObstacles = list.ToArray();
    }

    private static bool ShouldSkipObject(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;

        // Constraint: NEVER modify dungeon entrance or related portals/triggers
        if (name.IndexOf("Dungeon", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (name.IndexOf("Enter", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (name.IndexOf("Portal", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

        // Ground / Background layers to skip
        if (name == "StoneGround" || name == "Lake" || name == "Grass" || name == "Hoa" || name == "Bong" || name == "Ruong" || name == "Wall")
            return true;

        // UI / Managers
        if (name.IndexOf("Canvas", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (name.IndexOf("Manager", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (name.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;

        return false;
    }

    private void Update()
    {
        if (sceneObstacles == null || sceneObstacles.Length == 0) return;

        if (playerTransform == null)
        {
            if (PlayerMovement.Instance != null)
            {
                playerTransform = PlayerMovement.Instance.transform;
            }
            else
            {
                if (Time.time < nextPlayerSearchTime) return;
                nextPlayerSearchTime = Time.time + 0.5f;

                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                else return;
            }
        }

        Vector3 pPos = playerTransform.position;
        // Early exit if the player hasn't moved at all this frame (avoids looping 50+ obstacles while standing still)
        if (pPos == lastPlayerPos) return;
        lastPlayerPos = pPos;

        // Player feet position (visual ground contact point)
        float feetY = pPos.y - 0.42f;

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            var obs = sceneObstacles[i];
            if (obs.renderer == null) continue;

            // Sorting logic:
            // - Player is always sortingOrder = 3
            // - When Player stands in front (lower Y / south of object base):
            //   Object sortingOrder = 2 -> Player (3) renders ON TOP of the object.
            // - When Player stands behind (higher Y / north of object base):
            //   Object sortingOrder = 4 -> Object (4) renders ON TOP of Player (3), obscuring the player.
            int targetOrder = 2;
            if (Mathf.Abs(pPos.x - obs.baseX) <= obs.halfWidth && feetY >= obs.baseY && pPos.y <= obs.topY)
            {
                targetOrder = 4;
            }

            if (obs.renderer.sortingOrder != targetOrder)
            {
                obs.renderer.sortingOrder = targetOrder;
            }
        }
    }
}
