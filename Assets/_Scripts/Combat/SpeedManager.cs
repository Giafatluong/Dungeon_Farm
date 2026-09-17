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
    [SerializeField] private List<EnemyStats> enemies;

    public List<SpeedEntry> speedOrder = new List<SpeedEntry>();

    public void CreateSpeedOrder()
    {
        speedOrder.Clear();

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
}