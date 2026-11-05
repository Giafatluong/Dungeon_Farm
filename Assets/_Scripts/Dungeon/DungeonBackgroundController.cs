using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the multi-layered parallax scrolling background and solid ground in the Dungeon scene.
/// Features:
/// - Solid platformer ground beneath player & enemy feet (similar to Mario/platformers),
///   filling the lower screen completely with subterranean earth/stone strata.
/// - 3 Parallax Layers (Back, Mid, Overhead Front) with micro-overlap to guarantee zero seams/gaps.
/// - Dynamic segment count calculation based on camera aspect ratio and sprite dimensions.
/// - Strict ordered queue wrapping: segments remain perfectly contiguous and seamless forever.
/// - Synchronized walking with PlayerCombatVisual and WaveManager transition states.
/// - Floor themes: Floor 1 (Ancient Dungeon), Floor 2 (Swamp), Floor 3 (Demon Citadel).
/// </summary>
public class DungeonBackgroundController : MonoBehaviour
{
    public static DungeonBackgroundController Instance { get; private set; }

    [System.Serializable]
    public class BackgroundTheme
    {
        public int floorNumber = 1;
        public string themeName = "Ancient Dungeon";
        public Sprite backgroundSprite;
        public Sprite midgroundSprite;
        public Sprite foregroundSprite;
        public Sprite customGroundSprite;
        public Color backgroundColor = new Color(0.92f, 0.92f, 0.95f, 1f);
        public Color midgroundColor = Color.white;
        public Color foregroundColor = Color.white;

        [Header("Ground Palette")]
        public Color groundHighlightColor = new Color(0.58f, 0.64f, 0.74f, 1f);
        public Color groundBaseColor = new Color(0.20f, 0.22f, 0.29f, 1f);
        public Color groundShadowColor = new Color(0.09f, 0.09f, 0.13f, 1f);
    }

    [System.Serializable]
    public class ParallaxLayer
    {
        public string name = "Layer";
        public Sprite sprite;
        public Color tint = Color.white;
        [Range(0f, 2f)] public float speedMultiplier = 1f;
        public float yOffset = 0f;
        public int sortingOrder = -10;
        public Vector2 pivot = new Vector2(0.5f, 0.5f);

        [HideInInspector] public GameObject container;
        [HideInInspector] public List<SpriteRenderer> segments = new List<SpriteRenderer>();
        [HideInInspector] public float segmentWidth = 8f;
        [HideInInspector] public int segmentCount = 6;
    }

    [Header("Floor Themes")]
    [SerializeField] private List<BackgroundTheme> themes = new List<BackgroundTheme>();
    [SerializeField] private int currentFloor = 1;

    [Header("Parallax Settings")]
    [SerializeField] private float baseMoveSpeed = 4.5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float baseScale = 2.7f;
    [Tooltip("Micro-overlap between adjacent tiles (in world units) to prevent 1-pixel subpixel rasterization gaps.")]
    [SerializeField] private float overlapAmount = 0.04f;

    [Header("Ground Floor (Platform Surface & Underground Strata)")]
    [Tooltip("Y position of the top walking surface (where player and enemies stand).")]
    [SerializeField] private float groundSurfaceY = -0.5625f;

    [SerializeField]
    private ParallaxLayer groundLayer = new ParallaxLayer
    {
        name = "GroundLayer",
        speedMultiplier = 1.0f,
        yOffset = -0.5625f,
        sortingOrder = -2,
        pivot = new Vector2(0.5f, 1.0f),
        tint = Color.white
    };

    [Header("Background & Overhead Layers Configuration")]
    [SerializeField]
    private ParallaxLayer backgroundLayer = new ParallaxLayer
    {
        name = "BackLayer",
        speedMultiplier = 0.22f,
        yOffset = 0.5f,
        sortingOrder = -30,
        pivot = new Vector2(0.5f, 0.5f),
        tint = new Color(0.88f, 0.88f, 0.95f, 1f)
    };

    [SerializeField]
    private ParallaxLayer midgroundLayer = new ParallaxLayer
    {
        name = "MidLayer",
        speedMultiplier = 0.52f,
        yOffset = -0.2f,
        sortingOrder = -20,
        pivot = new Vector2(0.5f, 0.5f),
        tint = Color.white
    };

    [SerializeField]
    private ParallaxLayer foregroundLayer = new ParallaxLayer
    {
        name = "FrontLayer",
        speedMultiplier = 1.0f,
        yOffset = 0.85f,
        sortingOrder = -10,
        pivot = new Vector2(0.5f, 0.5f),
        tint = Color.white
    };

    private readonly List<ParallaxLayer> allLayers = new List<ParallaxLayer>();
    private float currentScrollSpeed = 0f;
    private bool isPlayerMoving = false;
    private bool isForcedMoving = false;
    private Camera targetCamera;
    private PlayerCombatVisual cachedPlayerVisual;
    private float lastCameraWidth = 0f;
    private readonly Dictionary<int, Sprite> runtimeGeneratedGroundSprites = new Dictionary<int, Sprite>();

    public float CurrentScrollSpeed => currentScrollSpeed;
    public bool IsMoving => isPlayerMoving || isForcedMoving || currentScrollSpeed > 0.05f;

    public static DungeonBackgroundController EnsureInstance()
    {
        if (Instance != null) return Instance;

        DungeonBackgroundController existing = FindFirstObjectByType<DungeonBackgroundController>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("[DungeonBackgroundController]");
        go.transform.position = Vector3.zero;
        Instance = go.AddComponent<DungeonBackgroundController>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        targetCamera = Camera.main;
        if (targetCamera == null) targetCamera = FindFirstObjectByType<Camera>();

        InitializeDefaultThemesIfEmpty();
        SetupLayersList();

        int initialFloor = 1;
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.currentFloor > 0)
        {
            initialFloor = ProgressionManager.Instance.currentFloor;
        }
        SetFloorTheme(initialFloor);

        // Subscribe to transition state events
        PlayerCombatVisual.OnMoveTransitionStateChanged += HandlePlayerMoveTransitionChanged;
    }

    private void OnDestroy()
    {
        PlayerCombatVisual.OnMoveTransitionStateChanged -= HandlePlayerMoveTransitionChanged;
        if (Instance == this) Instance = null;
    }

    private void HandlePlayerMoveTransitionChanged(bool moving)
    {
        isPlayerMoving = moving;
    }

    private void SetupLayersList()
    {
        allLayers.Clear();
        allLayers.Add(backgroundLayer);
        allLayers.Add(midgroundLayer);
        allLayers.Add(foregroundLayer);
        allLayers.Add(groundLayer);
    }

    private void InitializeDefaultThemesIfEmpty()
    {
        if (themes != null && themes.Count > 0) return;

        themes = new List<BackgroundTheme>();

        Sprite[] allSprites = GameAssetHelper.LoadAll<Sprite>();
        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
        if (allSprites != null)
        {
            for (int i = 0; i < allSprites.Length; i++)
            {
                if (allSprites[i] != null && !spriteMap.ContainsKey(allSprites[i].name))
                {
                    spriteMap[allSprites[i].name] = allSprites[i];
                }
            }
        }

        // Floor 1 Theme: Ancient Dungeon (E1 - Catacombs & Ancient Stone)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 1,
            themeName = "Ancient Dungeon",
            backgroundSprite = spriteMap.GetValueOrDefault("E1_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E1_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E1_FrontLayer"),
            customGroundSprite = spriteMap.GetValueOrDefault("DungeonGround_Floor1"),
            backgroundColor = new Color(0.85f, 0.88f, 0.95f, 1f),
            midgroundColor = Color.white,
            foregroundColor = Color.white,
            groundHighlightColor = new Color(0.62f, 0.68f, 0.80f, 1f),
            groundBaseColor = new Color(0.22f, 0.24f, 0.32f, 1f),
            groundShadowColor = new Color(0.09f, 0.09f, 0.13f, 1f)
        });

        // Floor 2 Theme: Monster Swamp (E2 - Murky Peat & Overgrown Stone)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 2,
            themeName = "Monster Swamp",
            backgroundSprite = spriteMap.GetValueOrDefault("E2_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E2_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E2_FrontLayer"),
            customGroundSprite = spriteMap.GetValueOrDefault("DungeonGround_Floor2"),
            backgroundColor = new Color(0.82f, 0.95f, 0.85f, 1f),
            midgroundColor = Color.white,
            foregroundColor = Color.white,
            groundHighlightColor = new Color(0.52f, 0.70f, 0.45f, 1f),
            groundBaseColor = new Color(0.18f, 0.26f, 0.19f, 1f),
            groundShadowColor = new Color(0.08f, 0.12f, 0.08f, 1f)
        });

        // Floor 3 Theme: Demon Citadel (E3 - Basalt Pavement & Volcanic Strata)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 3,
            themeName = "Demon Citadel",
            backgroundSprite = spriteMap.GetValueOrDefault("E3_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E3_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E3_FrontLayer"),
            customGroundSprite = spriteMap.GetValueOrDefault("DungeonGround_Floor3"),
            backgroundColor = new Color(0.95f, 0.85f, 0.85f, 1f),
            midgroundColor = Color.white,
            foregroundColor = Color.white,
            groundHighlightColor = new Color(0.78f, 0.42f, 0.36f, 1f),
            groundBaseColor = new Color(0.26f, 0.17f, 0.20f, 1f),
            groundShadowColor = new Color(0.11f, 0.06f, 0.08f, 1f)
        });

        // Floor 4 Theme: Magma Caverns (E3 - Fiery Molten Glow)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 4,
            themeName = "Magma Caverns",
            backgroundSprite = spriteMap.GetValueOrDefault("E3_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E3_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E3_FrontLayer"),
            backgroundColor = new Color(1.0f, 0.65f, 0.45f, 1f),
            midgroundColor = new Color(1.0f, 0.80f, 0.60f, 1f),
            foregroundColor = new Color(1.0f, 0.70f, 0.50f, 1f),
            groundHighlightColor = new Color(0.92f, 0.52f, 0.22f, 1f),
            groundBaseColor = new Color(0.32f, 0.16f, 0.12f, 1f),
            groundShadowColor = new Color(0.14f, 0.06f, 0.05f, 1f)
        });

        // Floor 5 Theme: Frost Catacombs (E1 - Frozen Glacial Stone)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 5,
            themeName = "Frost Catacombs",
            backgroundSprite = spriteMap.GetValueOrDefault("E1_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E1_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E1_FrontLayer"),
            backgroundColor = new Color(0.65f, 0.88f, 1.0f, 1f),
            midgroundColor = new Color(0.78f, 0.92f, 1.0f, 1f),
            foregroundColor = new Color(0.85f, 0.95f, 1.0f, 1f),
            groundHighlightColor = new Color(0.60f, 0.85f, 0.98f, 1f),
            groundBaseColor = new Color(0.18f, 0.28f, 0.38f, 1f),
            groundShadowColor = new Color(0.08f, 0.12f, 0.18f, 1f)
        });

        // Floor 6 Theme: Toxic Spore Hollow (E2 - Toxic Emerald Gloom)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 6,
            themeName = "Toxic Spore Hollow",
            backgroundSprite = spriteMap.GetValueOrDefault("E2_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E2_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E2_FrontLayer"),
            backgroundColor = new Color(0.55f, 0.95f, 0.65f, 1f),
            midgroundColor = new Color(0.75f, 1.0f, 0.75f, 1f),
            foregroundColor = new Color(0.70f, 0.95f, 0.70f, 1f),
            groundHighlightColor = new Color(0.40f, 0.85f, 0.45f, 1f),
            groundBaseColor = new Color(0.14f, 0.30f, 0.16f, 1f),
            groundShadowColor = new Color(0.06f, 0.14f, 0.07f, 1f)
        });

        // Floor 7 Theme: Bloodfang Lair (E3 - Crimson Bloodstone)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 7,
            themeName = "Bloodfang Lair",
            backgroundSprite = spriteMap.GetValueOrDefault("E3_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E3_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E3_FrontLayer"),
            backgroundColor = new Color(0.95f, 0.45f, 0.45f, 1f),
            midgroundColor = new Color(1.0f, 0.60f, 0.60f, 1f),
            foregroundColor = new Color(1.0f, 0.50f, 0.50f, 1f),
            groundHighlightColor = new Color(0.85f, 0.30f, 0.30f, 1f),
            groundBaseColor = new Color(0.32f, 0.10f, 0.12f, 1f),
            groundShadowColor = new Color(0.14f, 0.04f, 0.05f, 1f)
        });

        // Floor 8 Theme: Ruins of the Void (E1 - Twilight Violet Marble)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 8,
            themeName = "Ruins of the Void",
            backgroundSprite = spriteMap.GetValueOrDefault("E1_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E1_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E1_FrontLayer"),
            backgroundColor = new Color(0.75f, 0.60f, 0.95f, 1f),
            midgroundColor = new Color(0.85f, 0.75f, 1.0f, 1f),
            foregroundColor = new Color(0.80f, 0.70f, 0.95f, 1f),
            groundHighlightColor = new Color(0.68f, 0.48f, 0.90f, 1f),
            groundBaseColor = new Color(0.24f, 0.16f, 0.34f, 1f),
            groundShadowColor = new Color(0.10f, 0.06f, 0.15f, 1f)
        });

        // Floor 9 Theme: Abyssal Depths (E2 - Deep Subterranean Trench)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 9,
            themeName = "Abyssal Depths",
            backgroundSprite = spriteMap.GetValueOrDefault("E2_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E2_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E2_FrontLayer"),
            backgroundColor = new Color(0.40f, 0.60f, 0.85f, 1f),
            midgroundColor = new Color(0.50f, 0.75f, 0.95f, 1f),
            foregroundColor = new Color(0.45f, 0.65f, 0.85f, 1f),
            groundHighlightColor = new Color(0.35f, 0.60f, 0.80f, 1f),
            groundBaseColor = new Color(0.12f, 0.20f, 0.30f, 1f),
            groundShadowColor = new Color(0.05f, 0.08f, 0.14f, 1f)
        });

        // Floor 10 Theme: The Eternal Throne (E3 - Royal Obsidian & Radiant Gold)
        themes.Add(new BackgroundTheme
        {
            floorNumber = 10,
            themeName = "The Eternal Throne",
            backgroundSprite = spriteMap.GetValueOrDefault("E3_BackgroundLayer"),
            midgroundSprite = spriteMap.GetValueOrDefault("E3_MidLayer"),
            foregroundSprite = spriteMap.GetValueOrDefault("E3_FrontLayer"),
            backgroundColor = new Color(1.0f, 0.88f, 0.45f, 1f),
            midgroundColor = new Color(1.0f, 0.92f, 0.65f, 1f),
            foregroundColor = new Color(1.0f, 0.85f, 0.55f, 1f),
            groundHighlightColor = new Color(1.0f, 0.82f, 0.30f, 1f),
            groundBaseColor = new Color(0.28f, 0.22f, 0.14f, 1f),
            groundShadowColor = new Color(0.12f, 0.09f, 0.05f, 1f)
        });
    }

    public void SetFloorTheme(int floorNum)
    {
        currentFloor = floorNum;
        BackgroundTheme selectedTheme = GetThemeForFloor(floorNum);
        if (selectedTheme == null) return;

        backgroundLayer.sprite = selectedTheme.backgroundSprite;
        backgroundLayer.tint = selectedTheme.backgroundColor;

        midgroundLayer.sprite = selectedTheme.midgroundSprite;
        midgroundLayer.tint = selectedTheme.midgroundColor;

        foregroundLayer.sprite = selectedTheme.foregroundSprite;
        foregroundLayer.tint = selectedTheme.foregroundColor;

        // Ground sprite: use custom if provided, otherwise find or generate themed procedural pixel-art ground
        Sprite groundSpr = selectedTheme.customGroundSprite;
        if (groundSpr == null)
        {
            Sprite[] allGrounds = GameAssetHelper.LoadAll<Sprite>();
            if (allGrounds != null)
            {
                string targetName = $"DungeonGround_Floor{floorNum}";
                for (int i = 0; i < allGrounds.Length; i++)
                {
                    if (allGrounds[i] != null && allGrounds[i].name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        groundSpr = allGrounds[i];
                        break;
                    }
                }
            }

            if (groundSpr == null)
            {
                if (!runtimeGeneratedGroundSprites.TryGetValue(floorNum, out groundSpr) || groundSpr == null)
                {
                    groundSpr = GenerateGroundSprite(selectedTheme);
                    runtimeGeneratedGroundSprites[floorNum] = groundSpr;
                }
            }
        }
        groundLayer.sprite = groundSpr;
        groundLayer.yOffset = groundSurfaceY;

        RebuildAllLayers();
    }

    private BackgroundTheme GetThemeForFloor(int floorNum)
    {
        if (themes == null || themes.Count == 0) return null;

        for (int i = 0; i < themes.Count; i++)
        {
            if (themes[i] != null && themes[i].floorNumber == floorNum)
            {
                return themes[i];
            }
        }

        int cycleIndex = Mathf.Abs((floorNum - 1) % themes.Count);
        return themes[cycleIndex];
    }

    /// <summary>
    /// Generates a high-quality 256x480 pixel art ground block with a crisp surface curb/flagstone
    /// where player and enemy boots stand, and rich subterranean bedrock filling all the way down.
    /// </summary>
    private Sprite GenerateGroundSprite(BackgroundTheme theme)
    {
        int width = 256;
        int height = 480;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color highlight = theme != null ? theme.groundHighlightColor : new Color(0.60f, 0.66f, 0.76f, 1f);
        Color baseCol = theme != null ? theme.groundBaseColor : new Color(0.22f, 0.24f, 0.32f, 1f);
        Color shadow = theme != null ? theme.groundShadowColor : new Color(0.09f, 0.09f, 0.13f, 1f);
        Color mortar = Color.Lerp(shadow, Color.black, 0.45f);

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1);
            Color rowBase = Color.Lerp(shadow, baseCol, Mathf.Pow(t, 0.72f));

            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;

                // 1. Top Rim / Walking Surface (top 42 pixels: y from 438 to 479)
                if (y >= 475)
                {
                    // Bright upper highlight edge right under player's feet
                    pixels[idx] = highlight;
                }
                else if (y >= 440)
                {
                    // Paved flagstone curb blocks
                    bool isMortarX = (x % 32 == 0);
                    if (isMortarX)
                    {
                        pixels[idx] = mortar;
                    }
                    else
                    {
                        float stoneShade = ((x + y * 3) % 7 == 0) ? 0.04f : 0f;
                        pixels[idx] = Color.Lerp(baseCol, highlight, 0.45f + stoneShade);
                    }
                }
                else if (y >= 436)
                {
                    // Dark mortar groove beneath top curb
                    pixels[idx] = mortar;
                }
                else
                {
                    // 2. Subterranean Earth & Stone Masonry (y < 436)
                    int row = y / 28;
                    int offsetX = (row % 2 == 0) ? 0 : 24;
                    bool isJointX = ((x + offsetX) % 48 == 0);
                    bool isJointY = (y % 28 == 0);

                    if (isJointX || isJointY)
                    {
                        pixels[idx] = Color.Lerp(mortar, shadow, 0.5f);
                    }
                    else
                    {
                        float speckle = ((x * 17 + y * 31) % 11 == 0) ? 0.03f : (((x * 13 + y * 7) % 13 == 0) ? -0.03f : 0f);
                        pixels[idx] = new Color(
                            Mathf.Clamp01(rowBase.r + speckle),
                            Mathf.Clamp01(rowBase.g + speckle),
                            Mathf.Clamp01(rowBase.b + speckle),
                            1f
                        );
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        // Pivot at Top-Center (0.5f, 1.0f) so the surface anchors exactly at groundSurfaceY
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 1.0f), 100f);
    }

    /// <summary>
    /// Rebuilds all layer segment objects and places them seamlessly edge-to-edge.
    /// </summary>
    public void RebuildAllLayers()
    {
        if (targetCamera == null) targetCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        float camWidth = 18f;
        if (targetCamera != null)
        {
            camWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
        }
        lastCameraWidth = camWidth;

        foreach (ParallaxLayer layer in allLayers)
        {
            BuildLayerSegments(layer, camWidth);
        }
    }

    private void BuildLayerSegments(ParallaxLayer layer, float camWidth)
    {
        if (layer == null) return;

        if (layer.container == null)
        {
            Transform existing = transform.Find(layer.name);
            if (existing != null)
            {
                layer.container = existing.gameObject;
            }
            else
            {
                layer.container = new GameObject(layer.name);
                layer.container.transform.SetParent(transform, false);
            }
        }

        layer.container.transform.localPosition = new Vector3(0f, layer.yOffset, 0f);

        // Ground layer uses scale 1.0 (since it's already 4.8 units tall), others use baseScale
        float scale = (layer == groundLayer) ? 1.0f : baseScale;
        float nativeWidth = (layer.sprite != null) ? layer.sprite.bounds.size.x : 1.6f;

        // Check if container already has segments present in the scene
        if (layer.segments.Count == 0)
        {
            List<SpriteRenderer> existingRenderers = new List<SpriteRenderer>();
            foreach (Transform child in layer.container.transform)
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) existingRenderers.Add(sr);
            }

            if (existingRenderers.Count > 0)
            {
                layer.segments.AddRange(existingRenderers);
                layer.segments.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));

                if (layer.sprite == null && existingRenderers[0].sprite != null)
                {
                    nativeWidth = existingRenderers[0].sprite.bounds.size.x;
                }
                layer.segmentWidth = nativeWidth * scale;
                layer.segmentCount = layer.segments.Count;

                // Sync sprites & materials & tints
                for (int i = 0; i < layer.segments.Count; i++)
                {
                    if (layer.sprite != null) layer.segments[i].sprite = layer.sprite;
                    layer.segments[i].color = layer.tint;
                    layer.segments[i].sortingOrder = layer.sortingOrder;
                }
                return;
            }
        }
        else
        {
            // Update existing segment visuals
            layer.segmentWidth = nativeWidth * scale;

            for (int i = 0; i < layer.segments.Count; i++)
            {
                if (layer.segments[i] != null)
                {
                    if (layer.sprite != null) layer.segments[i].sprite = layer.sprite;
                    layer.segments[i].color = layer.tint;
                    layer.segments[i].sortingOrder = layer.sortingOrder;
                }
            }
            return;
        }

        // Destroy old segment GameObjects if any
        for (int i = 0; i < layer.segments.Count; i++)
        {
            if (layer.segments[i] != null)
            {
                Destroy(layer.segments[i].gameObject);
            }
        }
        layer.segments.Clear();

        if (layer.sprite == null) return;
        float pieceWidth = nativeWidth * scale;
        if (pieceWidth < 0.2f) pieceWidth = 4.32f;
        layer.segmentWidth = pieceWidth;

        // Step distance with micro-overlap to eliminate any subpixel gap
        float stepX = Mathf.Max(0.1f, pieceWidth - overlapAmount);

        // Required span: covers entire camera view plus large safety buffer on both sides
        float spanNeeded = camWidth * 2.8f;
        int count = Mathf.CeilToInt(spanNeeded / stepX);
        if (count < 6) count = 6;
        if (count % 2 == 0) count++;
        layer.segmentCount = count;

        int half = count / 2;
        float startX = -half * stepX;

        for (int i = 0; i < count; i++)
        {
            GameObject segGo = new GameObject($"{layer.name}_Seg_{i}");
            segGo.transform.SetParent(layer.container.transform, false);

            float posX = startX + i * stepX;
            segGo.transform.localPosition = new Vector3(posX, 0f, 0f);
            segGo.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = segGo.AddComponent<SpriteRenderer>();
            sr.sprite = layer.sprite;
            sr.color = layer.tint;
            sr.sortingOrder = layer.sortingOrder;

            layer.segments.Add(sr);
        }
    }

    private void Update()
    {
        // Detect camera resize / resolution changes
        if (targetCamera != null)
        {
            float curCamWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
            if (Mathf.Abs(curCamWidth - lastCameraWidth) > 0.5f)
            {
                RebuildAllLayers();
            }
        }

        // Check moving condition
        bool movingNow = isForcedMoving || isPlayerMoving;

        // Fallback polling if event wasn't captured
        if (!movingNow)
        {
            if (cachedPlayerVisual == null) cachedPlayerVisual = FindFirstObjectByType<PlayerCombatVisual>();
            if (cachedPlayerVisual != null && cachedPlayerVisual.IsMovingTransition)
            {
                movingNow = true;
            }
            else if (WaveManager.Instance != null && WaveManager.Instance.IsTransitioning)
            {
                movingNow = true;
            }
        }

        // Smooth acceleration & deceleration
        float targetSpeed = movingNow ? baseMoveSpeed : 0f;
        float rate = movingNow ? acceleration : deceleration;
        currentScrollSpeed = Mathf.MoveTowards(currentScrollSpeed, targetSpeed, rate * Time.deltaTime);

        // Move background & ground layers backward (to the left)
        if (currentScrollSpeed > 0.0001f)
        {
            ScrollLayers(currentScrollSpeed * Time.deltaTime);
        }
    }

    private void ScrollLayers(float distance)
    {
        float camLeft = -9f;
        if (targetCamera != null)
        {
            float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            camLeft = targetCamera.transform.position.x - camHalfWidth;
        }

        foreach (ParallaxLayer layer in allLayers)
        {
            if (layer.segments == null || layer.segments.Count == 0) continue;

            float stepX = Mathf.Max(0.1f, layer.segmentWidth - overlapAmount);
            float layerMove = distance * layer.speedMultiplier;

            // 1. Shift all segments to the left
            for (int i = 0; i < layer.segments.Count; i++)
            {
                SpriteRenderer sr = layer.segments[i];
                if (sr == null) continue;

                Vector3 pos = sr.transform.localPosition;
                pos.x -= layerMove;
                sr.transform.localPosition = pos;
            }

            // 2. Wrap leftmost segments to the right
            float leftThreshold = camLeft - (layer.segmentWidth * 0.6f);

            while (layer.segments.Count > 1 && layer.segments[0] != null &&
                   (layer.segments[0].transform.localPosition.x + layer.segmentWidth * 0.5f) < leftThreshold)
            {
                SpriteRenderer leftmost = layer.segments[0];
                SpriteRenderer rightmost = layer.segments[layer.segments.Count - 1];

                Vector3 newPos = leftmost.transform.localPosition;
                newPos.x = rightmost.transform.localPosition.x + stepX;
                leftmost.transform.localPosition = newPos;

                layer.segments.RemoveAt(0);
                layer.segments.Add(leftmost);
            }
        }
    }

    /// <summary>
    /// Programmatically triggers background and ground movement for a specified duration.
    /// </summary>
    public void TriggerMove(float duration)
    {
        StartCoroutine(ForcedMoveRoutine(duration));
    }

    private System.Collections.IEnumerator ForcedMoveRoutine(float duration)
    {
        isForcedMoving = true;
        yield return new WaitForSeconds(duration);
        isForcedMoving = false;
    }
}
