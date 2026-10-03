using UnityEngine;

public class Camp : MonoBehaviour
{
    public enum ActionType
    {
        Eat,
        Cook,
        Exercise,
        Rest,
        Continue,
        ReturnHome
    }

    [Header("Encounter Risk Rates")]
    public float encounterChance = 0f;
    public float eatRisk = 0.15f;
    public float cookRisk = 0.20f;
    public float exerciseRisk = 0.35f;
    public float restRisk = 0.10f;

    [Header("Values")]
    public int exerciseHungerReduction = 25;
    public int restHealAmount = 25;

    [Header("Camp State")]
    public bool isCampOpen = false;
    public bool ambushState = false;
    public bool mustContinue = false; // After ambush, player must continue

    [Header("References")]
    public PlayerStats playerStats;
    public ItemContainer itemContainer;
    public WaveManager waveManager;

    public event System.Action OnCampOpened;
    public event System.Action OnCampClosed;
    public event System.Action<ActionType, float> OnCampAction;
    public event System.Action OnAmbushTriggered;

    public void OpenCamp(PlayerStats pStats, ItemContainer container, WaveManager wManager)
    {
        playerStats = pStats;
        itemContainer = container;
        waveManager = wManager;

        encounterChance = 0f;
        ambushState = false;
        mustContinue = false;
        isCampOpen = true;

        gameObject.SetActive(true);
        Debug.Log("[Camp] Safe camp opened!");
        OnCampOpened?.Invoke();
    }

    public void CloseCamp()
    {
        isCampOpen = false;
        OnCampClosed?.Invoke();
    }

    public bool PerformAction(ActionType action, object parameter = null)
    {
        if (ambushState || mustContinue)
        {
            if (action != ActionType.Continue)
            {
                Debug.LogWarning("[Camp] Ambushed or must continue forward!");
                return false;
            }
        }

        switch (action)
        {
            case ActionType.Eat:
                if (parameter is FoodData food)
                {
                    if (playerStats == null || itemContainer == null) return false;

                    if (!itemContainer.HasItem(food))
                    {
                        Debug.LogWarning("[Camp] Food not found in backpack!");
                        return false;
                    }

                    if (playerStats.currentHunger >= playerStats.maxHunger)
                    {
                        Debug.LogWarning("[Camp] Player fullness is at max!");
                        return false;
                    }

                    playerStats.Eat(food);
                }
                else
                {
                    Debug.LogWarning("[Camp] No food selected to eat.");
                    return false;
                }
                encounterChance += eatRisk;
                break;

            case ActionType.Cook:
                if (parameter is RecipeData recipe)
                {
                    if (CookingManager.Instance != null && itemContainer != null)
                    {
                        bool success = CookingManager.Instance.Cook(recipe, itemContainer);
                        if (!success) return false;
                    }
                    else
                    {
                        Debug.LogWarning("[Camp] CookingManager not found or backpack empty!");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning("[Camp] No recipe selected to cook.");
                    return false;
                }
                encounterChance += cookRisk;
                break;

            case ActionType.Exercise:
                if (playerStats != null)
                {
                    if (playerStats.currentHunger <= 0)
                    {
                        Debug.LogWarning("[Camp] Fullness is already 0, cannot exercise!");
                        return false;
                    }
                    playerStats.ReduceHunger(exerciseHungerReduction);
                    Debug.Log($"[Camp] Exercised! Reduced {exerciseHungerReduction} fullness.");
                }
                encounterChance += exerciseRisk;
                break;

            case ActionType.Rest:
                if (playerStats != null)
                {
                    if (playerStats.currentHealth >= playerStats.maxHealth)
                    {
                        Debug.LogWarning("[Camp] Health is already full!");
                        return false;
                    }
                    playerStats.Heal(restHealAmount);
                    Debug.Log($"[Camp] Rested by campfire! Recovered {restHealAmount} HP.");
                }
                encounterChance += restRisk;
                break;

            case ActionType.Continue:
                ContinueToNextStage();
                return true;

            case ActionType.ReturnHome:
                ReturnHome();
                return true;
        }

        encounterChance = Mathf.Clamp01(encounterChance);
        Debug.Log($"[Camp] Action {action} succeeded. Current ambush risk: {encounterChance * 100:F0}%");
        OnCampAction?.Invoke(action, encounterChance);

        // Ambush roll
        CheckAmbushRoll();
        return true;
    }

    public void ReturnHome()
    {
        Debug.Log("[Camp] Returning safely to Base. Keeping 100% loot!");
        encounterChance = 0f;
        ambushState = false;
        mustContinue = false;

        CloseCamp();
        OnCampAction?.Invoke(ActionType.ReturnHome, 0f);

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteRun();
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    private void CheckAmbushRoll()
    {
        float roll = Random.value;
        Debug.Log($"[Camp Roll] Roll: {roll:F2} vs Ambush Risk: {encounterChance:F2}");
        if (roll < encounterChance)
        {
            TriggerAmbush();
        }
    }

    private void TriggerAmbush()
    {
        Debug.Log("[Camp] WARNING: Ambushed at Camp!");
        ambushState = true;
        mustContinue = true;
        encounterChance = 0f;
        isCampOpen = false;

        OnAmbushTriggered?.Invoke();

        if (waveManager != null)
        {
            waveManager.TriggerAmbush();
        }
    }

    public void ContinueToNextStage()
    {
        encounterChance = 0f;
        ambushState = false;
        mustContinue = false;

        CloseCamp();
        Debug.Log("[Camp] Leaving Camp, proceeding to next stage.");
        OnCampAction?.Invoke(ActionType.Continue, 0f);

        if (waveManager != null)
        {
            waveManager.Continue();
        }
    }
}
