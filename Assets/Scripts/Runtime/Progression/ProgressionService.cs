using System;
using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    public class ProgressionService
    {
        private readonly LevelRegistry _registry;
        private readonly PersistenceService _persistence;

        private const int ScaledGridStep = 4;
        private const float ScaledTimePerUnit = 3f;

        public ProgressionService(LevelRegistry registry, PersistenceService persistence)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        public int GetTotalCampaignLevels() => _registry.CampaignLevels.Count;

        public LevelDefinition GetLevel(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index < _registry.CampaignLevels.Count)
            {
                LevelDefinitionAsset levelAsset = _registry.CampaignLevels[index];
                if (levelAsset == null || levelAsset.Definition == null)
                    throw new InvalidOperationException($"Campaign level at index {index} is null.");

                return levelAsset.Definition;
            }

            return GenerateScaledLevel(index);
        }

        public bool IsUnlocked(int index)
        {
            if (index == 0) return true;
            if (index < 0) return false;

            LevelDefinition previous = GetLevel(index - 1);
            return _persistence.GetBestStars(previous.LevelId) >= 1;
        }

        public int GetHighestUnlockedIndex()
        {
            int total = GetTotalCampaignLevels();
            if (total <= 0)
                return 0;

            for (int i = 0; i < total; i++)
                if (!IsUnlocked(i))
                    return Mathf.Max(0, i - 1);

            return total - 1;
        }

        private LevelDefinition GenerateScaledLevel(int index)
        {
            int campaignCount = _registry.CampaignLevels.Count;
            int stepsAbove = index - campaignCount + 1;
            int lastGrid = campaignCount > 0 && _registry.CampaignLevels[campaignCount - 1] != null
                ? _registry.CampaignLevels[campaignCount - 1].Definition.GridSize
                : 63;

            int gridSize = lastGrid + (stepsAbove * ScaledGridStep);
            if (gridSize % 2 == 0)
                gridSize++;

            gridSize = Mathf.Clamp(gridSize, 15, 127);

            float fiveStar = gridSize * ScaledTimePerUnit;
            float[] thresholds =
            {
                fiveStar,
                fiveStar * 2f,
                fiveStar * 3f,
                fiveStar * 4f
            };

            return new GeneratedLevelDefinition
            {
                LevelId = $"scaled_{index:D4}",
                DisplayName = $"Level {index + 1}",
                GridSize = gridSize,
                SeedMode = SeedMode.Random,
                FixedSeed = 0,
                IsCampaignLevel = false,
                StarThresholds = thresholds
            };
        }
    }
}
