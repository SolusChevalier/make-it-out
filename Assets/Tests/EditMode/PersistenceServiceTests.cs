using System.Collections.Generic;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class PersistenceServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        [Test]
        public void SaveResult_FirstTime_StoresBothTimeAndStars()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 4);

            Assert.AreEqual(90f, sut.GetBestTime("level_001"));
            Assert.AreEqual(4, sut.GetBestStars("level_001"));
        }

        [Test]
        public void SaveResult_BetterTime_UpdatesBothValues()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 4);
            sut.SaveResult("level_001", 75f, 5);

            Assert.AreEqual(75f, sut.GetBestTime("level_001"));
            Assert.AreEqual(5, sut.GetBestStars("level_001"));
        }

        [Test]
        public void SaveResult_WorseTime_DoesNotOverwrite()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 4);
            sut.SaveResult("level_001", 120f, 3);

            Assert.AreEqual(90f, sut.GetBestTime("level_001"));
            Assert.AreEqual(4, sut.GetBestStars("level_001"));
        }

        [Test]
        public void SaveResult_SameTimeBetterStars_UpdatesStarsOnly()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 3);
            sut.SaveResult("level_001", 90f, 4);

            Assert.AreEqual(90f, sut.GetBestTime("level_001"));
            Assert.AreEqual(4, sut.GetBestStars("level_001"));
        }

        [Test]
        public void GetBestTime_NoRecord_ReturnsFloatMaxValue()
        {
            var sut = new PersistenceService();
            Assert.AreEqual(float.MaxValue, sut.GetBestTime("level_001"));
        }

        [Test]
        public void GetBestStars_NoRecord_ReturnsZero()
        {
            var sut = new PersistenceService();
            Assert.AreEqual(0, sut.GetBestStars("level_001"));
        }

        [Test]
        public void IsPersonalBest_NoRecord_ReturnsTrue()
        {
            var sut = new PersistenceService();
            Assert.IsTrue(sut.IsPersonalBest("level_001", 120f));
        }

        [Test]
        public void IsPersonalBest_WorseThanExisting_ReturnsFalse()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 4);
            Assert.IsFalse(sut.IsPersonalBest("level_001", 95f));
        }

        [Test]
        public void IsPersonalBest_BetterThanExisting_ReturnsTrue()
        {
            var sut = new PersistenceService();
            sut.SaveResult("level_001", 90f, 4);
            Assert.IsTrue(sut.IsPersonalBest("level_001", 85f));
        }

        [Test]
        public void GetLeaderboard_NoCompletedLevels_ReturnsEmptyList()
        {
            var sut = new PersistenceService();
            LevelRegistry registry = CreateRegistry("level_001", "level_002", "level_003");

            List<LeaderboardEntry> result = sut.GetLeaderboard(registry);
            Assert.IsEmpty(result);
        }

        [Test]
        public void GetLeaderboard_MultipleEntries_SortedByStarsThenTime()
        {
            var sut = new PersistenceService();
            LevelRegistry registry = CreateRegistry("level_001", "level_002", "level_003");

            sut.SaveResult("level_001", 120f, 3);
            sut.SaveResult("level_002", 90f, 5);
            sut.SaveResult("level_003", 100f, 3);

            List<LeaderboardEntry> result = sut.GetLeaderboard(registry);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("level_002", result[0].LevelId);
            Assert.AreEqual("level_003", result[1].LevelId);
            Assert.AreEqual("level_001", result[2].LevelId);
        }

        [Test]
        public void VersionedKeys_DoNotCollideWithUnversionedKeys()
        {
            var sut = new PersistenceService();
            PlayerPrefs.SetFloat("level_001_time", 999f);
            PlayerPrefs.Save();

            Assert.AreEqual(float.MaxValue, sut.GetBestTime("level_001"));
        }

        private static LevelRegistry CreateRegistry(params string[] levelIds)
        {
            var registry = new LevelRegistry();
            foreach (string id in levelIds)
            {
                LevelDefinitionAsset asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
                asset.Definition = new LevelDefinition
                {
                    LevelId = id,
                    DisplayName = id,
                    GridSize = 31,
                    SeedMode = SeedMode.Random,
                    StarThresholds = new[] { 60f, 120f, 180f, 240f }
                };
                registry.CampaignLevels.Add(asset);
            }

            return registry;
        }
    }
}
