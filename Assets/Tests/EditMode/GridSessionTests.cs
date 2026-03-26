using System;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class GridSessionTests
    {
        [TearDown]
        public void TearDown()
        {
            GridSession.Reset();
        }

        [Test]
        public void Initialise_ValidOddSize_SetsGridSize()
        {
            GridSession.Initialise(MakeLevel(31), 42);

            Assert.AreEqual(31, GridSession.GridSize);
            Assert.AreEqual(42, GridSession.Seed);
        }

        [Test]
        public void Initialise_EvenSize_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => GridSession.Initialise(MakeLevel(32), 0));
        }

        [Test]
        public void Initialise_SizeBelowMinimum_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GridSession.Initialise(MakeLevel(13), 0));
        }

        [Test]
        public void Initialise_SizeAboveMaximum_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GridSession.Initialise(MakeLevel(129), 0));
        }

        [Test]
        public void ChunksPerAxis_CorrectForVariousSizes()
        {
            AssertChunks(15, 2);
            AssertChunks(31, 4);
            AssertChunks(63, 8);
            AssertChunks(95, 12);
            AssertChunks(127, 16);
        }

        [Test]
        public void Reset_RestoresDefaults()
        {
            GridSession.Initialise(MakeLevel(31), 99);
            GridSession.Reset();

            Assert.AreEqual(63, GridSession.GridSize);
            Assert.AreEqual(0, GridSession.Seed);
        }

        [Test]
        public void ToIndex_RoundTrip_MatchesFromIndex()
        {
            GridSession.Initialise(MakeLevel(63), 0);

            int[] values = { 0, 5, 10 };
            foreach (int x in values)
            foreach (int y in values)
            foreach (int z in values)
            {
                int idx = GridIndex.ToIndex(x, y, z);
                Vector3Int roundTrip = GridIndex.FromIndex(idx);
                Assert.AreEqual(new Vector3Int(x, y, z), roundTrip);
            }
        }

        [Test]
        public void ToIndex_OutOfBoundsCoordinate_ProducesOutOfRangeIndex()
        {
            GridSession.Initialise(MakeLevel(63), 0);
            int total = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;

            int negative = GridIndex.ToIndex(-1, 0, 0);
            int tooLarge = GridIndex.ToIndex(GridSession.GridSize, 0, 0);

            Assert.Less(negative, 0);
            Assert.GreaterOrEqual(tooLarge, total);
        }

        private static void AssertChunks(int gridSize, int expected)
        {
            GridSession.Initialise(MakeLevel(gridSize), 0);
            Assert.AreEqual(expected, GridSession.ChunksPerAxis);
        }

        private static LevelDefinition MakeLevel(int gridSize)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = $"grid_{gridSize}",
                DisplayName = "Grid Session Test",
                GridSize = gridSize,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 0,
                StarThresholds = new[] { 60f, 120f, 180f, 240f },
            };
        }
    }
}
