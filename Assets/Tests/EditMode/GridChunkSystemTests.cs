using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class GridChunkSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            GridSession.Initialise(CreateLevel(63), 123);
            WorldGrid.Instance.Initialise(GridSession.GridSize);
            WorldGrid.Instance.ResetForGeneration();
        }

        [Test]
        public void GetBlock_ReturnsSolid_ForOutOfBounds()
        {
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(-1, 0, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, -1, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, 0, -1));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(GridSession.GridSize, 0, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, GridSession.GridSize, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, 0, GridSession.GridSize));
        }

        [Test]
        public void WorldToGrid_GridToWorld_RoundTrip_ForAllValidCells()
        {
            for (int z = 0; z < GridSession.GridSize; z++)
            {
                for (int y = 0; y < GridSession.GridSize; y++)
                {
                    for (int x = 0; x < GridSession.GridSize; x++)
                    {
                        Vector3Int gridPos = new Vector3Int(x, y, z);
                        Vector3 worldPos = WorldGrid.Instance.GridToWorld(gridPos);
                        Vector3Int roundTrip = WorldGrid.Instance.WorldToGrid(worldPos);
                        Assert.AreEqual(gridPos, roundTrip);
                    }
                }
            }
        }

        [Test]
        public void GridIndex_ToIndexFromIndex_RoundTrip_ForAllIndices()
        {
            int total = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;
            for (int i = 0; i < total; i++)
            {
                Vector3Int gridPos = GridIndex.FromIndex(i);
                int roundTrip = GridIndex.ToIndex(gridPos.x, gridPos.y, gridPos.z);
                Assert.AreEqual(i, roundTrip);
            }
        }

        [Test]
        public void ChunkMeshBuilder_CullsInteriorFace_ForAdjacentSolidBlocks()
        {
            int c = GridSession.GridSize / 2;
            WorldGrid.Instance.SetBlock(c - 1, c, c, BlockType.Solid);
            WorldGrid.Instance.SetBlock(c, c, c, BlockType.Solid);

            Vector3Int chunk = ChunkCoordinateUtility.GridToChunk(new Vector3Int(c - 1, c, c));
            Mesh mesh = ChunkMeshBuilder.BuildChunkMesh(chunk);

            const int expectedVisibleFaces = 10;
            const int indicesPerFace = 6;
            Assert.AreEqual(expectedVisibleFaces * indicesPerFace, mesh.triangles.Length);
        }

        [Test]
        public void ChunkManager_ActivatesNearbyChunks_AndDeactivatesFarChunks()
        {
            int c = GridSession.GridSize / 2;
            WorldGrid.Instance.SetBlock(c, c, c, BlockType.Solid);
            WorldGrid.Instance.SetBlock(GridSession.GridSize - 2, GridSession.GridSize - 2, GridSession.GridSize - 2, BlockType.Solid);

            GameObject chunkRoot = new GameObject("ChunkManagerTest");
            ChunkManager manager = chunkRoot.AddComponent<ChunkManager>();
            manager.ViewDistanceChunks = 0;
            manager.InitialiseAllChunks();

            Vector3Int playerNearOrigin = new Vector3Int(c, c, c);
            manager.UpdateActiveChunks(playerNearOrigin);

            ChunkData nearChunk = manager.GetChunk(playerNearOrigin);
            Assert.IsNotNull(nearChunk);
            Assert.IsTrue(nearChunk.IsActive);

            Vector3Int farGridPos = new Vector3Int(
                GridSession.GridSize - 1,
                GridSession.GridSize - 1,
                GridSession.GridSize - 1);

            ChunkData farChunk = manager.GetChunk(farGridPos);
            Assert.IsNotNull(farChunk);
            Assert.IsFalse(farChunk.IsActive);

            Object.DestroyImmediate(chunkRoot);
        }

        private static LevelDefinition CreateLevel(int gridSize)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = "grid_chunk_tests",
                DisplayName = "Grid Chunk Tests",
                GridSize = gridSize,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 123,
                StarThresholds = new[] { 60f, 120f, 180f, 240f },
            };
        }
    }
}
