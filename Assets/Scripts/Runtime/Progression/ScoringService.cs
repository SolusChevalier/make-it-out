using System;

namespace MakeItOut.Runtime.Progression
{
    public class ScoringService
    {
        private readonly PersistenceService _persistence;

        public ScoringService(PersistenceService persistence)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        // Returns 1-5. Always returns at least 1 for any finite completion time.
        public int EvaluateStars(LevelDefinition level, float completionTimeSeconds)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            float[] thresholds = level.StarThresholds;
            if (thresholds == null || thresholds.Length < 4)
                return 1;

            if (completionTimeSeconds <= thresholds[0]) return 5;
            if (completionTimeSeconds <= thresholds[1]) return 4;
            if (completionTimeSeconds <= thresholds[2]) return 3;
            if (completionTimeSeconds <= thresholds[3]) return 2;
            return 1;
        }

        public bool IsPersonalBest(string levelId, float completionTimeSeconds)
        {
            float existing = _persistence.GetBestTime(levelId);
            return completionTimeSeconds < existing;
        }
    }
}
