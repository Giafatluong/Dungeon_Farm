using UnityEngine;

public class EnemyTargetSelector : MonoBehaviour
{
    public EnemyStats selectedEnemy;

    public void SelectEnemy(EnemyStats enemy)
    {
        if (enemy == null) return;
        if (enemy.currentHealth <= 0) return;

        selectedEnemy = enemy;

        Debug.Log("Selected Enemy: " + enemy.enemyData.enemyName);
    }

    public void ClearTarget()
    {
        selectedEnemy = null;
    }

    public void CheckTarget()
    {
        if (selectedEnemy == null) return;

        if (selectedEnemy.currentHealth <= 0)
        {
            selectedEnemy = null;
        }
    }
}