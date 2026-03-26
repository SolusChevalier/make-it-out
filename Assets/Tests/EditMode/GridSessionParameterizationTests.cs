using System;
using System.Reflection;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class GridSessionParameterizationTests
    {
        [TearDown]
        public void TearDown()
        {
            GridSession.Reset();
        }

        [Test]
        public void GridSessionInitialise_ThrowsForEvenGridSize()
        {
            LevelDefinition evenLevel = CreateLevel(30, SeedMode.Fixed, 11);
            Assert.Throws<ArgumentException>(() => GridSession.Initialise(evenLevel, 11));
        }

        [Test]
        public void GridSessionInitialise_ThrowsForOutOfRangeGridSize()
        {
            LevelDefinition tooSmall = CreateLevel(13, SeedMode.Fixed, 11);
            LevelDefinition tooLarge = CreateLevel(129, SeedMode.Fixed, 11);

            Assert.Throws<ArgumentOutOfRangeException>(() => GridSession.Initialise(tooSmall, 11));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridSession.Initialise(tooLarge, 11));
        }

        [Test]
        public void ChunksPerAxis_ComputesExpectedValues()
        {
            Assert.AreEqual(2, GridConfig.ChunksPerAxis(15));
            Assert.AreEqual(4, GridConfig.ChunksPerAxis(31));
            Assert.AreEqual(8, GridConfig.ChunksPerAxis(63));
            Assert.AreEqual(12, GridConfig.ChunksPerAxis(95));
        }

        [Test]
        public void WorldGridInitialise_AllocatesExpectedCellCount_ForTargetSizes()
        {
            int[] sizes = { 15, 31, 63, 95 };
            for (int i = 0; i < sizes.Length; i++)
            {
                GridSession.Initialise(CreateLevel(sizes[i], SeedMode.Fixed, i + 1), i + 1);
                WorldGrid.Instance.Initialise(GridSession.GridSize);

                int expected = sizes[i] * sizes[i] * sizes[i];
                Assert.AreEqual(expected, WorldGrid.Instance.CopyBlockGrid().Length);
                Assert.AreEqual(expected, WorldGrid.Instance.CopyFeatureGrid().Length);
            }
        }

        [Test]
        public void WorldGridPublicMethods_ThrowBeforeInitialise()
        {
            FieldInfo blockField = typeof(WorldGrid).GetField("_blockGrid", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo featureField = typeof(WorldGrid).GetField("_featureGrid", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(blockField);
            Assert.IsNotNull(featureField);

            blockField.SetValue(WorldGrid.Instance, null);
            featureField.SetValue(WorldGrid.Instance, null);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => WorldGrid.Instance.GetBlock(0, 0, 0));
            StringAssert.Contains("WorldGrid has not been initialised", ex.Message);
        }

        [Test]
        public void SeedResolver_RespectsSeedMode()
        {
            LevelDefinition fixedLevel = CreateLevel(31, SeedMode.Fixed, 4242);
            Assert.AreEqual(4242, SeedResolver.Resolve(fixedLevel));
            Assert.AreEqual(4242, SeedResolver.Resolve(fixedLevel));

            LevelDefinition randomLevel = CreateLevel(31, SeedMode.Random, 0);
            int baseline = SeedResolver.Resolve(randomLevel);
            bool sawDifference = false;
            for (int i = 0; i < 10; i++)
            {
                if (SeedResolver.Resolve(randomLevel) != baseline)
                {
                    sawDifference = true;
                    break;
                }
            }

            Assert.IsTrue(sawDifference, "Expected random seed mode to vary across repeated resolves.");
        }

        [Test]
        public void ChunkManagerClear_RemovesRegisteredChunkObjects()
        {
            GameObject root = new GameObject("ChunkManager_Clear_Test");
            ChunkManager manager = root.AddComponent<ChunkManager>();

            Vector3Int coord = new Vector3Int(0, 0, 0);
            GameObject chunkObj = new GameObject("Chunk_Object_Test");
            manager.RegisterChunk(coord, new ChunkData { ChunkCoord = coord, IsActive = true }, chunkObj);

            manager.Clear();

            Assert.IsNull(manager.GetChunkObject(coord));
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static LevelDefinition CreateLevel(int gridSize, SeedMode seedMode, int fixedSeed)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = "grid_session_test",
                DisplayName = "Grid Session Test",
                GridSize = gridSize,
                SeedMode = seedMode,
                FixedSeed = fixedSeed,
                IsCampaignLevel = false,
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };
        }
    }
}
