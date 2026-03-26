using System;
using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    public enum SeedMode
    {
        Random,
        Fixed
    }

    [Serializable]
    public class LevelDefinition
    {
        [Header("Identity")]
        public string LevelId;
        public string DisplayName;
        public bool IsCampaignLevel = true;

        [Header("Generation")]
        [Range(15, 127)]
        public int GridSize = 31;
        public SeedMode SeedMode = SeedMode.Random;
        public int FixedSeed = 0;

        [Header("Scoring")]
        [Tooltip("4 values defining time boundaries in seconds for 5, 4, 3, and 2 stars." +
                 " Finishing at any time always awards at least 1 star.")]
        public float[] StarThresholds = new[] { 120f, 240f, 360f, 480f };

        public void Validate(string contextId)
        {
            string id = string.IsNullOrWhiteSpace(LevelId) ? contextId : LevelId;

            if (StarThresholds == null || StarThresholds.Length != 4)
                Debug.LogError($"LevelDefinition '{id}': StarThresholds must have exactly 4 values.");

            if (GridSize % 2 == 0)
                Debug.LogWarning($"LevelDefinition '{id}': GridSize should be odd for a true centre cell.");
        }
    }
}
