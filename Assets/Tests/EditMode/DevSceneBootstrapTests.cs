using MakeItOut.Runtime.Dev;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class DevSceneBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            GridSession.Initialise(CreateLevel(63), 0);
            WorldGrid.Instance.Initialise(GridSession.GridSize);
            WorldGrid.Instance.ResetForGeneration();
        }

        [Test]
        public void BuildLayout_SetsStartLadderAndExitFeatures()
        {
            Vector3Int spawn = DevCorridorBootstrap.BuildLayout(10);

            Assert.AreEqual(FeatureType.Start, WorldGrid.Instance.GetFeature(spawn));

            Vector3Int ladderOne = new Vector3Int(spawn.x + 2, spawn.y + 1, spawn.z);
            Vector3Int ladderTwo = new Vector3Int(spawn.x + 2, spawn.y + 2, spawn.z);
            Vector3Int exit = new Vector3Int(spawn.x + 10, spawn.y + 1, spawn.z);

            Assert.AreEqual(FeatureType.Ladder, WorldGrid.Instance.GetFeature(ladderOne));
            Assert.AreEqual(FeatureType.Ladder, WorldGrid.Instance.GetFeature(ladderTwo));
            Assert.AreEqual(FeatureType.Exit, WorldGrid.Instance.GetFeature(exit));
        }

        [Test]
        public void BuildLayout_ClampsMinimumCorridorLength()
        {
            Vector3Int spawn = DevCorridorBootstrap.BuildLayout(2);

            Vector3Int clampedExit = new Vector3Int(spawn.x + 6, spawn.y + 1, spawn.z);
            Assert.AreEqual(FeatureType.Exit, WorldGrid.Instance.GetFeature(clampedExit));
        }

        [Test]
        public void BuildLayout_LeavesStepBlockSolidWithAirAbove()
        {
            Vector3Int spawn = DevCorridorBootstrap.BuildLayout(10);

            Vector3Int stepBlock = new Vector3Int(spawn.x + 5, spawn.y, spawn.z);
            Vector3Int stepHeadroom = new Vector3Int(spawn.x + 5, spawn.y + 1, spawn.z);

            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(stepBlock));
            Assert.AreEqual(BlockType.Air, WorldGrid.Instance.GetBlock(stepHeadroom));
        }

        private static LevelDefinition CreateLevel(int gridSize)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = "dev_scene_tests",
                DisplayName = "Dev Scene Tests",
                GridSize = gridSize,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 0,
                StarThresholds = new[] { 60f, 120f, 180f, 240f },
            };
        }
    }
}
