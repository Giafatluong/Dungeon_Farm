using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpeedEntry
{
    public GameObject character;
    public int speed;
}

public class SpeedManager : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private List<EnemyStats> enemies = new List<EnemyStats>();

    public List<SpeedEntry> speedOrder = new List<SpeedEntry>();

    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
    }

    public void SetEnemies(EnemyStats[] newEnemies)
    {
        enemies.Clear();
        if (newEnemies != null)
        {
            for (int i = 0; i < newEnemies.Length; i++)
            {
                if (newEnemies[i] != null)
                {
                    enemies.Add(newEnemies[i]);
                }
            }
        }
    }

    public void CreateSpeedOrder(bool isAmbush = false)
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
        speedOrder.Clear();

        if (isAmbush)
        {
            // In Ambush: Enemies act first sorted by Speed; Player acts last
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null) continue;
                if (enemies[i].currentHealth <= 0) continue;

                speedOrder.Add(new SpeedEntry
                {
                    character = enemies[i].gameObject,
                    speed = enemies[i].GetCurrentSpeed()
                });
            }

            SortEntries();
            RandomEqualSpeed();

            if (playerStats != null)
            {
                speedOrder.Add(new SpeedEntry
                {
                    character = playerStats.gameObject,
                    speed = playerStats.GetCurrentSpeed()
                });
            }
            return;
        }

        // Normal turn order: Add both Player and Enemies, then sort by Speed
        if (playerStats != null)
        {
            speedOrder.Add(new SpeedEntry
            {
                character = playerStats.gameObject,
                speed = playerStats.GetCurrentSpeed()
            });
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            if (enemies[i].currentHealth <= 0) continue;

            speedOrder.Add(new SpeedEntry
            {
                character = enemies[i].gameObject,
                speed = enemies[i].GetCurrentSpeed()
            });
        }

        SortEntries();
        RandomEqualSpeed();
    }

    private void SortEntries()
    {
        for (int i = 0; i < speedOrder.Count - 1; i++)
        {
            for (int j = i + 1; j < speedOrder.Count; j++)
            {
                if (speedOrder[j].speed > speedOrder[i].speed)
                {
                    SpeedEntry temp = speedOrder[i];
                    speedOrder[i] = speedOrder[j];
                    speedOrder[j] = temp;
                }
            }
        }
    }

    public void RandomEqualSpeed()
    {
        int start = 0;
        while (start < speedOrder.Count)
        {
            int end = start;
            while (end + 1 < speedOrder.Count && speedOrder[end + 1].speed == speedOrder[start].speed)
            {
                end++;
            }

            if (end > start)
            {
                for (int i = start; i <= end; i++)
                {
                    int randomIndex = Random.Range(start, end + 1);
                    SpeedEntry temp = speedOrder[i];
                    speedOrder[i] = speedOrder[randomIndex];
                    speedOrder[randomIndex] = temp;
                }
            }

            start = end + 1;
        }
    }
}