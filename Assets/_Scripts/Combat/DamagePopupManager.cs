using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Colors")]
    public Color playerDamageColor = new Color(1f, 0.3f, 0.3f, 1f);      // Crimson red
    public Color enemyDamageColor = new Color(1f, 0.65f, 0.15f, 1f);     // Vibrant orange
    public Color critDamageColor = new Color(1f, 0.85f, 0.2f, 1f);       // Golden yellow
    public Color healColor = new Color(0.35f, 0.95f, 0.55f, 1f);          // Bright green
    public Color blockColor = new Color(0.4f, 0.75f, 1f, 1f);            // Ice cyan

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spawns a floating damage popup above a target.
    /// </summary>
    public static void ShowDamage(Vector3 worldPos, int amount, bool isPlayer = false, bool isCrit = false)
    {
        if (amount <= 0)
        {
            ShowText(worldPos, "BLOCKED", Instance != null ? Instance.blockColor : new Color(0.4f, 0.75f, 1f), 4.2f);
            return;
        }

        Color color;
        if (isPlayer)
        {
            color = Instance != null ? Instance.playerDamageColor : new Color(1f, 0.3f, 0.3f);
        }
        else if (isCrit)
        {
            color = Instance != null ? Instance.critDamageColor : new Color(1f, 0.85f, 0.2f);
        }
        else
        {
            color = Instance != null ? Instance.enemyDamageColor : new Color(1f, 0.65f, 0.15f);
        }

        string text = isCrit ? $"-{amount}!" : $"-{amount}";
        float size = isCrit ? 5.8f : (isPlayer ? 5.2f : 4.8f);

        DamagePopup.Create(worldPos, text, color, size, isCrit);
    }

    /// <summary>
    /// Spawns a floating heal popup.
    /// </summary>
    public static void ShowHeal(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;
        Color color = Instance != null ? Instance.healColor : new Color(0.35f, 0.95f, 0.55f);
        DamagePopup.Create(worldPos, $"+{amount}", color, 4.8f, false);
    }

    /// <summary>
    /// Spawns a floating text popup with custom string and color.
    /// </summary>
    public static void ShowText(Vector3 worldPos, string text, Color color, float size = 4.5f)
    {
        DamagePopup.Create(worldPos, text, color, size, false);
    }
}
