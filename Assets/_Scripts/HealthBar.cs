using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private Vector3 offset = new(0, 0.9f, 0);
    public EnemyStats enemyTarget;
    public PlayerStats playerTarget;

    public void SetHealthBar(int currentHealth, int maxHealth)
    {
        healthBar.fillAmount = (float)currentHealth / maxHealth;
    }

    public void SetEnemyTarget(EnemyStats enemy)
    {
        enemyTarget = enemy;
        playerTarget = null;
    }

    public void SetPlayerTarget(PlayerStats player)
    {
        playerTarget = player;
        enemyTarget = null;
    }

    private void Update()
    {
        if (enemyTarget != null)
        {
            transform.position = enemyTarget.transform.position + offset;

            SetHealthBar(
                enemyTarget.currentHealth,
                enemyTarget.enemyData.maxHealth
            );
        }

        if (playerTarget != null)
        {
            transform.position = playerTarget.transform.position + offset;

            SetHealthBar(
                playerTarget.currentHealth,
                playerTarget.maxHealth
            );
        }
    }
}