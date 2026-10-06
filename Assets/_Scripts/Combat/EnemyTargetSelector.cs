using UnityEngine;

public class EnemyTargetSelector : MonoBehaviour
{
    public EnemyStats selectedEnemy;
    public event System.Action<EnemyStats> OnTargetChanged;

    public void SelectEnemy(EnemyStats enemy)
    {
        if (enemy == null) return;
        if (enemy.currentHealth <= 0) return;

        selectedEnemy = enemy;
        OnTargetChanged?.Invoke(selectedEnemy);

        Debug.Log("Selected Enemy: " + (enemy.enemyData != null ? enemy.enemyData.enemyName : enemy.name));
    }

    public void ClearTarget()
    {
        selectedEnemy = null;
        OnTargetChanged?.Invoke(null);
    }

    public void CheckTarget()
    {
        if (selectedEnemy == null) return;

        if (selectedEnemy.currentHealth <= 0)
        {
            selectedEnemy = null;
            OnTargetChanged?.Invoke(null);
        }
    }

    public void AutoSelectTarget(EnemyStats[] enemies)
    {
        if (selectedEnemy != null && selectedEnemy.currentHealth > 0) return;

        if (enemies == null) return;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].currentHealth > 0)
            {
                SelectEnemy(enemies[i]);
                return;
            }
        }

        ClearTarget();
    }

    public void CycleTarget(EnemyStats[] enemies, bool forward = true)
    {
        if (enemies == null || enemies.Length == 0) return;

        System.Collections.Generic.List<EnemyStats> aliveEnemies = new();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].currentHealth > 0)
            {
                aliveEnemies.Add(enemies[i]);
            }
        }

        if (aliveEnemies.Count == 0)
        {
            ClearTarget();
            return;
        }

        int curIdx = aliveEnemies.IndexOf(selectedEnemy);
        if (curIdx < 0)
        {
            SelectEnemy(aliveEnemies[0]);
            return;
        }

        int nextIdx = forward
            ? (curIdx + 1) % aliveEnemies.Count
            : (curIdx - 1 + aliveEnemies.Count) % aliveEnemies.Count;

        SelectEnemy(aliveEnemies[nextIdx]);
    }
}