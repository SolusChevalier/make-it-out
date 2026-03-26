using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public static class ChunkMeshBuilder
    {
        public static readonly Vector3Int[] FaceDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        public static readonly float3[][] FaceVertexOffsets =
        {
            // +X
            new[]
            {
                new float3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
                new float3(GridConfig.BlockSize, 0f, 0f),
            },
            // -X
            new[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, GridConfig.BlockSize, 0f),
                new float3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
                new float3(0f, 0f, GridConfig.BlockSize),
            },
            // +Y
            new[]
            {
                new float3(0f, GridConfig.BlockSize, 0f),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new float3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
            },
            // -Y
            new[]
            {
                new float3(0f, 0f, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, 0f, 0f),
                new float3(0f, 0f, 0f),
            },
            // +Z
            new[]
            {
                new float3(0f, 0f, GridConfig.BlockSize),
                new float3(0f, GridConfig.BlockSize, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, GridConfig.BlockSize),
                new float3(GridConfig.BlockSize, 0f, GridConfig.BlockSize),
            },
            // -Z
            new[]
            {
                new float3(GridConfig.BlockSize, 0f, 0f),
                new float3(GridConfig.BlockSize, GridConfig.BlockSize, 0f),
                new float3(0f, GridConfig.BlockSize, 0f),
                new float3(0f, 0f, 0f),
            },
        };

        public static readonly float3[] FaceNormals =
        {
            new float3(1f, 0f, 0f),
            new float3(-1f, 0f, 0f),
            new float3(0f, 1f, 0f),
            new float3(0f, -1f, 0f),
            new float3(0f, 0f, 1f),
            new float3(0f, 0f, -1f),
        };

        private static readonly int3[] s_faceDirectionInts =
        {
            new int3(1, 0, 0), new int3(-1, 0, 0), new int3(0, 1, 0),
            new int3(0, -1, 0), new int3(0, 0, 1), new int3(0, 0, -1),
        };

        private static readonly float3[] s_faceVertexOffsetsFlat =
        {
            FaceVertexOffsets[0][0], FaceVertexOffsets[0][1], FaceVertexOffsets[0][2], FaceVertexOffsets[0][3],
            FaceVertexOffsets[1][0], FaceVertexOffsets[1][1], FaceVertexOffsets[1][2], FaceVertexOffsets[1][3],
            FaceVertexOffsets[2][0], FaceVertexOffsets[2][1], FaceVertexOffsets[2][2], FaceVertexOffsets[2][3],
            FaceVertexOffsets[3][0], FaceVertexOffsets[3][1], FaceVertexOffsets[3][2], FaceVertexOffsets[3][3],
            FaceVertexOffsets[4][0], FaceVertexOffsets[4][1], FaceVertexOffsets[4][2], FaceVertexOffsets[4][3],
            FaceVertexOffsets[5][0], FaceVertexOffsets[5][1], FaceVertexOffsets[5][2], FaceVertexOffsets[5][3],
        };

        public static Mesh BuildChunkMesh(Vector3Int chunkCoord)
        {
            byte[] blockCopy = WorldGrid.Instance.CopyBlockGrid();
            int totalCells = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;
            NativeArray<byte> native = new NativeArray<byte>(totalCells, Allocator.TempJob);
            native.CopyFrom(blockCopy);
            Mesh mesh = BuildSingleChunkFromNative(chunkCoord, native);
            native.Dispose();
            return mesh;
        }

        public static void BuildAllChunksImmediate(ChunkManager manager)
        {
            BuildAllChunksInternal(manager, false);
        }

        public static IEnumerator BuildAllChunksCoroutine(ChunkManager manager)
        {
            BuildAllChunksInternal(manager, true);
            yield break;
        }

        private static void BuildAllChunksInternal(ChunkManager manager, bool asyncBake)
        {
            int chunksPerAxis = GridSession.ChunksPerAxis;
            int totalChunks = chunksPerAxis * chunksPerAxis * chunksPerAxis;
            byte[] blockGridManaged = WorldGrid.Instance.CopyBlockGrid();
            int totalCells = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;
            NativeArray<byte> blockGridNative = new NativeArray<byte>(totalCells, Allocator.TempJob);
            blockGridNative.CopyFrom(blockGridManaged);

            NativeList<float3>[] chunkVertices = new NativeList<float3>[totalChunks];
            NativeList<int>[] chunkTriangles = new NativeList<int>[totalChunks];

            NativeArray<int3> faceDirections = new NativeArray<int3>(s_faceDirectionInts, Allocator.TempJob);
            NativeArray<float3> faceVertexOffsets = new NativeArray<float3>(s_faceVertexOffsetsFlat, Allocator.TempJob);

            try
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    chunkVertices[i] = new NativeList<float3>(Allocator.TempJob);
                    chunkTriangles[i] = new NativeList<int>(Allocator.TempJob);
                }

                NativeArray<JobHandle> handles = new NativeArray<JobHandle>(totalChunks, Allocator.Temp);
                for (int i = 0; i < totalChunks; i++)
                {
                    Vector3Int chunkCoord = ChunkIndexToCoord(i, chunksPerAxis);
                    handles[i] = new BuildChunkMeshDataJob
                    {
                        BlockGrid = blockGridNative,
                        Vertices = chunkVertices[i],
                        Triangles = chunkTriangles[i],
                        GridSize = GridSession.GridSize,
                        ChunkSize = GridConfig.ChunkSize,
                        ChunkX = chunkCoord.x,
                        ChunkY = chunkCoord.y,
                        ChunkZ = chunkCoord.z,
                        FaceDirections = faceDirections,
                        FaceVertexOffsets = faceVertexOffsets,
                    }.Schedule();
                }

                JobHandle.CompleteAll(handles);
                handles.Dispose();
                manager.ReportLoadingProgress(0.6f);

                Mesh[] meshes = BuildUnityMeshes(chunkVertices, chunkTriangles, totalChunks);
                manager.ReportLoadingProgress(0.8f);

                manager.ClearRegisteredChunks();

                List<int> meshIds = new List<int>();
                List<MeshCollider> colliders = new List<MeshCollider>();
                List<Mesh> colliderMeshes = new List<Mesh>();
                for (int i = 0; i < totalChunks; i++)
                {
                    if (meshes[i] == null || meshes[i].vertexCount == 0)
                    {
                        continue;
                    }

                    Vector3Int chunkCoord = ChunkIndexToCoord(i, chunksPerAxis);
                    GameObject chunkObj = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
                    chunkObj.isStatic = true;
                    chunkObj.transform.SetParent(manager.transform, false);
                    chunkObj.transform.position = Vector3.zero;

                    MeshFilter filter = chunkObj.AddComponent<MeshFilter>();
                    MeshRenderer renderer = chunkObj.AddComponent<MeshRenderer>();
                    MeshCollider collider = chunkObj.AddComponent<MeshCollider>();

                    filter.sharedMesh = meshes[i];
                    renderer.sharedMaterial = manager.BlockMaterial;
                    collider.sharedMesh = meshes[i];

                    meshIds.Add(meshes[i].GetInstanceID());
                    colliders.Add(collider);
                    colliderMeshes.Add(meshes[i]);

                    ChunkData data = new ChunkData
                    {
                        ChunkCoord = chunkCoord,
                        GridOrigin = ChunkCoordinateUtility.ChunkOrigin(chunkCoord),
                        WorldOrigin = WorldGrid.Instance.GridToWorld(ChunkCoordinateUtility.ChunkOrigin(chunkCoord)),
                        OpaqueMesh = meshes[i],
                        TransparentMesh = null,
                        IsMeshDirty = false,
                        IsActive = true,
                    };

                    manager.RegisterChunk(chunkCoord, data, chunkObj);
                }

                if (asyncBake)
                {
                    Task task = Task.Run(() =>
                    {
                        for (int i = 0; i < meshIds.Count; i++)
                        {
                            Physics.BakeMesh(meshIds[i], false);
                        }
                    });
                    task.Wait();
                }
                else
                {
                    for (int i = 0; i < meshIds.Count; i++)
                    {
                        Physics.BakeMesh(meshIds[i], false);
                    }
                }

                for (int i = 0; i < colliders.Count; i++)
                {
                    colliders[i].sharedMesh = colliderMeshes[i];
                }

                manager.RebuildFeaturePropInstances();
                manager.UpdateActiveChunks(WorldGrid.Instance.GetCentreCell());
                manager.ReportLoadingProgress(0.95f);
            }
            finally
            {
                for (int i = 0; i < chunkVertices.Length; i++)
                {
                    if (chunkVertices[i].IsCreated)
                    {
                        chunkVertices[i].Dispose();
                    }

                    if (chunkTriangles[i].IsCreated)
                    {
                        chunkTriangles[i].Dispose();
                    }
                }

                if (blockGridNative.IsCreated)
                {
                    blockGridNative.Dispose();
                }

                if (faceDirections.IsCreated)
                {
                    faceDirections.Dispose();
                }

                if (faceVertexOffsets.IsCreated)
                {
                    faceVertexOffsets.Dispose();
                }
            }
        }

        private static Mesh[] BuildUnityMeshes(NativeList<float3>[] chunkVertices, NativeList<int>[] chunkTriangles, int totalChunks)
        {
            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(totalChunks);
            for (int i = 0; i < totalChunks; i++)
            {
                Mesh.MeshData data = meshDataArray[i];
                int vertexCount = chunkVertices[i].Length;
                int indexCount = chunkTriangles[i].Length;
                if (vertexCount == 0)
                {
                    continue;
                }

                data.SetVertexBufferParams(
                    vertexCount,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0));

                NativeArray<VertexPositionNormal> meshVertices = data.GetVertexData<VertexPositionNormal>(0);
                for (int v = 0; v < vertexCount; v += 4)
                {
                    float3 p0 = chunkVertices[i][v];
                    float3 p1 = chunkVertices[i][v + 1];
                    float3 p2 = chunkVertices[i][v + 2];

                    float3 edgeA = p1 - p0;
                    float3 edgeB = p2 - p0;
                    float3 normal = math.normalize(math.cross(edgeA, edgeB));

                    meshVertices[v + 0] = new VertexPositionNormal { Position = p0, Normal = normal };
                    meshVertices[v + 1] = new VertexPositionNormal { Position = p1, Normal = normal };
                    meshVertices[v + 2] = new VertexPositionNormal { Position = p2, Normal = normal };
                    meshVertices[v + 3] = new VertexPositionNormal { Position = chunkVertices[i][v + 3], Normal = normal };
                }

                data.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
                NativeArray<int> meshIndices = data.GetIndexData<int>();
                for (int t = 0; t < indexCount; t++)
                {
                    meshIndices[t] = chunkTriangles[i][t];
                }

                data.subMeshCount = 1;
                data.SetSubMesh(0, new SubMeshDescriptor(0, indexCount));
            }

            Mesh[] meshes = new Mesh[totalChunks];
            for (int i = 0; i < totalChunks; i++)
            {
                meshes[i] = new Mesh { name = $"Chunk_{i}" };
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshes);

            for (int i = 0; i < totalChunks; i++)
            {
                if (meshes[i].vertexCount > 0)
                {
                    meshes[i].RecalculateBounds();
                }
            }

            return meshes;
        }

        private static Mesh BuildSingleChunkFromNative(Vector3Int chunkCoord, NativeArray<byte> blockGridNative)
        {
            NativeList<float3> vertices = new NativeList<float3>(Allocator.Temp);
            NativeList<int> triangles = new NativeList<int>(Allocator.Temp);
            NativeArray<int3> faceDirections = new NativeArray<int3>(s_faceDirectionInts, Allocator.Temp);
            NativeArray<float3> faceVertexOffsets = new NativeArray<float3>(s_faceVertexOffsetsFlat, Allocator.Temp);

            try
            {
                BuildChunkMeshDataJob job = new BuildChunkMeshDataJob
                {
                    BlockGrid = blockGridNative,
                    Vertices = vertices,
                    Triangles = triangles,
                    GridSize = GridSession.GridSize,
                    ChunkSize = GridConfig.ChunkSize,
                    ChunkX = chunkCoord.x,
                    ChunkY = chunkCoord.y,
                    ChunkZ = chunkCoord.z,
                    FaceDirections = faceDirections,
                    FaceVertexOffsets = faceVertexOffsets,
                };

                job.Execute();

                Mesh mesh = new Mesh { name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}" };
                if (vertices.Length == 0)
                {
                    return mesh;
                }

                List<Vector3> managedVertices = new List<Vector3>(vertices.Length);
                List<int> managedTriangles = new List<int>(triangles.Length);
                for (int i = 0; i < vertices.Length; i++)
                {
                    managedVertices.Add(vertices[i]);
                }

                for (int i = 0; i < triangles.Length; i++)
                {
                    managedTriangles.Add(triangles[i]);
                }

                mesh.SetVertices(managedVertices);
                mesh.SetTriangles(managedTriangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
            finally
            {
                vertices.Dispose();
                triangles.Dispose();
                faceDirections.Dispose();
                faceVertexOffsets.Dispose();
            }
        }

        public static Vector3Int ChunkIndexToCoord(int index, int chunksPerAxis)
        {
            int x = index % chunksPerAxis;
            int y = (index / chunksPerAxis) % chunksPerAxis;
            int z = index / (chunksPerAxis * chunksPerAxis);
            return new Vector3Int(x, y, z);
        }

        private struct VertexPositionNormal
        {
            public float3 Position;
            public float3 Normal;
        }

        [BurstCompile]
        private struct BuildChunkMeshDataJob : IJob
        {
            [ReadOnly] public NativeArray<byte> BlockGrid;
            public NativeList<float3> Vertices;
            public NativeList<int> Triangles;

            public int GridSize;
            public int ChunkSize;
            public int ChunkX;
            public int ChunkY;
            public int ChunkZ;

            [ReadOnly] public NativeArray<int3> FaceDirections;
            [ReadOnly] public NativeArray<float3> FaceVertexOffsets;

            public void Execute()
            {
                int originX = ChunkX * ChunkSize;
                int originY = ChunkY * ChunkSize;
                int originZ = ChunkZ * ChunkSize;

                for (int x = originX; x < originX + ChunkSize; x++)
                {
                    for (int y = originY; y < originY + ChunkSize; y++)
                    {
                        for (int z = originZ; z < originZ + ChunkSize; z++)
                        {
                            if (x >= GridSize || y >= GridSize || z >= GridSize)
                            {
                                continue;
                            }

                            int cellIndex = ToIndex(x, y, z);
                            if (BlockGrid[cellIndex] != BlockType.Solid)
                            {
                                continue;
                            }

                            float3 worldOrigin = new float3(
                                x * GridConfig.BlockSize,
                                y * GridConfig.BlockSize,
                                z * GridConfig.BlockSize);

                            for (int faceIndex = 0; faceIndex < FaceDirections.Length; faceIndex++)
                            {
                                int3 direction = FaceDirections[faceIndex];
                                int nx = x + direction.x;
                                int ny = y + direction.y;
                                int nz = z + direction.z;

                                bool outOfBounds = nx < 0 || nx >= GridSize ||
                                                   ny < 0 || ny >= GridSize ||
                                                   nz < 0 || nz >= GridSize;
                                if (!outOfBounds && BlockGrid[ToIndex(nx, ny, nz)] != BlockType.Air)
                                {
                                    continue;
                                }

                                int baseVertex = Vertices.Length;
                                int offset = faceIndex * 4;
                                Vertices.Add(worldOrigin + FaceVertexOffsets[offset + 0]);
                                Vertices.Add(worldOrigin + FaceVertexOffsets[offset + 1]);
                                Vertices.Add(worldOrigin + FaceVertexOffsets[offset + 2]);
                                Vertices.Add(worldOrigin + FaceVertexOffsets[offset + 3]);

                                Triangles.Add(baseVertex + 0);
                                Triangles.Add(baseVertex + 1);
                                Triangles.Add(baseVertex + 2);
                                Triangles.Add(baseVertex + 0);
                                Triangles.Add(baseVertex + 2);
                                Triangles.Add(baseVertex + 3);
                            }
                        }
                    }
                }
            }

            private int ToIndex(int x, int y, int z)
            {
                return x + GridSize * (y + GridSize * z);
            }
        }

    }
}
