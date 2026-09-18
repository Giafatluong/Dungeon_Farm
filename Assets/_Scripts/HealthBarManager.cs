using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

public class HealthBarManager : MonoBehaviour
{
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Transform healthBarCanvas;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EnemyStats[] enemies;

    private List<HealthBar> healthBars = new();

    private void Update()
    {
        ClearHealthBars();
    }

    public void CreateHealthBars()
    {
        ClearHealthBars();

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

    public void ClearHealthBars()
    {
        for (int i = healthBars.Count - 1; i >= 0; i--)
        {
            if (healthBars[i] == null) continue;

            if (healthBars[i].enemyTarget != null && healthBars[i].enemyTarget.currentHealth <= 0)
            {
                Destroy(healthBars[i].gameObject);
                healthBars.RemoveAt(i);
                continue;
            }

            if (healthBars[i].playerTarget != null && healthBars[i].playerTarget.currentHealth <= 0)
            {
                Destroy(healthBars[i].gameObject);
                healthBars.RemoveAt(i);
            }
        }
    }
}