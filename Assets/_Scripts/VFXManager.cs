using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for all game visual effects (VFX):
/// 1. Sword slash blade arcs (Slash Blade Arc VFX) in combat
/// 2. Footstep dust VFX when moving
/// 3. Ambient falling leaves VFX at Farm Base
/// 4. Harvest sparkles VFX when harvesting crops
/// 5. Planting dust poof VFX when sowing seeds
/// 6. Item pickup flash when loot enters backpack
/// 7. Cooking sparkles VFX at Kitchen
/// </summary>
public class VFXManager : MonoBehaviour
{
    private static VFXManager _instance;
    public static VFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VFXManager>();
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("[VFXManager]");
                    _instance = go.AddComponent<VFXManager>();
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance != null)
        {
            Instance.EnsureInitialized();
        }
    }

    #region References & Slices
    private Sprite[] _p1VfxSprites; // Tier 1: Silver blade slash (P1_VFX - 15 frames)
    private Sprite[] _p2VfxSprites; // Tier 2: Golden greatsword cleave (P2_VFX - 13 frames)
    private Sprite[] _p3VfxSprites; // Tier 3: Fiery blaze strike (P3_VFX - 18 frames)
    private Sprite _leafSprite;

    private readonly List<GameObject> _slashPool = new();
    private const int SLASH_POOL_SIZE = 5;

    // Particle Systems
    private ParticleSystem _footstepDustSystem;
    private ParticleSystem _harvestSparklesSystem;
    private ParticleSystem _plantingDustSystem;
    private ParticleSystem _pickupSparklesSystem;
    private ParticleSystem _cookingSparklesSystem;
    private ParticleSystem _ambientLeavesSystem;

    private bool _initialized = false;
    private Camera _cachedMainCam;
    private Camera MainCam
    {
        get
        {
            if (_cachedMainCam == null) _cachedMainCam = Camera.main;
            return _cachedMainCam;
        }
    }

    private static readonly Dictionary<float, WaitForSeconds> _waitMap = new();
    private static WaitForSeconds GetCachedWait(float time)
    {
        if (!_waitMap.TryGetValue(time, out var wait))
        {
            wait = new WaitForSeconds(time);
            _waitMap[time] = wait;
        }
        return wait;
    }
    #endregion

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInitialized();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cachedMainCam = null;
        UpdateAmbientLeavesState(scene.name);
    }

    private void LateUpdate()
    {
        // At Base, keep leaf particles positioned above camera so leaves always fall in view
        if (_ambientLeavesSystem != null && _ambientLeavesSystem.isPlaying)
        {
            try
            {
                Camera mainCam = MainCam;
                if (mainCam != null)
                {
                    Vector3 camPos = mainCam.transform.position;
                    _ambientLeavesSystem.transform.position = new Vector3(camPos.x, camPos.y + 6.5f, 0f);
                }
            }
            catch (System.Exception)
            {
                // Safety guard against internal ParticleSystem state race
            }
        }
    }

    public void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        LoadSprites();
        BuildSlashPool();
        BuildParticleSystems();
        UpdateAmbientLeavesState(SceneManager.GetActiveScene().name);
    }

    #region Asset Loading
    private void LoadSprites()
    {
        // 1. Load 3 attack animation VFX sets from Fantasy Adventure Pack
        _p1VfxSprites = LoadVfxGroup("Assets/Fantasy Adventure Pack/Sprites/Player Sprites/Player1/P1_VFX.png", "P1_VFX_");
        _p2VfxSprites = LoadVfxGroup("Assets/Fantasy Adventure Pack/Sprites/Player Sprites/Player2/P2_VFX.png", "P2_VFX_");
        _p3VfxSprites = LoadVfxGroup("Assets/Fantasy Adventure Pack/Sprites/Player Sprites/Player3/P3_VFX.png", "P3_VFX_");

        // 2. Load Oak_Leaf_Particle sprite (Kenmi Pack)
#if UNITY_EDITOR
        string leafPath = "Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Trees/Oak_Leaf_Particle.png";
        _leafSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(leafPath);
#endif
        if (_leafSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < allSprites.Length; i++)
            {
                if (allSprites[i] != null && allSprites[i].name.Contains("Leaf_Particle"))
                {
                    _leafSprite = allSprites[i];
                    break;
                }
            }
        }
    }

    private static Sprite[] LoadVfxGroup(string assetPath, string prefix)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> list = new();
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite s && s.name.StartsWith(prefix))
                {
                    list.Add(s);
                }
            }
        }
        list.Sort((a, b) => ExtractPrefixIndex(a.name, prefix).CompareTo(ExtractPrefixIndex(b.name, prefix)));
        if (list.Count > 0) return list.ToArray();
#endif
        Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
        List<Sprite> fallback = new();
        for (int i = 0; i < loaded.Length; i++)
        {
            if (loaded[i] != null && loaded[i].name.StartsWith(prefix))
            {
                fallback.Add(loaded[i]);
            }
        }
        fallback.Sort((a, b) => ExtractPrefixIndex(a.name, prefix).CompareTo(ExtractPrefixIndex(b.name, prefix)));
        return fallback.ToArray();
    }

    private static int ExtractPrefixIndex(string name, string prefix)
    {
        string numPart = name.Replace(prefix, "");
        int.TryParse(numPart, out int idx);
        return idx;
    }

    private static Sprite[] SliceSubArray(Sprite[] source, int start, int count)
    {
        if (source == null || start >= source.Length) return new Sprite[0];
        int actualCount = Mathf.Min(count, source.Length - start);
        Sprite[] sub = new Sprite[actualCount];
        System.Array.Copy(source, start, sub, 0, actualCount);
        return sub;
    }
    #endregion

    #region Slash VFX Pool & Execution
    private void BuildSlashPool()
    {
        for (int i = 0; i < SLASH_POOL_SIZE; i++)
        {
            GameObject go = new GameObject($"SlashVFX_Pool_{i}");
            go.transform.SetParent(transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 35; // Above monsters and player
            go.SetActive(false);

            _slashPool.Add(go);
        }
    }

    /// <summary>
    /// Triggers attack VFX customized by damage tier and color:
    /// - Tier 1 (< 15 damage): P1_VFX (agile silver slash), bright silver-white
    /// - Tier 2 (15 - 29 damage): P2_VFX (heavy golden cleave), amber-gold
    /// - Tier 3 (>= 30 damage or crit): P3_VFX (blazing execution slash), fiery red-orange
    /// - Poison / Decay: P1_VFX poison purple
    /// </summary>
    public void PlayAttackAnimationByDamage(Vector3 worldPos, int damage, bool isCrit = false, bool isPoison = false)
    {
        EnsureInitialized();

        Sprite[] frames;
        Color vfxColor;
        float scale;
        float frameTime;

        if (isPoison)
        {
            frames = (_p1VfxSprites != null && _p1VfxSprites.Length > 0) ? _p1VfxSprites : _p2VfxSprites;
            vfxColor = new Color(0.72f, 0.35f, 0.95f, 1f); // Toxic purple
            scale = 1.35f;
            frameTime = 0.035f;
        }
        else if (isCrit || damage >= 30)
        {
            // Tier 3: Blazing crit slash (P3_VFX), fiery red
            frames = (_p3VfxSprites != null && _p3VfxSprites.Length > 0) ? _p3VfxSprites : (_p2VfxSprites ?? _p1VfxSprites);
            vfxColor = new Color(1f, 0.28f, 0.16f, 1f); // Fiery red-orange
            scale = 1.95f;
            frameTime = 0.032f;
        }
        else if (damage >= 15)
        {
            // Tier 2: Golden greatsword cleave (P2_VFX), amber-gold
            frames = (_p2VfxSprites != null && _p2VfxSprites.Length > 0) ? _p2VfxSprites : (_p1VfxSprites ?? _p3VfxSprites);
            vfxColor = new Color(1f, 0.78f, 0.18f, 1f); // Radiant gold
            scale = 1.55f;
            frameTime = 0.038f;
        }
        else
        {
            // Tier 1: Basic silver blade slash (P1_VFX), bright silver-white
            frames = (_p1VfxSprites != null && _p1VfxSprites.Length > 0) ? _p1VfxSprites : _p2VfxSprites;
            vfxColor = new Color(0.95f, 0.98f, 1.0f, 1f); // Silver white
            scale = 1.20f;
            frameTime = 0.034f;
        }

        if (frames == null || frames.Length == 0) return;

        GameObject slashObj = GetAvailableSlashObject();
        if (slashObj == null) return;

        slashObj.SetActive(true);
        StartCoroutine(AnimateSlashRoutine(slashObj, frames, worldPos, vfxColor, scale, frameTime));
    }

    public void PlaySlashVFX(Vector3 worldPos, bool isCrit = false, float scale = 1.35f)
    {
        PlayAttackAnimationByDamage(worldPos, isCrit ? 35 : 10, isCrit, false);
    }

    private GameObject GetAvailableSlashObject()
    {
        for (int i = 0; i < _slashPool.Count; i++)
        {
            if (_slashPool[i] != null && !_slashPool[i].activeSelf)
            {
                return _slashPool[i];
            }
        }

        // Expand pool if necessary
        GameObject go = new GameObject($"SlashVFX_Pool_{_slashPool.Count}");
        go.transform.SetParent(transform);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 35;
        go.SetActive(false);
        _slashPool.Add(go);
        return go;
    }

    private IEnumerator AnimateSlashRoutine(GameObject slashObj, Sprite[] frames, Vector3 worldPos, Color color, float scale, float frameTime)
    {
        SpriteRenderer sr = slashObj.GetComponent<SpriteRenderer>();
        Transform t = slashObj.transform;

        Vector2 randomJitter = Random.insideUnitCircle * 0.12f;
        t.position = worldPos + new Vector3(randomJitter.x, randomJitter.y, 0f);

        float randomAngle = Random.Range(-15f, 15f);
        t.rotation = Quaternion.Euler(0f, 0f, randomAngle);
        t.localScale = new Vector3(scale, scale, 1f);

        sr.color = color;

        WaitForSeconds waitFrame = GetCachedWait(frameTime);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                sr.sprite = frames[i];
            }
            yield return waitFrame;
        }

        sr.sprite = null;
        slashObj.SetActive(false);
    }
    #endregion

    #region Particle Systems Construction
    private void BuildParticleSystems()
    {
        _footstepDustSystem = CreateParticleSystem("VFX_FootstepDust", 5);
        ConfigureFootstepDust(_footstepDustSystem);

        _harvestSparklesSystem = CreateParticleSystem("VFX_HarvestSparkles", 30);
        ConfigureHarvestSparkles(_harvestSparklesSystem);

        _plantingDustSystem = CreateParticleSystem("VFX_PlantingDust", 15);
        ConfigurePlantingDust(_plantingDustSystem);

        _pickupSparklesSystem = CreateParticleSystem("VFX_PickupSparkles", 30);
        ConfigurePickupSparkles(_pickupSparklesSystem);

        _cookingSparklesSystem = CreateParticleSystem("VFX_CookingSparkles", 30);
        ConfigureCookingSparkles(_cookingSparklesSystem);

        _ambientLeavesSystem = CreateParticleSystem("VFX_AmbientLeaves", 25);
        ConfigureAmbientLeaves(_ambientLeavesSystem);
    }

    private ParticleSystem CreateParticleSystem(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;
        }

        return ps;
    }

    private void ConfigureFootstepDust(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
        main.startColor = new Color(0.72f, 0.62f, 0.48f, 0.65f); // Light brown dirt dust

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.78f, 0.68f, 0.52f), 0f), new GradientColorKey(new Color(0.65f, 0.55f, 0.40f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.75f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.6f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private void ConfigureHarvestSparkles(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.gravityModifier = 0.85f; // Gently falling mini firework sparkles

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.15f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.90f, 0.25f), 0f), new GradientColorKey(new Color(0.45f, 0.95f, 0.35f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1.2f);
        curve.AddKey(0.8f, 0.8f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private void ConfigurePlantingDust(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
        main.startColor = new Color(0.40f, 0.28f, 0.16f, 0.85f); // Dark rich soil

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.45f, 0.30f, 0.18f), 0f), new GradientColorKey(new Color(0.35f, 0.22f, 0.12f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(0.2f, 1.0f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private void ConfigurePickupSparkles(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.75f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private void ConfigureCookingSparkles(ParticleSystem ps)
    {
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.50f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
        main.gravityModifier = 0.4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60f;
        shape.radius = 0.25f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.65f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.45f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1.2f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private void ConfigureAmbientLeaves(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7.0f, 10.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.30f, 0.45f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 2.2f; // ~2 leaves/sec gentle rate

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 1.2f, 1f); // Spans entire camera width

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.85f, -1.35f); // Drift downward gently
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f); // Sync TwoConstants mode for all 3 axes

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotation.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotation.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        // Fade in when spawned, fade out before disappearing
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        // Assign leaf sprite if available
        if (_leafSprite != null && _leafSprite.texture != null)
        {
            // Setup TextureSheetAnimation with Sprite mode so Particle automatically detects frame
            var texSheet = ps.textureSheetAnimation;
            texSheet.enabled = true;
            texSheet.mode = ParticleSystemAnimationMode.Sprites;
            texSheet.SetSprite(0, _leafSprite);

            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit")
                        ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                        ?? Shader.Find("Sprites/Default");
                if (s != null)
                {
                    Material mat = new Material(s);

                    // Configure transparent render mode for URP
                    mat.SetFloat("_Surface", 1f); // 1 = Transparent
                    mat.SetFloat("_Blend", 0f);   // 0 = Alpha blend
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                    // Enable Alpha Clipping to remove black borders
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.SetFloat("_Cutoff", 0.1f);
                    mat.EnableKeyword("_ALPHATEST_ON");

                    // Assign texture to both URP BaseMap and standard MainTex
                    mat.SetTexture("_BaseMap", _leafSprite.texture);
                    mat.SetTexture("_MainTex", _leafSprite.texture);
                    mat.mainTexture = _leafSprite.texture;

                    rend.material = mat;
                }
            }
        }
    }
    #endregion

    #region Public Triggers
    /// <summary>
    /// Player running dust effect (disabled by design).
    /// </summary>
    public void PlayFootstepDust(Vector3 footPos)
    {
        // Player running VFX disabled
    }

    /// <summary>
    /// Sparkles burst when harvesting ripe crops.
    /// </summary>
    public void PlayHarvestSparkles(Vector3 plotPos)
    {
        if (_harvestSparklesSystem == null) return;
        _harvestSparklesSystem.transform.position = plotPos + Vector3.up * 0.35f;
        _harvestSparklesSystem.Emit(16);
    }

    /// <summary>
    /// Small dust poof when planting seeds in soil.
    /// </summary>
    public void PlayPlantingDust(Vector3 plotPos)
    {
        if (_plantingDustSystem == null) return;
        _plantingDustSystem.transform.position = plotPos;
        _plantingDustSystem.Emit(9);
    }

    /// <summary>
    /// Golden flash when collecting loot into backpack.
    /// </summary>
    public void PlayItemPickup(Vector3 itemPos)
    {
        if (_pickupSparklesSystem == null) return;
        _pickupSparklesSystem.transform.position = itemPos;
        _pickupSparklesSystem.Emit(8);
    }

    /// <summary>
    /// Mini sparkle firework when cooking a dish successfully.
    /// </summary>
    public void PlayCookingSparkles(Vector3 stovePos)
    {
        if (_cookingSparklesSystem == null) return;
        _cookingSparklesSystem.transform.position = stovePos;
        _cookingSparklesSystem.Emit(22);
    }

    /// <summary>
    /// Enables/disables ambient leaf particles depending on scene (active only in Base).
    /// </summary>
    public void UpdateAmbientLeavesState(string sceneName)
    {
        if (_ambientLeavesSystem == null) return;

        try
        {
            bool isBase = sceneName.Equals("Base", System.StringComparison.OrdinalIgnoreCase);
            if (isBase)
            {
                if (!_ambientLeavesSystem.isPlaying)
                {
                    _ambientLeavesSystem.Play();
                }
            }
            else
            {
                if (_ambientLeavesSystem.isPlaying)
                {
                    _ambientLeavesSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[VFXManager] UpdateAmbientLeavesState warning: {ex.Message}");
        }
    }
    #endregion
}
