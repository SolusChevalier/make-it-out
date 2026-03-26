using System.Collections.Generic;
using MakeItOut.Runtime.GridSystem;
using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    public sealed class TransparencyManager : MonoBehaviour
    {
        [Header("Transparency")]
        [Range(0f, 1f)]
        [SerializeField] private float _occluderAlpha = 0.15f;

        private MaterialPropertyBlock _mpb;
        private List<Renderer> _currentlyTransparent;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _currentlyTransparent = new List<Renderer>();
        }

        public void UpdateTransparency(Vector3Int playerGridPos, Quaternion cameraOrientation)
        {
            if (_mpb == null)
            {
                Debug.LogError("TransparencyManager: _mpb is null. Ensure Awake has run.");
                return;
            }

            RestorePreviouslyTransparentRenderers();

            if (WorldGrid.Instance == null || ChunkManager.Instance == null)
            {
                return;
            }

            Vector3 viewForward = cameraOrientation * Vector3.forward;
            Vector3Int viewAxis = Vector3Int.RoundToInt(viewForward);
            int steps = GridConfig.ChunkSize * Mathf.Max(1, ChunkManager.Instance.ViewDistanceChunks);

            for (int i = 1; i <= steps; i++)
            {
                Vector3Int candidate = playerGridPos + viewAxis * i;

                if (!WorldGrid.Instance.InBounds(candidate))
                {
                    break;
                }

                if (!WorldGrid.Instance.IsSolid(candidate))
                {
                    continue;
                }

                ChunkData chunk = ChunkManager.Instance.GetChunk(candidate);
                if (chunk == null || !chunk.IsActive)
                {
                    continue;
                }

                GameObject chunkObject = ChunkManager.Instance.GetChunkObject(chunk.ChunkCoord);
                if (chunkObject == null)
                {
                    continue;
                }

                Renderer renderer = chunkObject.GetComponent<Renderer>();
                if (renderer == null || _currentlyTransparent.Contains(renderer))
                {
                    continue;
                }

                _mpb.Clear();
                renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_Alpha", _occluderAlpha);
                renderer.SetPropertyBlock(_mpb);
                _currentlyTransparent.Add(renderer);
            }
        }

        private void RestorePreviouslyTransparentRenderers()
        {
            for (int i = 0; i < _currentlyTransparent.Count; i++)
            {
                Renderer renderer = _currentlyTransparent[i];
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }

            _currentlyTransparent.Clear();
        }
    }
}
