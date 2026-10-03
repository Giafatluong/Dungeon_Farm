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
    }

    private void Update()
    {
        ClearHealthBars();
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