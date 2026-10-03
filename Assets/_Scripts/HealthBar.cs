using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private Vector3 offset = new(0, 0.9f, 0);
    public EnemyStats enemyTarget;
    public PlayerStats playerTarget;

    public void SetHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBar != null && maxHealth > 0)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    public void SetEnemyTarget(EnemyStats enemy)
    {
        enemyTarget = enemy;
        playerTarget = null;
        if (intentText != null)
        {
            intentText.gameObject.SetActive(true);
        }
    }

    public void SetPlayerTarget(PlayerStats player)
    {
        playerTarget = player;
        enemyTarget = null;
        if (intentText != null)
        {
            intentText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Enemy health bar: If target is null, dead (HP <= 0) or disabled, destroy health bar
        if (playerTarget == null)
        {
            if (enemyTarget == null || enemyTarget.currentHealth <= 0 || !enemyTarget.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (enemyTarget != null)
        {
            transform.position = enemyTarget.transform.position + offset;

            SetHealthBar(
                enemyTarget.currentHealth,
                enemyTarget.enemyData.maxHealth
            );

            if (intentText != null)
            {
                switch (enemyTarget.currentIntent)
                {
                    case EnemyData.EnemyIntent.Attack:
                        intentText.text = $"ATK {enemyTarget.GetCurrentATK()}";
                        break;
                    case EnemyData.EnemyIntent.Defend:
                        intentText.text = "DEFEND";
                        break;
                    case EnemyData.EnemyIntent.Buff:
                        intentText.text = "BUFF";
                        break;
                    case EnemyData.EnemyIntent.Debuff:
                        intentText.text = "DEBUFF";
                        break;
                }
            }
        }

        if (playerTarget != null)
        {
            if (playerTarget.currentHealth <= 0 || !playerTarget.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = playerTarget.transform.position + offset;

            SetHealthBar(
                playerTarget.currentHealth,
                playerTarget.maxHealth
            );
        }
    }
}