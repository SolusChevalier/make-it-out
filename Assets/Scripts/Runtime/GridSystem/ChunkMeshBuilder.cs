using System.Collections.Generic;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public static class ChunkMeshBuilder
    {
        private static readonly Vector3Int[] s_faceDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        private static readonly Vector3[][] s_faceVertexOffsets =
        {
            // +X
            new[]
            {
                new Vector3(GridConfig.BlockSize, 0f, 0f),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
            },
            // -X
            new[]
            {
                new Vector3(0f, 0f, GridConfig.BlockSize),
                new Vector3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(0f, GridConfig.BlockSize, 0f),
                new Vector3(0f, 0f, 0f),
            },
            // +Y
            new[]
            {
                new Vector3(0f, GridConfig.BlockSize, 0f),
                new Vector3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
            },
            // -Y
            new[]
            {
                new Vector3(0f, 0f, GridConfig.BlockSize),
                new Vector3(0f, 0f, 0f),
                new Vector3(GridConfig.BlockSize, 0f, 0f),
                new Vector3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
            },
            // +Z
            new[]
            {
                new Vector3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
                new Vector3(0f, 0f, GridConfig.BlockSize),
            },
            // -Z
            new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, GridConfig.BlockSize, 0f),
                new Vector3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
                new Vector3(GridConfig.BlockSize, 0f, 0f),
            },
        };

        public static Mesh BuildChunkMesh(Vector3Int chunkCoord)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Vector3Int origin = ChunkCoordinateUtility.ChunkOrigin(chunkCoord);
            int chunkSize = GridConfig.ChunkSize;

            for (int localZ = 0; localZ < chunkSize; localZ++)
            {
                for (int localY = 0; localY < chunkSize; localY++)
                {
                    for (int localX = 0; localX < chunkSize; localX++)
                    {
                        Vector3Int gridPos = new Vector3Int(origin.x + localX, origin.y + localY, origin.z + localZ);
                        if (!WorldGrid.Instance.InBounds(gridPos))
                        {
                            continue;
                        }

                        if (WorldGrid.Instance.GetBlock(gridPos) == BlockType.Air)
                        {
                            continue;
                        }

                        for (int face = 0; face < s_faceDirections.Length; face++)
                        {
                            Vector3Int neighbourPos = gridPos + s_faceDirections[face];
                            if (WorldGrid.Instance.GetBlock(neighbourPos) != BlockType.Air)
                            {
                                continue;
                            }

                            AddFace(vertices, triangles, gridPos, face);
                        }
                    }
                }
            }

            Mesh mesh = new Mesh
            {
                name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}",
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(List<Vector3> vertices, List<int> triangles, Vector3Int gridPos, int faceIndex)
        {
            Vector3 worldBase = WorldGrid.Instance.GridToWorld(gridPos);
            Vector3[] faceOffsets = s_faceVertexOffsets[faceIndex];
            int startIndex = vertices.Count;

            vertices.Add(worldBase + faceOffsets[0]);
            vertices.Add(worldBase + faceOffsets[1]);
            vertices.Add(worldBase + faceOffsets[2]);
            vertices.Add(worldBase + faceOffsets[3]);

            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 0);
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 3);
        }
    }
}
