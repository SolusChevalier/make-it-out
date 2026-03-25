using System.Collections.Generic;
using System.Linq;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class MazeGeneratorSystemTests
    {
        [Test]
        public void Generation_IsReproducible_WithSameSeed()
        {
            GameObject go = new GameObject("MazeGeneratorTests");
            MazeGenerator generator = go.AddComponent<MazeGenerator>();

            MazeGenerationSnapshot first = generator.GenerateSynchronouslyForTests(1337);
            MazeGenerationSnapshot second = generator.GenerateSynchronouslyForTests(1337);

            Assert.IsTrue(first.BlockGrid.SequenceEqual(second.BlockGrid));
            Assert.IsTrue(first.FeatureGrid.SequenceEqual(second.FeatureGrid));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Generation_MeetsAcceptanceConstraints()
        {
            GameObject go = new GameObject("MazeGeneratorTests");
            MazeGenerator generator = go.AddComponent<MazeGenerator>();
            MazeGenerationSnapshot snapshot = generator.GenerateSynchronouslyForTests(2048);

            int size = GridConfig.GridSize;
            int c = size / 2;
            Vector3Int centre = new Vector3Int(c, c, c);
            Vector3Int[] centreRoom =
            {
                centre,
                centre + Vector3Int.right,
                centre + Vector3Int.left,
                centre + Vector3Int.up,
                centre + Vector3Int.down,
                centre + new Vector3Int(0, 0, 1),
                centre + new Vector3Int(0, 0, -1),
            };

            foreach (Vector3Int cell in centreRoom)
            {
                Assert.AreEqual(BlockType.Air, snapshot.BlockGrid[GridIndex.ToIndex(cell.x, cell.y, cell.z)]);
            }

            Assert.AreEqual(3, CountFeatures(snapshot.FeatureGrid, FeatureType.Exit));
            Assert.AreEqual(3, snapshot.ExitWorldPositions.Count);
            Assert.IsTrue(AllExitsOnDifferentFaces(snapshot.BlockGrid, snapshot.FeatureGrid));
            Assert.IsTrue(IsBoundarySolidExceptExits(snapshot.BlockGrid, snapshot.FeatureGrid));
            Assert.IsTrue(AllAirCellsReachable(snapshot.BlockGrid, centre));

            Object.DestroyImmediate(go);
        }

        private static int CountFeatures(byte[] featureGrid, byte feature)
        {
            int count = 0;
            for (int i = 0; i < featureGrid.Length; i++)
            {
                if (featureGrid[i] == feature)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool AllExitsOnDifferentFaces(byte[] blockGrid, byte[] featureGrid)
        {
            HashSet<int> faces = new HashSet<int>();

            for (int i = 0; i < featureGrid.Length; i++)
            {
                if (featureGrid[i] != FeatureType.Exit)
                {
                    continue;
                }

                Vector3Int pos = GridIndex.FromIndex(i);
                if (blockGrid[i] != BlockType.Air)
                {
                    return false;
                }

                int face = GetFaceId(pos);
                if (face < 0 || faces.Contains(face))
                {
                    return false;
                }

                faces.Add(face);
            }

            return faces.Count == 3;
        }

        private static bool IsBoundarySolidExceptExits(byte[] blockGrid, byte[] featureGrid)
        {
            int size = GridConfig.GridSize;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (!IsBoundary(x, y, z))
                        {
                            continue;
                        }

                        int idx = GridIndex.ToIndex(x, y, z);
                        bool isExit = featureGrid[idx] == FeatureType.Exit;
                        if (!isExit && blockGrid[idx] != BlockType.Solid)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool AllAirCellsReachable(byte[] blockGrid, Vector3Int start)
        {
            int size = GridConfig.GridSize;
            bool[] visited = new bool[blockGrid.Length];
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            int startIndex = GridIndex.ToIndex(start.x, start.y, start.z);
            queue.Enqueue(start);
            visited[startIndex] = true;

            Vector3Int[] dirs =
            {
                Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down,
                new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1),
            };

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                foreach (Vector3Int dir in dirs)
                {
                    Vector3Int next = current + dir;
                    if (next.x < 0 || next.x >= size || next.y < 0 || next.y >= size || next.z < 0 || next.z >= size)
                    {
                        continue;
                    }

                    int nextIndex = GridIndex.ToIndex(next.x, next.y, next.z);
                    if (visited[nextIndex] || blockGrid[nextIndex] != BlockType.Air)
                    {
                        continue;
                    }

                    visited[nextIndex] = true;
                    queue.Enqueue(next);
                }
            }

            for (int i = 0; i < blockGrid.Length; i++)
            {
                if (blockGrid[i] == BlockType.Air && !visited[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBoundary(int x, int y, int z)
        {
            int max = GridConfig.GridSize - 1;
            return x == 0 || y == 0 || z == 0 || x == max || y == max || z == max;
        }

        private static int GetFaceId(Vector3Int pos)
        {
            int max = GridConfig.GridSize - 1;
            if (pos.x == 0) return 0;
            if (pos.x == max) return 1;
            if (pos.y == 0) return 2;
            if (pos.y == max) return 3;
            if (pos.z == 0) return 4;
            if (pos.z == max) return 5;
            return -1;
        }
    }
}
