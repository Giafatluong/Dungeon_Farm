using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private CombatManager combatManager;

    private int currentIndex;

    public void StartRound()
    {
        speedManager.CreateSpeedOrder();
        currentIndex = 0;

        StartCurrentTurn();
    }

    public void StartCurrentTurn()
    {
        if (currentIndex >= speedManager.speedOrder.Count)
        {
            StartRound();
            return;
        }

        SpeedEntry currentEntry = speedManager.speedOrder[currentIndex];

        if (currentEntry.character == combatManager.playerStats.gameObject)
        {
            combatManager.StartPlayerTurn();
        }
        else
        {
            EnemyStats enemy = currentEntry.character.GetComponent<EnemyStats>();

            if (enemy == null)
            {
                NextTurn();
                return;
            }

            combatManager.StartEnemyTurn(enemy);
        }
    }

    public void NextTurn()
    {
        currentIndex++;

        if (currentIndex >= speedManager.speedOrder.Count)
        {
            StartRound();
            return;
        }

        StartCurrentTurn();
    }

    public void EndPlayerTurn()
    {
        NextTurn();
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public SpeedEntry GetCurrentEntry()
    {
        if (currentIndex >= speedManager.speedOrder.Count)
        {
            return null;
        }

        return speedManager.speedOrder[currentIndex];
    }
}