using System;
using System.Collections.Generic;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class ScoringServiceTests
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
        public void EvaluateStars_UnderFiveStarThreshold_ReturnsFive()
        {
            Assert.AreEqual(5, CreateSut().EvaluateStars(CreateLevel(), 59f));
        }

        [Test]
        public void EvaluateStars_ExactlyAtFiveStarThreshold_ReturnsFive()
        {
            Assert.AreEqual(5, CreateSut().EvaluateStars(CreateLevel(), 60f));
        }

        [Test]
        public void EvaluateStars_JustOverFiveStarThreshold_ReturnsFour()
        {
            Assert.AreEqual(4, CreateSut().EvaluateStars(CreateLevel(), 61f));
        }

        [Test]
        public void EvaluateStars_ExactlyAtFourStarThreshold_ReturnsFour()
        {
            Assert.AreEqual(4, CreateSut().EvaluateStars(CreateLevel(), 120f));
        }

        [Test]
        public void EvaluateStars_ExactlyAtThreeStarThreshold_ReturnsThree()
        {
            Assert.AreEqual(3, CreateSut().EvaluateStars(CreateLevel(), 180f));
        }

        [Test]
        public void EvaluateStars_ExactlyAtTwoStarThreshold_ReturnsTwo()
        {
            Assert.AreEqual(2, CreateSut().EvaluateStars(CreateLevel(), 240f));
        }

        [Test]
        public void EvaluateStars_OverAllThresholds_ReturnsOne()
        {
            Assert.AreEqual(1, CreateSut().EvaluateStars(CreateLevel(), 241f));
        }

        [Test]
        public void EvaluateStars_VeryLargeTime_ReturnsOne()
        {
            Assert.AreEqual(1, CreateSut().EvaluateStars(CreateLevel(), 999999f));
        }

        [Test]
        public void EvaluateStars_NullLevel_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateSut().EvaluateStars(null, 60f));
        }

        [Test]
        public void EvaluateStars_AllFiveCampaignLevels_NeverReturnsZero()
        {
            var sut = CreateSut();
            List<LevelDefinition> levels = new List<LevelDefinition>
            {
                CreateLevel("level_001", 15),
                CreateLevel("level_002", 23),
                CreateLevel("level_003", 31),
                CreateLevel("level_004", 47),
                CreateLevel("level_005", 63),
            };

            foreach (LevelDefinition level in levels)
                Assert.GreaterOrEqual(sut.EvaluateStars(level, 999f), 1);
        }

        private static ScoringService CreateSut()
        {
            return new ScoringService(new PersistenceService());
        }

        private static LevelDefinition CreateLevel(string levelId = "level_001", int size = 31)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = levelId,
                DisplayName = levelId,
                GridSize = size,
                SeedMode = SeedMode.Random,
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };
        }
    }
}
