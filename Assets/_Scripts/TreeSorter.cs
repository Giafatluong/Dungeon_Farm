using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Persistent World Sorting & Transparency Manager.
/// Manages dynamic 2D sorting order and foliage transparency for trees, buildings, and decor props.
/// When the player walks behind trees or foliage, the obstacles smoothly become semi-transparent
/// (alpha ~0.45f) and render on top of the player so the player is never hidden blindly,
/// and returns smoothly to opaque when stepping out.
/// Survives scene reloads (DontDestroyOnLoad) so starting a New Game never breaks sorting.
/// </summary>
public class TreeSorter : MonoBehaviour
{
    private struct ObstacleData
    {
        public Renderer renderer;
        public SpriteRenderer spriteRenderer;
        public Tilemap tilemap;
        public float baseX;
        public float baseY;
        public float halfWidth;
        public float topY;
        public float currentAlpha;
        public int currentOrder;
    }

    private static ObstacleData CreateObstacleData(Renderer r, float bx, float by, float hw, float ty)
    {
        SpriteRenderer sr = r as SpriteRenderer;
        Tilemap tm = null;
        if (sr == null && r is TilemapRenderer tmr)
        {
            tm = tmr.GetComponent<Tilemap>();
        }

        float a = sr != null ? sr.color.a : (tm != null ? tm.color.a : 1.0f);
        return new ObstacleData
        {
            renderer = r,
            spriteRenderer = sr,
            tilemap = tm,
            baseX = bx,
            baseY = by,
            halfWidth = hw,
            topY = ty,
            currentAlpha = a,
            currentOrder = r != null ? r.sortingOrder : 4
        };
    }

    private static TreeSorter _instance;
    public static TreeSorter Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureInstance();
            }
            return _instance;
        }
    }

    private static ObstacleData[] sceneObstacles;
    private static Transform playerTransform;
    private static float nextPlayerSearchTime = 0f;
    private static float nextCacheSearchTime = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        sceneObstacles = null;
        playerTransform = null;
        nextPlayerSearchTime = 0f;
        nextCacheSearchTime = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        EnsureInstance();
    }

    public static void EnsureInstance()
    {
        if (_instance != null) return;

        _instance = FindFirstObjectByType<TreeSorter>();
        if (_instance == null)
        {
            GameObject go = new GameObject("TreeSorter_Manager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<TreeSorter>();
        }
        else
        {
            if (_instance.transform.parent != null)
            {
                _instance.transform.SetParent(null);
            }
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        FindAndCacheWorldObjects();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        playerTransform = null;
        nextPlayerSearchTime = 0f;
        nextCacheSearchTime = 0f;
        FindAndCacheWorldObjects();
    }

    // Kept for backward compatibility
    public static void FindAndCacheTrees()
    {
        FindAndCacheWorldObjects();
    }

    public static void FindAndCacheWorldObjects()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(activeScene) && !activeScene.Equals("Base", System.StringComparison.OrdinalIgnoreCase))
        {
            sceneObstacles = System.Array.Empty<ObstacleData>();
            return;
        }

        List<ObstacleData> list = new List<ObstacleData>();

        // 1. Goddess Statue
        GameObject statueObj = GameObject.Find("Statue");
        if (statueObj != null)
        {
            Renderer sr = statueObj.GetComponent<Renderer>();
            if (sr != null)
            {
                sr.sortingOrder = 4;
                ResetVisual(sr);
                list.Add(CreateObstacleData(sr, 40.5f, 10.88f, 1.6f, 14.0f));
            }
        }

        // 2. Well
        GameObject wellObj = GameObject.Find("Well");
        if (wellObj != null)
        {
            if (wellObj.GetComponent<Collider2D>() == null)
            {
                var col = wellObj.AddComponent<BoxCollider2D>();
                col.offset = new Vector2(5.5f, 0.4f);
                col.size = new Vector2(1.1f, 0.7f);
            }

            Renderer wr = wellObj.GetComponent<Renderer>();
            if (wr != null)
            {
                wr.sortingOrder = 4;
                ResetVisual(wr);
                list.Add(CreateObstacleData(wr, 5.5f, 0.4f, 1.2f, 2.0f));
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
                cr.sortingOrder = 4;
                ResetVisual(cr);
                list.Add(CreateObstacleData(cr, campObj.transform.position.x, campObj.transform.position.y - 0.2f, 0.8f, campObj.transform.position.y + 0.8f));
            }
        }

        // 4. House
        GameObject houseObj = GameObject.Find("House");
        if (houseObj != null)
        {
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
                hr.sortingOrder = 4;
                ResetVisual(hr);
                list.Add(CreateObstacleData(hr, 12.0f, 1.4f, 2.8f, 4.8f));
            }
        }

        // 5. HomeChest
        GameObject chestObj = GameObject.Find("HomeChest");
        if (chestObj != null)
        {
            Renderer chr = chestObj.GetComponent<Renderer>();
            if (chr != null)
            {
                chr.sortingOrder = 4;
                ResetVisual(chr);
                list.Add(CreateObstacleData(chr, chestObj.transform.position.x, chestObj.transform.position.y - 0.15f, 0.7f, chestObj.transform.position.y + 0.6f));
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

                r.sortingOrder = 4;
                ResetVisual(r);

                BoxCollider2D box = r.GetComponent<BoxCollider2D>();
                float bx, by, hw, ty;

                if (box != null)
                {
                    bx = r.transform.position.x + box.offset.x;
                    by = r.transform.position.y + box.offset.y - (box.size.y * 0.5f);
                    hw = Mathf.Max(box.size.x * 0.5f + 0.6f, r.bounds.extents.x * 0.9f);
                    ty = r.bounds.max.y > by ? r.bounds.max.y : r.transform.position.y + 3.0f;
                }
                else
                {
                    bx = r.bounds.center.x;
                    by = r.bounds.min.y;
                    hw = Mathf.Max(r.bounds.extents.x * 0.9f, 0.8f);
                    ty = r.bounds.max.y;
                }

                list.Add(CreateObstacleData(r, bx, by, hw, ty));
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

                r.sortingOrder = 4;
                ResetVisual(r);

                Collider2D col = r.GetComponent<Collider2D>();
                float bx, by, hw, ty;

                if (col != null)
                {
                    bx = col.bounds.center.x;
                    by = col.bounds.min.y;
                    hw = Mathf.Max(col.bounds.extents.x + 0.4f, r.bounds.extents.x * 0.85f);
                    ty = r.bounds.max.y > by ? r.bounds.max.y : r.transform.position.y + 1.5f;
                }
                else
                {
                    bx = r.bounds.center.x;
                    by = r.bounds.min.y;
                    hw = Mathf.Max(r.bounds.extents.x * 0.85f, 0.6f);
                    ty = r.bounds.max.y;
                }

                list.Add(CreateObstacleData(r, bx, by, hw, ty));
            }
        }

        sceneObstacles = list.ToArray();
    }

    private static void ResetVisual(Renderer r)
    {
        if (r == null) return;
        if (r is SpriteRenderer sr)
        {
            Color c = sr.color;
            if (c.a != 1.0f)
            {
                sr.color = new Color(c.r, c.g, c.b, 1.0f);
            }
        }
        else if (r is TilemapRenderer tmr)
        {
            Tilemap tm = tmr.GetComponent<Tilemap>();
            if (tm != null)
            {
                Color c = tm.color;
                if (c.a != 1.0f)
                {
                    tm.color = new Color(c.r, c.g, c.b, 1.0f);
                }
            }
        }
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
        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(activeScene) && !activeScene.Equals("Base", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (sceneObstacles == null || sceneObstacles.Length == 0)
        {
            if (Time.time >= nextCacheSearchTime)
            {
                nextCacheSearchTime = Time.time + 1.0f;
                FindAndCacheWorldObjects();
            }
            if (sceneObstacles == null || sceneObstacles.Length == 0) return;
        }

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
        float feetY = pPos.y - 0.42f;
        float pX = pPos.x;
        float pY = pPos.y;

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            ref ObstacleData obs = ref sceneObstacles[i];
            if (obs.renderer == null) continue;

            int targetOrder = 4;
            float targetAlpha = 1.0f;
            bool isBehind = false;

            if (obs.spriteRenderer != null)
            {
                if (Mathf.Abs(pX - obs.baseX) <= obs.halfWidth && feetY >= obs.baseY && pY <= obs.topY + 0.5f)
                {
                    isBehind = true;
                    targetOrder = 14;
                    targetAlpha = 0.45f;
                }

                // Optimization: if already settled at target alpha and target order, skip completely!
                if (!isBehind && obs.currentOrder == 4 && obs.currentAlpha >= 0.999f)
                {
                    continue;
                }

                UpdateSpriteObstacleVisual(ref obs, targetOrder, targetAlpha, isBehind);
            }
            else if (obs.tilemap != null)
            {
                if (obs.currentOrder != targetOrder)
                {
                    obs.renderer.sortingOrder = targetOrder;
                    obs.currentOrder = targetOrder;
                }
            }
        }
    }

    private static void UpdateSpriteObstacleVisual(ref ObstacleData obs, int targetOrder, float targetAlpha, bool isBehind)
    {
        SpriteRenderer sr = obs.spriteRenderer;
        if (sr == null) return;

        float curA = obs.currentAlpha;
        if (Mathf.Abs(curA - targetAlpha) > 0.01f)
        {
            float newA = Mathf.MoveTowards(curA, targetAlpha, Time.deltaTime * 6f);
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, newA);
            obs.currentAlpha = newA;
        }
        else if (curA != targetAlpha)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, targetAlpha);
            obs.currentAlpha = targetAlpha;
        }

        int desiredOrder = targetOrder;
        if (obs.currentOrder != desiredOrder)
        {
            obs.renderer.sortingOrder = desiredOrder;
            obs.currentOrder = desiredOrder;
        }
    }
}

