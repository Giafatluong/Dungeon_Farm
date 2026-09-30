using UnityEngine;

public class Camp : MonoBehaviour
{
    public enum ActionType
    {
        Eat,
        Cook,
        Exercise,
        Continue,
        ReturnHome
    }

    [Header("Encounter Risk Rates")]
    public float encounterChance = 0f;
    public float eatRisk = 0.15f;
    public float cookRisk = 0.20f;
    public float exerciseRisk = 0.35f;
    public int exerciseHungerReduction = 25;

    [Header("Camp State")]
    public bool ambushState = false;
    public bool mustContinue = false; // Sau ambush thì bắt buộc phải Continue

    [Header("References")]
    public PlayerStats playerStats;
    public ItemContainer itemContainer;
    public WaveManager waveManager;

    public event System.Action<ActionType, float> OnCampAction;
    public event System.Action OnAmbushTriggered;

    public bool PerformAction(ActionType action, object parameter = null)
    {
        if (ambushState || mustContinue)
        {
            if (action != ActionType.Continue)
            {
                Debug.LogWarning("Đang bị phục kích hoặc bắt buộc phải Đi tiếp!");
                return false;
            }
        }

        switch (action)
        {
            case ActionType.Eat:
                if (parameter is FoodData food)
                {
                    if (playerStats != null)
                    {
                        playerStats.Eat(food);
                    }
                }
                else
                {
                    Debug.LogWarning("Chưa chọn món ăn để ăn tại Camp.");
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
                }
                else
                {
                    Debug.LogWarning("Chưa chọn công thức để nấu tại Camp.");
                    return false;
                }
                encounterChance += cookRisk;
                break;

            case ActionType.Exercise:
                if (playerStats != null)
                {
                    playerStats.ReduceHunger(exerciseHungerReduction);
                    Debug.Log("Tập thể dục tại Camp! Giảm " + exerciseHungerReduction + " Hunger.");
                }
                encounterChance += exerciseRisk;
                break;

            case ActionType.Continue:
                ContinueToNextStage();
                return true;

            case ActionType.ReturnHome:
                ReturnHome();
                return true;
        }

        encounterChance = Mathf.Clamp01(encounterChance);
        Debug.Log($"Hành động Camp {action} thành công. Tỷ lệ phục kích hiện tại: {encounterChance * 100}%");
        OnCampAction?.Invoke(action, encounterChance);

        // Kiểm tra phục kích (Ambush roll)
        CheckAmbushRoll();
        return true;
    }

    public void ReturnHome()
    {
        Debug.Log("Người chơi quyết định rút lui từ Camp về Base an toàn. Giữ lại 100% đồ trong ba lô!");
        encounterChance = 0f;
        ambushState = false;
        mustContinue = false;

        OnCampAction?.Invoke(ActionType.ReturnHome, 0f);

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteRun();
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    private void CheckAmbushRoll()
    {
        float roll = Random.value;
        if (roll < encounterChance)
        {
            TriggerAmbush();
        }
    }

    private void TriggerAmbush()
    {
        Debug.Log("CẢNH BÁO: Bị phục kích (Ambush) tại Camp!");
        ambushState = true;
        mustContinue = true;
        encounterChance = 0f;

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

        Debug.Log("Rời khỏi Camp, di chuyển sang Stage tiếp theo.");
        OnCampAction?.Invoke(ActionType.Continue, 0f);

        if (waveManager != null)
        {
            waveManager.Continue();
        }
    }
}
