using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class ProgressionServicesTests
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
        public void CampaignLevelAssets_ExistAndRegistryContainsAllFiveInOrder()
        {
            string[] expectedIds = { "level_001", "level_002", "level_003", "level_004", "level_005" };
            string[] expectedNames = { "The Cube", "Deeper", "Disoriented", "The Long Way", "No Going Back" };
            int[] expectedGridSizes = { 15, 23, 31, 47, 63 };

            LevelRegistryAsset registryAsset = AssetDatabase.LoadAssetAtPath<LevelRegistryAsset>(
                "Assets/Data/LevelRegistry.asset");

            Assert.IsNotNull(registryAsset, "LevelRegistry asset is missing.");
            Assert.IsNotNull(registryAsset.Registry, "LevelRegistry payload is null.");
            Assert.AreEqual(5, registryAsset.Registry.CampaignLevels.Count, "Campaign level count mismatch.");

            for (int i = 0; i < expectedIds.Length; i++)
            {
                LevelDefinitionAsset levelAsset = registryAsset.Registry.CampaignLevels[i];
                Assert.IsNotNull(levelAsset, $"Registry entry {i} is null.");
                Assert.IsNotNull(levelAsset.Definition, $"Registry entry {i} definition is null.");

                Assert.AreEqual(expectedIds[i], levelAsset.Definition.LevelId, $"LevelId mismatch at index {i}.");
                Assert.AreEqual(expectedNames[i], levelAsset.Definition.DisplayName, $"DisplayName mismatch at index {i}.");
                Assert.AreEqual(expectedGridSizes[i], levelAsset.Definition.GridSize, $"GridSize mismatch at index {i}.");
                Assert.AreEqual(4, levelAsset.Definition.StarThresholds.Length, $"Star threshold count mismatch at index {i}.");
            }
        }

        [Test]
        public void ProgressionUnlocking_DefaultSaveData_OnlyLevelZeroUnlocked()
        {
            ProgressionService service = CreateProgressionServiceWithCampaignLevels();

            Assert.IsTrue(service.IsUnlocked(0));
            Assert.IsFalse(service.IsUnlocked(1));
        }

        [Test]
        public void PersistenceSaveResult_PreservesBestTimeAndStars()
        {
            PersistenceService persistence = new PersistenceService();

            persistence.SaveResult("level_001", 90f, 4);
            Assert.AreEqual(4, persistence.GetBestStars("level_001"));
            Assert.AreEqual(90f, persistence.GetBestTime("level_001"));

            persistence.SaveResult("level_001", 150f, 3);
            Assert.AreEqual(4, persistence.GetBestStars("level_001"));
            Assert.AreEqual(90f, persistence.GetBestTime("level_001"));
        }

        [Test]
        public void ScoringService_EvaluateStars_MatchesThresholdRules()
        {
            PersistenceService persistence = new PersistenceService();
            ScoringService scoring = new ScoringService(persistence);

            LevelDefinition level = new LevelDefinition
            {
                LevelId = "level_001",
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };

            Assert.AreEqual(5, scoring.EvaluateStars(level, 59f));
            Assert.AreEqual(4, scoring.EvaluateStars(level, 61f));
            Assert.AreEqual(1, scoring.EvaluateStars(level, 241f));
            Assert.GreaterOrEqual(scoring.EvaluateStars(level, 10000f), 1);
            Assert.GreaterOrEqual(scoring.EvaluateStars(level, 0f), 1);
        }

        [Test]
        public void ProgressionService_BeyondCampaign_ReturnsScaledOddLargerGrid()
        {
            ProgressionService service = CreateProgressionServiceWithCampaignLevels();
            int beyondIndex = service.GetTotalCampaignLevels();

            LevelDefinition scaled = service.GetLevel(beyondIndex);
            LevelDefinition lastCampaign = service.GetLevel(service.GetTotalCampaignLevels() - 1);

            Assert.IsNotNull(scaled);
            Assert.AreEqual(1, scaled.GridSize % 2, "Scaled GridSize must be odd.");
            Assert.Greater(scaled.GridSize, lastCampaign.GridSize, "Scaled GridSize should grow beyond last campaign level.");
        }

        [Test]
        public void ScoringService_AlwaysReturnsAtLeastOne_ForFiniteTimes()
        {
            ScoringService scoring = new ScoringService(new PersistenceService());
            LevelDefinition level = new LevelDefinition
            {
                LevelId = "finite_check",
                StarThresholds = new[] { 10f, 20f, 30f, 40f }
            };

            float[] times = { 0f, 0.1f, 9.99f, 10f, 10.01f, 1000f, 123456f };
            foreach (float t in times)
            {
                Assert.GreaterOrEqual(scoring.EvaluateStars(level, t), 1, $"Expected >=1 stars for time {t}.");
            }
        }

        private static ProgressionService CreateProgressionServiceWithCampaignLevels()
        {
            LevelRegistry registry = new LevelRegistry();
            registry.CampaignLevels.Add(CreateLevelAsset("level_001", "The Cube", 15, new[] { 60f, 120f, 180f, 240f }));
            registry.CampaignLevels.Add(CreateLevelAsset("level_002", "Deeper", 23, new[] { 90f, 180f, 270f, 360f }));
            registry.CampaignLevels.Add(CreateLevelAsset("level_003", "Disoriented", 31, new[] { 120f, 240f, 360f, 480f }));
            registry.CampaignLevels.Add(CreateLevelAsset("level_004", "The Long Way", 47, new[] { 180f, 360f, 540f, 720f }));
            registry.CampaignLevels.Add(CreateLevelAsset("level_005", "No Going Back", 63, new[] { 240f, 480f, 720f, 960f }));

            return new ProgressionService(registry, new PersistenceService());
        }

        private static LevelDefinitionAsset CreateLevelAsset(string levelId, string displayName, int gridSize, float[] starThresholds)
        {
            LevelDefinitionAsset asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
            asset.Definition = new LevelDefinition
            {
                LevelId = levelId,
                DisplayName = displayName,
                IsCampaignLevel = true,
                GridSize = gridSize,
                SeedMode = SeedMode.Random,
                FixedSeed = 0,
                StarThresholds = starThresholds
            };

            return asset;
        }
    }
}
