using System.Collections.Generic;
using System;
using System.Collections;
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

        public void InitialiseAllChunks()
        {
            EnsureDictionaries();
            int chunksPerAxis = GridSession.ChunksPerAxis;
            int totalChunks = chunksPerAxis * chunksPerAxis * chunksPerAxis;
            int totalCells = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;
            float gridMegabytes = (totalCells * 2f) / (1024f * 1024f);
            Debug.Log($"ChunkManager: initialising {totalChunks} chunks for GridSize {GridSession.GridSize} (grid backing ~{gridMegabytes:F1} MB).");

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

        public IEnumerator InitialiseAllChunksCoroutine(Action<float> onProgress)
        {
            EnsureDictionaries();

            onProgress?.Invoke(0f);
            yield return null;

            // Keep the loading panel responsive before heavy work.
            ChunkMeshBuilder.BuildAllChunksImmediate(this);

            onProgress?.Invoke(1f);
            yield return null;
        }

        public void Clear()
        {
            EnsureDictionaries();
            foreach (KeyValuePair<Vector3Int, GameObject> kvp in _chunkObjects)
            {
                if (kvp.Value == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(kvp.Value);
                else
                    DestroyImmediate(kvp.Value);
            }

            _chunks.Clear();
            _chunkObjects.Clear();

            Debug.Log("ChunkManager cleared.");
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

        public void ForEachChunk(Action<ChunkData, GameObject> callback)
        {
            if (callback == null)
            {
                return;
            }

            EnsureDictionaries();
            foreach (KeyValuePair<Vector3Int, ChunkData> kvp in _chunks)
            {
                if (_chunkObjects.TryGetValue(kvp.Key, out GameObject obj) && obj != null)
                {
                    callback(kvp.Value, obj);
                }
            }
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
            Clear();
        }

        private void EnsureDictionaries()
        {
            _chunks ??= new Dictionary<Vector3Int, ChunkData>();
            _chunkObjects ??= new Dictionary<Vector3Int, GameObject>();
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
