using System.Collections.Generic;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public class FeaturePropRenderer : MonoBehaviour
    {
        [SerializeField] private Mesh _ladderMesh;
        [SerializeField] private Mesh _stairMesh;
        [SerializeField] private Mesh _exitMesh;

        [SerializeField] private Material _ladderMaterial;
        [SerializeField] private Material _stairMaterial;
        [SerializeField] private Material _exitMaterial;

        private readonly List<Matrix4x4[]> _ladderBatches = new List<Matrix4x4[]>();
        private readonly List<Matrix4x4[]> _stairBatches = new List<Matrix4x4[]>();
        private readonly List<Matrix4x4[]> _exitBatches = new List<Matrix4x4[]>();
        private readonly MaterialPropertyBlock _exitMpb = new MaterialPropertyBlock();
        private static readonly int s_emissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            ValidateMaterials();
        }

        public void BuildInstanceData()
        {
            _ladderBatches.Clear();
            _stairBatches.Clear();
            _exitBatches.Clear();

            List<Matrix4x4> ladderMatrices = new List<Matrix4x4>();
            List<Matrix4x4> stairMatrices = new List<Matrix4x4>();
            List<Matrix4x4> exitMatrices = new List<Matrix4x4>();

            Vector3 uniformScale = Vector3.one * GridConfig.BlockSize;
            for (int z = 0; z < GridSession.GridSize; z++)
            {
                for (int y = 0; y < GridSession.GridSize; y++)
                {
                    for (int x = 0; x < GridSession.GridSize; x++)
                    {
                        byte feature = WorldGrid.Instance.GetFeature(x, y, z);
                        if (feature == FeatureType.None)
                        {
                            continue;
                        }

                        Vector3 worldPos = WorldGrid.Instance.GridToWorld(x, y, z);
                        Matrix4x4 matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, uniformScale);

                        if (feature == FeatureType.Ladder)
                        {
                            ladderMatrices.Add(matrix);
                        }
                        else if (feature == FeatureType.Stair)
                        {
                            stairMatrices.Add(matrix);
                        }
                        else if (feature == FeatureType.Exit)
                        {
                            exitMatrices.Add(matrix);
                        }
                    }
                }
            }

            BuildBatches(ladderMatrices, _ladderBatches);
            BuildBatches(stairMatrices, _stairBatches);
            BuildBatches(exitMatrices, _exitBatches);
        }

        private void Update()
        {
            DrawBatches(_ladderMesh, _ladderMaterial, _ladderBatches);
            DrawBatches(_stairMesh, _stairMaterial, _stairBatches);
            DrawExitBatches();
        }

        private static void BuildBatches(List<Matrix4x4> source, List<Matrix4x4[]> batches)
        {
            const int maxBatchSize = 1023;
            int offset = 0;
            while (offset < source.Count)
            {
                int count = Mathf.Min(maxBatchSize, source.Count - offset);
                Matrix4x4[] batch = new Matrix4x4[count];
                source.CopyTo(offset, batch, 0, count);
                batches.Add(batch);
                offset += count;
            }
        }

        private static void DrawBatches(Mesh mesh, Material material, List<Matrix4x4[]> batches)
        {
            if (mesh == null || material == null)
            {
                return;
            }

            for (int i = 0; i < batches.Count; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, batches[i]);
            }
        }

        private void DrawExitBatches()
        {
            if (_exitMesh == null || _exitMaterial == null)
                return;

            float pulse = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
            Color emission = new Color(0.2f, 1f, 0.4f) * Mathf.Lerp(0.5f, 2.5f, pulse);
            _exitMpb.SetColor(s_emissionColorId, emission);

            for (int i = 0; i < _exitBatches.Count; i++)
                Graphics.DrawMeshInstanced(_exitMesh, 0, _exitMaterial, _exitBatches[i], _exitBatches[i].Length, _exitMpb);
        }

        private void ValidateMaterials()
        {
            if (_ladderMaterial != null && !_ladderMaterial.enableInstancing)
            {
                Debug.LogError("FeaturePropRenderer: LadderMaterial does not have GPU instancing enabled.");
            }

            if (_stairMaterial != null && !_stairMaterial.enableInstancing)
            {
                Debug.LogError("FeaturePropRenderer: StairMaterial does not have GPU instancing enabled.");
            }

            if (_exitMaterial != null && !_exitMaterial.enableInstancing)
            {
                Debug.LogError("FeaturePropRenderer: ExitMaterial does not have GPU instancing enabled.");
            }
        }
    }
}
