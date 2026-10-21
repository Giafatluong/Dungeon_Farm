using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for high-impact game feel (Juice):
/// - Directional / Micro Camera Shake
/// - Hit-Stop / Micro Freeze Frames (crunchy impact weight)
/// - Wave Finisher Slow-Mo
/// - Procedural Impact Sparks / Burst VFX
/// Fully isolated and non-destructive: cannot break or overwrite existing mechanics.
/// </summary>
public class CombatJuice : MonoBehaviour
{
    private static CombatJuice instance;
    public static CombatJuice Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CombatJuice>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[CombatJuice]");
                    instance = go.AddComponent<CombatJuice>();
                }
            }
            return instance;
        }
    }

    [Header("Camera Shake Settings")]
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 lastAppliedShakeOffset = Vector3.zero;

    [Header("Hit Stop")]
    private Coroutine hitStopRoutine;
    private Coroutine slowMoRoutine;

    // Cache for procedural particle systems
    private readonly List<ParticleSystem> sparkPool = new();

    private Camera cachedCam;
    private Camera MainCam
    {
        get
        {
            if (cachedCam == null) cachedCam = Camera.main;
            return cachedCam;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnDisable()
    {
        // Safety: Always restore timeScale when disabled
        Time.timeScale = 1f;

        Camera cam = MainCam;
        if (cam != null && lastAppliedShakeOffset != Vector3.zero)
        {
            cam.transform.position -= lastAppliedShakeOffset;
            lastAppliedShakeOffset = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        // Zero overhead early exit when no shake is active or needs clearing
        if (shakeTimer <= 0f && lastAppliedShakeOffset == Vector3.zero) return;

        Camera cam = MainCam;
        if (cam == null) return;

        // Cleanly undo the previous frame's offset so base position is unaffected
        if (lastAppliedShakeOffset != Vector3.zero)
        {
            cam.transform.position -= lastAppliedShakeOffset;
            lastAppliedShakeOffset = Vector3.zero;
        }

        // Apply new shake if active
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float progress = 1f - Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
            float currentIntensity = Mathf.Lerp(shakeMagnitude, 0f, progress);

            Vector2 random2D = Random.insideUnitCircle * currentIntensity;
            Vector3 offset = new Vector3(random2D.x, random2D.y, 0f);

            cam.transform.position += offset;
            lastAppliedShakeOffset = offset;
        }
    }

    #region Camera Shake
    /// <summary>
    /// Triggers a non-destructive camera shake that dampens over time.
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;

        // If a weaker shake is running, override it with the stronger one
        if (shakeTimer <= 0f || intensity > shakeMagnitude)
        {
            shakeMagnitude = intensity;
            shakeDuration = duration;
            shakeTimer = duration;
        }
    }
    #endregion

    #region Hit-Stop / Freeze Frame
    /// <summary>
    /// Freezes or severely slows game time for a micro duration to give physical weight to impacts.
    /// </summary>
    public void HitStop(float durationRealtime, float frozenScale = 0.04f)
    {
        if (durationRealtime <= 0f) return;

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
        }
        hitStopRoutine = StartCoroutine(HitStopRoutine(durationRealtime, frozenScale));
    }

    private IEnumerator HitStopRoutine(float durationRealtime, float frozenScale)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp(frozenScale, 0.01f, 1f);

        yield return new WaitForSecondsRealtime(durationRealtime);

        Time.timeScale = 1.0f;
        hitStopRoutine = null;
    }
    #endregion

    #region Finisher Slow-Mo
    /// <summary>
    /// Cinematic slow-motion for wave / floor clears.
    /// </summary>
    public void PlayFinisherSlowMo(float slowScale = 0.25f, float durationRealtime = 0.45f)
    {
        if (slowMoRoutine != null)
        {
            StopCoroutine(slowMoRoutine);
        }
        slowMoRoutine = StartCoroutine(FinisherSlowMoRoutine(slowScale, durationRealtime));
    }

    private IEnumerator FinisherSlowMoRoutine(float slowScale, float durationRealtime)
    {
        Time.timeScale = slowScale;
        yield return new WaitForSecondsRealtime(durationRealtime);

        float elapsed = 0f;
        float returnDuration = 0.15f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(slowScale, 1.0f, elapsed / returnDuration);
            yield return null;
        }

        Time.timeScale = 1.0f;
        slowMoRoutine = null;
    }
    #endregion

    #region Procedural Impact Sparks
    /// <summary>
    /// Spawns high-energy impact sparks bursting outward from the hit point.
    /// </summary>
    public void PlayHitSparks(Vector3 worldPos, Color color, int count = 12)
    {
        ParticleSystem ps = GetOrCreateSparkSystem();
        if (ps == null) return;

        ps.transform.position = worldPos;

        var main = ps.main;
        main.startColor = color;

        ps.Emit(count);
    }

    private ParticleSystem GetOrCreateSparkSystem()
    {
        for (int i = 0; i < sparkPool.Count; i++)
        {
            if (sparkPool[i] != null && !sparkPool[i].isPlaying)
            {
                return sparkPool[i];
            }
        }

        // Create new procedural particle system
        GameObject go = new GameObject("ProceduralSparks");
        go.transform.SetParent(transform);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 30; // Above sprites and UI
        }

        sparkPool.Add(ps);
        return ps;
    }
    #endregion

    #region High-Level Hooks
    /// <summary>
    /// Central trigger called whenever an enemy receives damage.
    /// Adapts shake, hit-stop, and spark colors according to damage tier.
    /// </summary>
    public void OnEnemyDamaged(Vector3 hitPos, int damage, float damagePercent, bool isLethal)
    {
        if (isLethal || damagePercent >= 1.0f)
        {
            // Tier 3: Lethal Execution / Overkill
            Shake(0.28f, 0.18f);
            HitStop(0.065f, 0.02f);
            PlayHitSparks(hitPos, new Color(1f, 0.22f, 0.22f, 1f), 16);
        }
        else if (damagePercent >= 0.5f)
        {
            // Tier 2: Heavy Hit (50% - 99% HP)
            Shake(0.14f, 0.12f);
            HitStop(0.038f, 0.05f);
            PlayHitSparks(hitPos, new Color(1f, 0.62f, 0.15f, 1f), 12);
        }
        else
        {
            // Tier 1: Light / Normal Hit (< 50% HP)
            Shake(0.05f, 0.08f);
            PlayHitSparks(hitPos, new Color(0.96f, 0.96f, 0.96f, 1f), 8);
        }
    }

    /// <summary>
    /// Central trigger called when the player receives damage from an enemy.
    /// </summary>
    public void OnPlayerDamaged(Vector3 playerPos, int damage)
    {
        Shake(0.18f, 0.14f);
        HitStop(0.035f, 0.08f);
        PlayHitSparks(playerPos, new Color(1f, 0.25f, 0.25f, 1f), 12);
    }
    #endregion
}
