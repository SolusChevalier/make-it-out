using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    public class LeaderboardEntry
    {
        public string LevelId;
        public string DisplayName;
        public float BestTime;
        public int BestStars;
    }

    public class PersistenceService
    {
        private const string KeyPrefix = "v1_";
        private const string KnownLevelsKey = "v1_known_levels";

        private string TimeKey(string levelId) => $"{KeyPrefix}{levelId}_time";
        private string StarsKey(string levelId) => $"{KeyPrefix}{levelId}_stars";

        public void SaveResult(string levelId, float time, int stars)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                throw new ArgumentException("Level id must be non-empty.", nameof(levelId));

            TrackLevel(levelId);

            if (IsPersonalBest(levelId, time))
                PlayerPrefs.SetFloat(TimeKey(levelId), time);

            if (stars > GetBestStars(levelId))
                PlayerPrefs.SetInt(StarsKey(levelId), stars);

            PlayerPrefs.Save();
        }

        public float GetBestTime(string levelId)
        {
            return PlayerPrefs.GetFloat(TimeKey(levelId), float.MaxValue);
        }

        public int GetBestStars(string levelId)
        {
            return PlayerPrefs.GetInt(StarsKey(levelId), 0);
        }

        public bool IsPersonalBest(string levelId, float time)
        {
            return time < GetBestTime(levelId);
        }

        public List<LeaderboardEntry> GetLeaderboard(LevelRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var entries = new List<LeaderboardEntry>();

            foreach (LevelDefinitionAsset asset in registry.CampaignLevels)
            {
                if (asset == null || asset.Definition == null)
                    continue;

                LevelDefinition level = asset.Definition;
                int stars = GetBestStars(level.LevelId);
                if (stars == 0)
                    continue;

                entries.Add(new LeaderboardEntry
                {
                    LevelId = level.LevelId,
                    DisplayName = level.DisplayName,
                    BestTime = GetBestTime(level.LevelId),
                    BestStars = stars
                });
            }

            return entries
                .OrderByDescending(e => e.BestStars)
                .ThenBy(e => e.BestTime)
                .ToList();
        }

        public void ClearAll()
        {
            foreach (string levelId in GetTrackedLevels())
            {
                PlayerPrefs.DeleteKey(TimeKey(levelId));
                PlayerPrefs.DeleteKey(StarsKey(levelId));
            }

            PlayerPrefs.DeleteKey(KnownLevelsKey);
            PlayerPrefs.Save();
        }

        private void TrackLevel(string levelId)
        {
            HashSet<string> known = GetTrackedLevels();
            if (!known.Add(levelId))
                return;

            PlayerPrefs.SetString(KnownLevelsKey, string.Join("|", known));
        }

        private HashSet<string> GetTrackedLevels()
        {
            string serialized = PlayerPrefs.GetString(KnownLevelsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(serialized))
                return new HashSet<string>();

            return new HashSet<string>(
                serialized.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
