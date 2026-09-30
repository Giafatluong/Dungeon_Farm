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
        if (healthBar != null)
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
                        intentText.text = $"⚔️ {enemyTarget.GetCurrentATK()}";
                        break;
                    case EnemyData.EnemyIntent.Defend:
                        intentText.text = "🛡️ Defend";
                        break;
                    case EnemyData.EnemyIntent.Buff:
                        intentText.text = "✨ Buff";
                        break;
                    case EnemyData.EnemyIntent.Debuff:
                        intentText.text = "💀 Debuff";
                        break;
                }
            }
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