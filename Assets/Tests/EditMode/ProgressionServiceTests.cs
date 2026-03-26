using System;
using System.Collections.Generic;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class ProgressionServiceTests
    {
        private LevelRegistry _registry;
        private PersistenceService _persistence;
        private ProgressionService _progression;
        private readonly List<ScriptableObject> _createdAssets = new List<ScriptableObject>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();

            _registry = new LevelRegistry
            {
                CampaignLevels = new List<LevelDefinitionAsset>
                {
                    MakeLevelAsset("level_001", 15),
                    MakeLevelAsset("level_002", 23),
                    MakeLevelAsset("level_003", 31),
                }
            };

            _persistence = new PersistenceService();
            _progression = new ProgressionService(_registry, _persistence);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            foreach (ScriptableObject asset in _createdAssets)
            {
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
            _createdAssets.Clear();
        }

        [Test]
        public void IsUnlocked_IndexZero_AlwaysTrue()
        {
            Assert.IsTrue(_progression.IsUnlocked(0));
        }

        [Test]
        public void IsUnlocked_IndexOne_FalseWithNoSaveData()
        {
            Assert.IsFalse(_progression.IsUnlocked(1));
        }

        [Test]
        public void IsUnlocked_IndexOne_TrueAfterCompletingLevelZero()
        {
            _persistence.SaveResult("level_001", 120f, 1);
            Assert.IsTrue(_progression.IsUnlocked(1));
        }

        [Test]
        public void IsUnlocked_IndexTwo_FalseIfOnlyLevelZeroComplete()
        {
            _persistence.SaveResult("level_001", 120f, 1);
            Assert.IsFalse(_progression.IsUnlocked(2));
        }

        [Test]
        public void IsUnlocked_NegativeIndex_ReturnsFalse()
        {
            Assert.IsFalse(_progression.IsUnlocked(-1));
        }

        [Test]
        public void GetLevel_IndexWithinCampaign_ReturnsCampaignLevel()
        {
            LevelDefinition level = _progression.GetLevel(0);
            Assert.AreEqual("level_001", level.LevelId);
        }

        [Test]
        public void GetLevel_IndexBeyondCampaign_ReturnsGeneratedLevel()
        {
            LevelDefinition level = _progression.GetLevel(3);
            Assert.IsInstanceOf<GeneratedLevelDefinition>(level);
            Assert.Greater(level.GridSize, 31);
            Assert.That(level.LevelId, Does.StartWith("scaled_"));
            Assert.AreEqual(4, level.StarThresholds.Length);
        }

        [Test]
        public void GetLevel_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _progression.GetLevel(-1));
        }

        [Test]
        public void GenerateScaledLevel_GridSizeIsAlwaysOdd()
        {
            for (int i = 3; i <= 20; i++)
                Assert.AreEqual(1, _progression.GetLevel(i).GridSize % 2);
        }

        [Test]
        public void GenerateScaledLevel_GridSizeIsAlwaysWithinBounds()
        {
            for (int i = 3; i <= 50; i++)
            {
                int size = _progression.GetLevel(i).GridSize;
                Assert.GreaterOrEqual(size, 15);
                Assert.LessOrEqual(size, 127);
            }
        }

        [Test]
        public void GenerateScaledLevel_GridSizeIncreasesMonotonically()
        {
            for (int i = 3; i <= 10; i++)
                Assert.GreaterOrEqual(_progression.GetLevel(i + 1).GridSize, _progression.GetLevel(i).GridSize);
        }

        [Test]
        public void GetHighestUnlockedIndex_NoSaveData_ReturnsZero()
        {
            Assert.AreEqual(0, _progression.GetHighestUnlockedIndex());
        }

        [Test]
        public void GetHighestUnlockedIndex_AllLevelsComplete_ReturnsLastIndex()
        {
            _persistence.SaveResult("level_001", 100f, 1);
            _persistence.SaveResult("level_002", 100f, 1);
            _persistence.SaveResult("level_003", 100f, 1);

            Assert.AreEqual(2, _progression.GetHighestUnlockedIndex());
        }

        [Test]
        public void NullRegistry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionService(null, _persistence));
        }

        [Test]
        public void NullPersistence_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgressionService(_registry, null));
        }

        private LevelDefinitionAsset MakeLevelAsset(string id, int gridSize)
        {
            LevelDefinitionAsset asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
            asset.Definition = new GeneratedLevelDefinition
            {
                LevelId = id,
                DisplayName = id,
                GridSize = gridSize,
                SeedMode = SeedMode.Random,
                StarThresholds = new[] { 60f, 120f, 180f, 240f },
                IsCampaignLevel = true,
            };

            _createdAssets.Add(asset);
            return asset;
        }
    }
}
