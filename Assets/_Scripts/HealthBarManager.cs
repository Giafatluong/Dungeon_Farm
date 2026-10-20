using UnityEngine;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{
    #region Inspector Fields & State
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarCanvas;
    [SerializeField] private PlayerStats playerStats;

    private EnemyStats[] enemies;
    private readonly List<HealthBar> healthBars = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (healthBarCanvas == null)
        {
            Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) healthBarCanvas = canvas.transform;
        }

        if (healthBarPrefab == null)
        {
            healthBarPrefab = Resources.Load<GameObject>("HealthBar");
#if UNITY_EDITOR
            if (healthBarPrefab == null)
            {
                healthBarPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Assets/_Prefabs/HealthBar.prefab");
            }
#endif
        }
    }

    private float nextCleanupTime = 0f;

    private void Update()
    {
        // Run cleanup periodically (once every 1 second) rather than every single frame
        if (Time.time >= nextCleanupTime)
        {
            nextCleanupTime = Time.time + 1.0f;
            if (healthBars.Count > 0)
            {
                ClearHealthBars();
            }
        }
    }
    #endregion

    #region Health Bar Creation
    public void SetEnemies(EnemyStats[] newEnemies)
    {
        enemies = newEnemies;
    }

    public void CreateHealthBars()
    {
        ClearAllHealthBars();
        EnsureReferences();

        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
        {
            SpawnHealthBar(playerStats);
        }

        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || enemies[i].currentHealth <= 0) continue;
            SpawnHealthBar(enemies[i]);
        }
    }

    private void SpawnHealthBar(PlayerStats player)
    {
        if (healthBarPrefab == null || healthBarCanvas == null) return;
        GameObject barObject = Instantiate(healthBarPrefab, healthBarCanvas);
        HealthBar bar = barObject.GetComponent<HealthBar>();
        if (bar != null)
        {
            bar.SetPlayerTarget(player);
            healthBars.Add(bar);
        }
    }

    private void SpawnHealthBar(EnemyStats enemy)
    {
        if (healthBarPrefab == null || healthBarCanvas == null) return;
        GameObject barObject = Instantiate(healthBarPrefab, healthBarCanvas);
        HealthBar bar = barObject.GetComponent<HealthBar>();
        if (bar != null)
        {
            bar.SetEnemyTarget(enemy);
            healthBars.Add(bar);
        }
    }
    #endregion

    #region Health Bar Cleanup & Management
    public void ClearHealthBars()
    {
        for (int i = healthBars.Count - 1; i >= 0; i--)
        {
            if (healthBars[i] == null)
            {
                healthBars.RemoveAt(i);
                continue;
            }

            // Enemy health bar validation
            if (healthBars[i].playerTarget == null)
            {
                if (healthBars[i].enemyTarget == null ||
                    healthBars[i].enemyTarget.currentHealth <= 0 ||
                    !healthBars[i].enemyTarget.gameObject.activeInHierarchy)
                {
                    Destroy(healthBars[i].gameObject);
                    healthBars.RemoveAt(i);
                    continue;
                }
            }
            // Player health bar validation
            else if (healthBars[i].playerTarget.currentHealth <= 0)
            {
                Destroy(healthBars[i].gameObject);
                healthBars.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Clear all enemy health bars (when wave is cleared or combat ends)
    /// </summary>
    public void ClearEnemyHealthBars()
    {
        for (int i = healthBars.Count - 1; i >= 0; i--)
        {
            if (healthBars[i] == null)
            {
                healthBars.RemoveAt(i);
                continue;
            }

            if (healthBars[i].playerTarget == null)
            {
                Destroy(healthBars[i].gameObject);
                healthBars.RemoveAt(i);
            }
        }

        if (healthBarCanvas != null)
        {
            HealthBar[] allBars = healthBarCanvas.GetComponentsInChildren<HealthBar>(true);
            foreach (var bar in allBars)
            {
                if (bar != null && bar.playerTarget == null)
                {
                    Destroy(bar.gameObject);
                }
            }
        }
    }

    public void ClearAllHealthBars()
    {
        for (int i = healthBars.Count - 1; i >= 0; i--)
        {
            if (healthBars[i] != null)
            {
                Destroy(healthBars[i].gameObject);
            }
        }

        healthBars.Clear();
    }
    #endregion
}