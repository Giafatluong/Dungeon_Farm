using UnityEngine;
using System.Collections.Generic;

public class HealthBarManager : MonoBehaviour
{
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarCanvas;
    [SerializeField] private PlayerStats playerStats;

    private EnemyStats[] enemies;
    private List<HealthBar> healthBars = new();

    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
    }

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
            GameObject healthBarObject = Instantiate(
                healthBarPrefab,
                healthBarCanvas
            );

            HealthBar healthBar = healthBarObject.GetComponent<HealthBar>();

            if (healthBar != null)
            {
                healthBar.SetPlayerTarget(playerStats);
                healthBars.Add(healthBar);
            }
        }

        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            if (enemies[i].currentHealth <= 0) continue;

            GameObject healthBarObject = Instantiate(
                healthBarPrefab,
                healthBarCanvas
            );

            HealthBar healthBar = healthBarObject.GetComponent<HealthBar>();

            if (healthBar != null)
            {
                healthBar.SetEnemyTarget(enemies[i]);
                healthBars.Add(healthBar);
            }
        }
    }

    private void Update()
    {
        ClearHealthBars();
    }

    public void ClearHealthBars()
    {
        for (int i = healthBars.Count - 1; i >= 0; i--)
        {
            if (healthBars[i] == null)
            {
                healthBars.RemoveAt(i);
                continue;
            }

            // Enemy health bar
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
            // Player health bar
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
}