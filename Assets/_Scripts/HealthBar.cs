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

    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private EnemyData.EnemyIntent? lastIntent = null;
    private int lastAtk = -1;

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
        lastHealth = -1;
        lastMaxHealth = -1;
        lastIntent = null;
        lastAtk = -1;

        if (intentText != null)
        {
            intentText.gameObject.SetActive(true);
        }
    }

    public void SetPlayerTarget(PlayerStats player)
    {
        playerTarget = player;
        enemyTarget = null;
        lastHealth = -1;
        lastMaxHealth = -1;
        lastIntent = null;
        lastAtk = -1;

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

            int curHp = enemyTarget.currentHealth;
            int maxHp = enemyTarget.enemyData != null ? enemyTarget.enemyData.maxHealth : 1;

            if (curHp != lastHealth || maxHp != lastMaxHealth)
            {
                lastHealth = curHp;
                lastMaxHealth = maxHp;
                SetHealthBar(curHp, maxHp);
            }

            if (intentText != null)
            {
                int curAtk = enemyTarget.GetCurrentATK();
                EnemyData.EnemyIntent curIntent = enemyTarget.currentIntent;

                if (curIntent != lastIntent || curAtk != lastAtk)
                {
                    lastIntent = curIntent;
                    lastAtk = curAtk;

                    switch (curIntent)
                    {
                        case EnemyData.EnemyIntent.Attack:
                            intentText.text = $"<color=#FF5555>ATK {curAtk}</color>";
                            break;
                        case EnemyData.EnemyIntent.Defend:
                            intentText.text = "<color=#55AAFF>DEFEND</color>";
                            break;
                        case EnemyData.EnemyIntent.Buff:
                            intentText.text = "<color=#FFDD44>BUFF</color>";
                            break;
                        case EnemyData.EnemyIntent.Debuff:
                            intentText.text = "<color=#DD55FF>DEBUFF</color>";
                            break;
                        default:
                            intentText.text = $"<color=#FF5555>ATK {curAtk}</color>";
                            break;
                    }
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

            int curHp = playerTarget.currentHealth;
            int maxHp = playerTarget.maxHealth;

            if (curHp != lastHealth || maxHp != lastMaxHealth)
            {
                lastHealth = curHp;
                lastMaxHealth = maxHp;
                SetHealthBar(curHp, maxHp);
            }
        }
    }
}