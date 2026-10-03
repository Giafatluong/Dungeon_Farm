using UnityEngine;
using System.Collections.Generic;

public class FloorGenerator : MonoBehaviour
{
    [Header("Generator Configuration")]
    public int totalStages = 8;
    public int minCombatCount = 3;
    public int maxConsecutiveReward = 1;
    public int maxConsecutiveCamp = 1;

    [Header("Score Balance (Combat=+1, Event=-1, Reward=-2, Camp=0)")]
    public int combatScore = 1;
    public int eventScore = -1;
    public int rewardScore = -2;
    public int campScore = 0;

    public int minTargetScore = 0;
    public int maxTargetScore = 4;

    [Header("Wave Templates Pool")]
    public WaveData[] combatWavePool;
    public WaveData[] campWavePool;
    public WaveData[] eventWavePool;
    public WaveData[] rewardWavePool;
    public WaveData bossWave;

    /// <summary>
    /// Returns the score delta for a given wave type according to GDD:
    /// Combat = +1, Event = -1, Reward = -2, Camp = 0.
    /// </summary>
    public int GetScoreForWaveType(WaveData.WaveType type)
    {
        switch (type)
        {
            case WaveData.WaveType.Combat: return combatScore;
            case WaveData.WaveType.Event: return eventScore;
            case WaveData.WaveType.Reward: return rewardScore;
            case WaveData.WaveType.Camp: return campScore;
            default: return 0;
        }
    }

    /// <summary>
    /// When entering a Random Wave (1 single wave), this calculates the current floor score
    /// and dynamically selects a balanced wave type (Combat, Camp, Event, Reward) matching the score rules:
    /// - Total projected score within [minScore, maxScore]
    /// - Consecutive Reward <= maxConsecutiveReward (1)
    /// - Consecutive Camp <= maxConsecutiveCamp (1)
    /// </summary>
    public WaveData.WaveType DetermineRandomWaveType(
        int currentScore,
        WaveData.WaveType previousType,
        int minScore,
        int maxScore)
    {
        List<WaveData.WaveType> allCandidates = new List<WaveData.WaveType>
        {
            WaveData.WaveType.Combat,
            WaveData.WaveType.Event
        };

        // Constraint: no consecutive Reward
        if (previousType != WaveData.WaveType.Reward)
        {
            allCandidates.Add(WaveData.WaveType.Reward);
        }

        // Constraint: no consecutive Camp
        if (previousType != WaveData.WaveType.Camp)
        {
            allCandidates.Add(WaveData.WaveType.Camp);
        }

        // Filter candidates whose projected score falls within [minScore, maxScore]
        List<WaveData.WaveType> validCandidates = new List<WaveData.WaveType>();
        foreach (var candidate in allCandidates)
        {
            int projected = currentScore + GetScoreForWaveType(candidate);
            if (projected >= minScore && projected <= maxScore)
            {
                validCandidates.Add(candidate);
            }
        }

        if (validCandidates.Count > 0)
        {
            WaveData.WaveType chosen = validCandidates[Random.Range(0, validCandidates.Count)];
            Debug.Log($"[FloorGenerator] Random Wave calculated: currentScore={currentScore}, valid candidates=[{string.Join(", ", validCandidates)}], chosen={chosen} (newScore={currentScore + GetScoreForWaveType(chosen)})");
            return chosen;
        }

        // If no candidate falls exactly within range, pick the candidate that gets closest to [minScore, maxScore]
        WaveData.WaveType bestType = allCandidates[0];
        int bestDistance = int.MaxValue;
        foreach (var candidate in allCandidates)
        {
            int projected = currentScore + GetScoreForWaveType(candidate);
            int dist = 0;
            if (projected < minScore) dist = minScore - projected;
            else if (projected > maxScore) dist = projected - maxScore;

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestType = candidate;
            }
        }

        Debug.Log($"[FloorGenerator] Random Wave (closest balance): currentScore={currentScore}, chosen={bestType} (projected={currentScore + GetScoreForWaveType(bestType)})");
        return bestType;
    }

    /// <summary>
    /// Generates a full procedurally balanced floor ending with a Boss stage.
    /// </summary>
    public WaveData[] GenerateFloor()
    {
        int maxAttempts = 200;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<WaveData.WaveType> stageTypes = new List<WaveData.WaveType>();

            int currentScore = 0;
            int combatCount = 0;
            int consecutiveReward = 0;
            int consecutiveCamp = 0;
            bool valid = true;

            // Generate stages from 0 to totalStages - 2
            for (int i = 0; i < totalStages - 1; i++)
            {
                List<WaveData.WaveType> allowedTypes = new List<WaveData.WaveType> { WaveData.WaveType.Combat, WaveData.WaveType.Event };

                if (consecutiveReward < maxConsecutiveReward)
                {
                    allowedTypes.Add(WaveData.WaveType.Reward);
                }

                if (consecutiveCamp < maxConsecutiveCamp)
                {
                    allowedTypes.Add(WaveData.WaveType.Camp);
                }

                WaveData.WaveType chosen = allowedTypes[Random.Range(0, allowedTypes.Count)];
                stageTypes.Add(chosen);

                if (chosen == WaveData.WaveType.Combat)
                {
                    combatCount++;
                    currentScore += combatScore;
                    consecutiveReward = 0;
                    consecutiveCamp = 0;
                }
                else if (chosen == WaveData.WaveType.Reward)
                {
                    currentScore += rewardScore;
                    consecutiveReward++;
                    consecutiveCamp = 0;
                }
                else if (chosen == WaveData.WaveType.Camp)
                {
                    currentScore += campScore;
                    consecutiveCamp++;
                    consecutiveReward = 0;
                }
                else if (chosen == WaveData.WaveType.Event)
                {
                    currentScore += eventScore;
                    consecutiveReward = 0;
                    consecutiveCamp = 0;
                }
            }

            // The final stage is always the Boss stage
            stageTypes.Add(WaveData.WaveType.Boss);

            // Check constraints
            if (combatCount < minCombatCount) valid = false;
            if (currentScore < minTargetScore || currentScore > maxTargetScore) valid = false;

            if (valid)
            {
                Debug.Log($"[FloorGenerator] Floor generation successful on attempt {attempt + 1}. Total score: {currentScore}, Combat stages: {combatCount}");
                return BuildWavesFromTypesWithFallbacks(stageTypes);
            }
        }

        Debug.LogWarning("[FloorGenerator] Could not find ideal floor configuration within attempt limit. Using fallback floor.");
        return BuildFallbackFloor();
    }

    public WaveData[] BuildWavesFromTypesWithFallbacks(
        List<WaveData.WaveType> types,
        WaveData sourceWave = null,
        FloorData activeFloor = null)
    {
        WaveData[] result = new WaveData[types.Count];

        for (int i = 0; i < types.Count; i++)
        {
            WaveData.WaveType type = types[i];
            result[i] = ResolveWaveForType(type, sourceWave, activeFloor);
        }

        return result;
    }

    private WaveData ResolveWaveForType(
        WaveData.WaveType type,
        WaveData sourceWave = null,
        FloorData activeFloor = null)
    {
        WaveData resolved = null;

        switch (type)
        {
            case WaveData.WaveType.Combat:
                resolved = GetRandomFromPool(combatWavePool);
                if (resolved == null && activeFloor != null && activeFloor.waves != null)
                {
                    resolved = FindWaveOfType(activeFloor.waves, WaveData.WaveType.Combat);
                }
                if (resolved == null && sourceWave != null && HasEnemies(sourceWave))
                {
                    resolved = CloneWaveWithType(sourceWave, WaveData.WaveType.Combat, "Random Area Combat");
                }
                if (resolved == null)
                {
                    resolved = CreateDefaultWave(WaveData.WaveType.Combat, "Default Combat Stage", sourceWave);
                }
                break;

            case WaveData.WaveType.Camp:
                resolved = GetRandomFromPool(campWavePool);
                if (resolved == null && activeFloor != null && activeFloor.waves != null)
                {
                    resolved = FindWaveOfType(activeFloor.waves, WaveData.WaveType.Camp);
                }
                if (resolved == null)
                {
                    resolved = CreateDefaultWave(WaveData.WaveType.Camp, "Camp Rest Stage", sourceWave);
                }
                break;

            case WaveData.WaveType.Reward:
                resolved = GetRandomFromPool(rewardWavePool);
                if (resolved == null && activeFloor != null && activeFloor.waves != null)
                {
                    resolved = FindWaveOfType(activeFloor.waves, WaveData.WaveType.Reward);
                }
                if (resolved == null)
                {
                    resolved = CreateDefaultWave(WaveData.WaveType.Reward, "Treasure Reward Stage", sourceWave);
                }
                break;

            case WaveData.WaveType.Event:
                resolved = GetRandomFromPool(eventWavePool);
                if (resolved == null && activeFloor != null && activeFloor.waves != null)
                {
                    resolved = FindWaveOfType(activeFloor.waves, WaveData.WaveType.Event);
                }
                if (resolved == null)
                {
                    resolved = CreateDefaultWave(WaveData.WaveType.Event, "Merchant Event Stage", sourceWave);
                }
                break;

            case WaveData.WaveType.Boss:
                resolved = bossWave;
                if (resolved == null && activeFloor != null && activeFloor.waves != null)
                {
                    resolved = FindWaveOfType(activeFloor.waves, WaveData.WaveType.Boss);
                }
                if (resolved == null)
                {
                    resolved = ResolveWaveForType(WaveData.WaveType.Combat, sourceWave, activeFloor);
                }
                break;
        }

        return resolved;
    }

    private WaveData FindWaveOfType(WaveData[] waves, WaveData.WaveType targetType)
    {
        if (waves == null) return null;
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] != null && waves[i].waveType == targetType)
                return waves[i];
        }
        return null;
    }

    private bool HasEnemies(WaveData wave)
    {
        if (wave == null) return false;
        if (wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0) return true;
        if (wave.randomEnemyPool != null && wave.randomEnemyPool.Length > 0) return true;
        return false;
    }

    private WaveData CloneWaveWithType(WaveData source, WaveData.WaveType newType, string namePrefix)
    {
        WaveData copy = ScriptableObject.CreateInstance<WaveData>();
        copy.name = $"{namePrefix} ({source.name})";
        copy.waveType = newType;
        copy.isRandom = source.isRandom;
        copy.enemyPrefabs = source.enemyPrefabs != null ? (GameObject[])source.enemyPrefabs.Clone() : null;
        copy.randomEnemyPool = source.randomEnemyPool != null ? (GameObject[])source.randomEnemyPool.Clone() : null;
        copy.minRandomEnemies = source.minRandomEnemies;
        copy.maxRandomEnemies = source.maxRandomEnemies;
        copy.transitionDelay = source.transitionDelay;
        return copy;
    }

    private WaveData CreateDefaultWave(WaveData.WaveType type, string stageName, WaveData sourceWave = null)
    {
        WaveData wave = ScriptableObject.CreateInstance<WaveData>();
        wave.name = stageName;
        wave.waveType = type;
        if (sourceWave != null)
        {
            wave.enemyPrefabs = sourceWave.enemyPrefabs != null ? (GameObject[])sourceWave.enemyPrefabs.Clone() : null;
            wave.randomEnemyPool = sourceWave.randomEnemyPool != null ? (GameObject[])sourceWave.randomEnemyPool.Clone() : null;
            wave.minRandomEnemies = sourceWave.minRandomEnemies;
            wave.maxRandomEnemies = sourceWave.maxRandomEnemies;
            wave.isRandom = sourceWave.isRandom;
            wave.transitionDelay = sourceWave.transitionDelay;
        }
        return wave;
    }

    private WaveData GetRandomFromPool(WaveData[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];
    }

    private WaveData[] BuildFallbackFloor()
    {
        List<WaveData.WaveType> fallbackTypes = new List<WaveData.WaveType>
        {
            WaveData.WaveType.Combat,
            WaveData.WaveType.Combat,
            WaveData.WaveType.Camp,
            WaveData.WaveType.Combat,
            WaveData.WaveType.Boss
        };
        return BuildWavesFromTypesWithFallbacks(fallbackTypes);
    }
}
