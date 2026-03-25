using System.Collections.Generic;
using MakeItOut.Runtime.MazeGeneration;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public class ChunkManager : MonoBehaviour
    {
        public static ChunkManager Instance { get; private set; }

        [SerializeField] private Material _blockMaterial;
        [SerializeField] private FeaturePropRenderer _featurePropRenderer;
        [SerializeField] private int _viewDistanceChunks = 4;

        private Dictionary<Vector3Int, ChunkData> _chunks;
        private Dictionary<Vector3Int, GameObject> _chunkObjects;
        private MazeGenerator _subscribedGenerator;
        private Coroutine _initialiseRoutine;

        public int ViewDistanceChunks
        {
            get => _viewDistanceChunks;
            set => _viewDistanceChunks = Mathf.Max(0, value);
        }

        public Material BlockMaterial => _blockMaterial;

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
            if (Application.isPlaying)
            {
                if (_initialiseRoutine != null)
                {
                    StopCoroutine(_initialiseRoutine);
                }

                _initialiseRoutine = StartCoroutine(ChunkMeshBuilder.BuildAllChunksCoroutine(this));
                return;
            }

            ChunkMeshBuilder.BuildAllChunksImmediate(this);
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

        public GameObject GetChunkObject(Vector3Int chunkCoord)
        {
            EnsureDictionaries();
            _chunkObjects.TryGetValue(chunkCoord, out GameObject obj);
            return obj;
        }

        public void RebuildChunkMesh(Vector3Int chunkCoord)
        {
            Debug.Log($"RebuildChunkMesh called for {chunkCoord} - stub, implement in System 5");
        }

        public void RegisterChunk(Vector3Int chunkCoord, ChunkData data, GameObject obj)
        {
            EnsureDictionaries();
            _chunks[chunkCoord] = data;
            _chunkObjects[chunkCoord] = obj;
        }

        public void ClearRegisteredChunks()
        {
            EnsureDictionaries();
            foreach (GameObject chunkObject in _chunkObjects.Values)
            {
                if (chunkObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(chunkObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(chunkObject);
                    }
                }
            }

            _chunks.Clear();
            _chunkObjects.Clear();
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

        public void RebuildFeaturePropInstances()
        {
            if (_featurePropRenderer == null)
            {
                return;
            }

            _featurePropRenderer.BuildInstanceData();
        }

        public void ReportLoadingProgress(float value)
        {
            if (MazeGenerator.Instance != null)
            {
                MazeGenerator.Instance.ReportProgress(value);
            }
        }
    }
}
