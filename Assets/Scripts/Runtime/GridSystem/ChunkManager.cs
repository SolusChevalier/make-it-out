using System.Collections.Generic;
using MakeItOut.Runtime.MazeGeneration;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager Instance { get; private set; }

        [SerializeField] private Material _opaqueMaterial;
        [SerializeField] private Transform _chunkParent;
        [SerializeField] private int _viewDistanceChunks = 4;
        [SerializeField] private bool _buildMeshesOnInitialise = true;

        private Dictionary<Vector3Int, ChunkData> _chunks;
        private Dictionary<Vector3Int, GameObject> _chunkObjects;
        private MazeGenerator _subscribedGenerator;

        public int ViewDistanceChunks
        {
            get => _viewDistanceChunks;
            set => _viewDistanceChunks = Mathf.Max(0, value);
        }

        public bool BuildMeshesOnInitialise
        {
            get => _buildMeshesOnInitialise;
            set => _buildMeshesOnInitialise = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            TrySubscribeToGenerationEvent();
        }

        private void OnDisable()
        {
            if (_subscribedGenerator == null)
            {
                return;
            }

            _subscribedGenerator.OnGenerationComplete -= HandleGenerationComplete;
            _subscribedGenerator = null;
        }

        public void InitialiseAllChunks()
        {
            EnsureDictionaries();
            _chunks.Clear();
            _chunkObjects.Clear();

            for (int z = 0; z < GridConfig.ChunksPerAxis; z++)
            {
                for (int y = 0; y < GridConfig.ChunksPerAxis; y++)
                {
                    for (int x = 0; x < GridConfig.ChunksPerAxis; x++)
                    {
                        Vector3Int chunkCoord = new Vector3Int(x, y, z);
                        Vector3Int origin = ChunkCoordinateUtility.ChunkOrigin(chunkCoord);

                        ChunkData data = new ChunkData
                        {
                            ChunkCoord = chunkCoord,
                            GridOrigin = origin,
                            WorldOrigin = WorldGrid.Instance.GridToWorld(origin),
                            OpaqueMesh = null,
                            TransparentMesh = null,
                            IsMeshDirty = false,
                            IsActive = false,
                        };

                        GameObject chunkObject = CreateChunkObject(chunkCoord, data.WorldOrigin);
                        AssignChunkMesh(chunkObject, chunkCoord, data);
                        chunkObject.SetActive(false);

                        _chunks.Add(chunkCoord, data);
                        _chunkObjects.Add(chunkCoord, chunkObject);
                    }
                }
            }
        }

        public void UpdateActiveChunks(Vector3Int playerGridPos)
        {
            EnsureDictionaries();

            Vector3Int playerChunk = ChunkCoordinateUtility.GridToChunk(playerGridPos);
            foreach (KeyValuePair<Vector3Int, ChunkData> kvp in _chunks)
            {
                Vector3Int coord = kvp.Key;
                ChunkData data = kvp.Value;

                int dx = Mathf.Abs(coord.x - playerChunk.x);
                int dy = Mathf.Abs(coord.y - playerChunk.y);
                int dz = Mathf.Abs(coord.z - playerChunk.z);

                bool shouldBeActive = dx <= ViewDistanceChunks &&
                                      dy <= ViewDistanceChunks &&
                                      dz <= ViewDistanceChunks;

                if (shouldBeActive == data.IsActive)
                {
                    continue;
                }

                _chunkObjects[coord].SetActive(shouldBeActive);
                data.IsActive = shouldBeActive;
            }
        }

        public ChunkData GetChunk(Vector3Int gridPos)
        {
            EnsureDictionaries();

            Vector3Int chunkCoord = ChunkCoordinateUtility.GridToChunk(gridPos);
            return _chunks.TryGetValue(chunkCoord, out ChunkData data) ? data : null;
        }

        public void RebuildChunkMesh(Vector3Int chunkCoord)
        {
            EnsureDictionaries();
            if (!_chunks.TryGetValue(chunkCoord, out ChunkData data))
            {
                return;
            }

            GameObject chunkObject = _chunkObjects[chunkCoord];
            AssignChunkMesh(chunkObject, chunkCoord, data);
        }

        private GameObject CreateChunkObject(Vector3Int chunkCoord, Vector3 worldOrigin)
        {
            GameObject chunkObject = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");
            chunkObject.transform.SetPositionAndRotation(worldOrigin, Quaternion.identity);
            chunkObject.transform.localScale = Vector3.one;
            chunkObject.isStatic = true;

            if (_chunkParent != null)
            {
                chunkObject.transform.SetParent(_chunkParent, true);
            }
            else
            {
                chunkObject.transform.SetParent(transform, true);
            }

            chunkObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
            if (_opaqueMaterial != null)
            {
                renderer.sharedMaterial = _opaqueMaterial;
            }

            chunkObject.AddComponent<MeshCollider>();
            return chunkObject;
        }

        private void AssignChunkMesh(GameObject chunkObject, Vector3Int chunkCoord, ChunkData data)
        {
            Mesh mesh = _buildMeshesOnInitialise ? ChunkMeshBuilder.BuildChunkMesh(chunkCoord) : new Mesh();
            data.OpaqueMesh = mesh;

            MeshFilter filter = chunkObject.GetComponent<MeshFilter>();
            MeshCollider collider = chunkObject.GetComponent<MeshCollider>();
            filter.sharedMesh = mesh;
            collider.sharedMesh = mesh;

            Physics.BakeMesh(mesh.GetInstanceID(), false);
        }

        private void EnsureDictionaries()
        {
            _chunks ??= new Dictionary<Vector3Int, ChunkData>();
            _chunkObjects ??= new Dictionary<Vector3Int, GameObject>();
        }

        private void TrySubscribeToGenerationEvent()
        {
            if (_subscribedGenerator != null)
            {
                return;
            }

            MazeGenerator generator = FindObjectOfType<MazeGenerator>();
            if (generator == null)
            {
                return;
            }

            generator.OnGenerationComplete += HandleGenerationComplete;
            _subscribedGenerator = generator;
        }

        private void HandleGenerationComplete()
        {
            InitialiseAllChunks();
        }
    }
}
