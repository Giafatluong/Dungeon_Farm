using UnityEngine;
using System.Collections.Generic;

public class FloorGenerator : MonoBehaviour
{
    [Header("Generator Configuration")]
    public int totalStages = 8;
    public int minCombatCount = 3;
    public int maxConsecutiveReward = 1;
    public int maxConsecutiveCamp = 1;

    [Header("Score Balance (GDD: Combat=+1, Event=-1, Reward=-2, Camp=0)")]
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

    public WaveData[] GenerateFloor()
    {
        int maxAttempts = 100;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            List<WaveData.WaveType> stageTypes = new List<WaveData.WaveType>();

            int currentScore = 0;
            int combatCount = 0;
            int consecutiveReward = 0;
            int consecutiveCamp = 0;
            bool valid = true;

            // Sinh các stage từ 0 đến totalStages - 2
            for (int i = 0; i < totalStages - 1; i++)
            {
                List<WaveData.WaveType> allowedTypes = new List<WaveData.WaveType> { WaveData.WaveType.Combat, WaveData.WaveType.Event };

                if (consecutiveReward < maxConsecutiveReward && rewardWavePool != null && rewardWavePool.Length > 0)
                {
                    allowedTypes.Add(WaveData.WaveType.Reward);
                }

                if (consecutiveCamp < maxConsecutiveCamp && campWavePool != null && campWavePool.Length > 0)
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

            // Stage cuối cùng luôn luôn là Boss (theo GDD: Boss luôn là Stage cuối của Floor)
            stageTypes.Add(WaveData.WaveType.Boss);

            // Kiểm tra các ràng buộc cứng (Constraints)
            if (combatCount < minCombatCount) valid = false;
            if (currentScore < minTargetScore || currentScore > maxTargetScore) valid = false;

            if (valid)
            {
                Debug.Log($"Sinh Floor thành công ở lần thử {attempt + 1}. Tổng điểm: {currentScore}, Số trận đánh: {combatCount}");
                return BuildWavesFromTypes(stageTypes);
            }
        }

        Debug.LogWarning("Không tìm được cấu hình Floor hoàn hảo trong giới hạn lần thử. Dùng cấu hình dự phòng.");
        return BuildFallbackFloor();
    }

    private WaveData[] BuildWavesFromTypes(List<WaveData.WaveType> types)
    {
        WaveData[] result = new WaveData[types.Count];

        for (int i = 0; i < types.Count; i++)
        {
            switch (types[i])
            {
                case WaveData.WaveType.Combat:
                    result[i] = GetRandomFromPool(combatWavePool);
                    break;
                case WaveData.WaveType.Camp:
                    result[i] = GetRandomFromPool(campWavePool);
                    break;
                case WaveData.WaveType.Event:
                    result[i] = GetRandomFromPool(eventWavePool);
                    break;
                case WaveData.WaveType.Reward:
                    result[i] = GetRandomFromPool(rewardWavePool);
                    break;
                case WaveData.WaveType.Boss:
                    result[i] = bossWave;
                    break;
            }
        }

        return result;
    }

    private WaveData GetRandomFromPool(WaveData[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];
    }

    private WaveData[] BuildFallbackFloor()
    {
        List<WaveData> waves = new List<WaveData>();
        if (combatWavePool != null && combatWavePool.Length > 0)
        {
            waves.Add(GetRandomFromPool(combatWavePool));
            waves.Add(GetRandomFromPool(combatWavePool));
        }
        if (campWavePool != null && campWavePool.Length > 0)
        {
            waves.Add(GetRandomFromPool(campWavePool));
        }
        if (combatWavePool != null && combatWavePool.Length > 0)
        {
            waves.Add(GetRandomFromPool(combatWavePool));
        }
        if (bossWave != null)
        {
            waves.Add(bossWave);
        }
        return waves.ToArray();
    }
}
