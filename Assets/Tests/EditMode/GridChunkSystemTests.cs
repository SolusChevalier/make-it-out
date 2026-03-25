using MakeItOut.Runtime.GridSystem;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class GridChunkSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            WorldGrid.Instance.ResetForGeneration();
        }

        [Test]
        public void GetBlock_ReturnsSolid_ForOutOfBounds()
        {
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(-1, 0, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, -1, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, 0, -1));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(GridConfig.GridSize, 0, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, GridConfig.GridSize, 0));
            Assert.AreEqual(BlockType.Solid, WorldGrid.Instance.GetBlock(0, 0, GridConfig.GridSize));
        }

        [Test]
        public void WorldToGrid_GridToWorld_RoundTrip_ForAllValidCells()
        {
            for (int z = 0; z < GridConfig.GridSize; z++)
            {
                for (int y = 0; y < GridConfig.GridSize; y++)
                {
                    for (int x = 0; x < GridConfig.GridSize; x++)
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
            int total = GridConfig.GridSize * GridConfig.GridSize * GridConfig.GridSize;
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
            int c = GridConfig.GridSize / 2;
            WorldGrid.Instance.SetBlock(c - 1, c, c, BlockType.Solid);
            WorldGrid.Instance.SetBlock(c, c, c, BlockType.Solid);

            Vector3Int chunk = ChunkCoordinateUtility.GridToChunk(new Vector3Int(c - 1, c, c));
            Mesh mesh = ChunkMeshBuilder.BuildChunkMesh(chunk);

            const int expectedVisibleFaces = 5;
            const int indicesPerFace = 6;
            Assert.AreEqual(expectedVisibleFaces * indicesPerFace, mesh.triangles.Length);
        }

        [Test]
        public void ChunkManager_ActivatesNearbyChunks_AndDeactivatesFarChunks()
        {
            GameObject chunkRoot = new GameObject("ChunkManagerTest");
            ChunkManager manager = chunkRoot.AddComponent<ChunkManager>();
            manager.BuildMeshesOnInitialise = false;
            manager.InitialiseAllChunks();

            Vector3Int playerNearOrigin = Vector3Int.zero;
            manager.UpdateActiveChunks(playerNearOrigin);

            ChunkData nearChunk = manager.GetChunk(playerNearOrigin);
            Assert.IsNotNull(nearChunk);
            Assert.IsTrue(nearChunk.IsActive);

            Vector3Int farGridPos = new Vector3Int(
                GridConfig.GridSize - 1,
                GridConfig.GridSize - 1,
                GridConfig.GridSize - 1);

            ChunkData farChunk = manager.GetChunk(farGridPos);
            Assert.IsNotNull(farChunk);
            Assert.IsFalse(farChunk.IsActive);

            Object.DestroyImmediate(chunkRoot);
        }
    }
}
