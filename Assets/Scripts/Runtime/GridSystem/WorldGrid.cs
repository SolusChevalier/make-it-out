using System.Collections.Generic;
using System;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public sealed class WorldGrid
    {
        private static WorldGrid s_instance;

        private byte[] _blockGrid;
        private byte[] _featureGrid;

        public static WorldGrid Instance => s_instance ??= new WorldGrid();

        private WorldGrid() { }

        public void Initialise(int gridSize)
        {
            if (_blockGrid != null)
            {
                _blockGrid = null;
                _featureGrid = null;
            }

            int total = gridSize * gridSize * gridSize;
            _blockGrid = new byte[total];
            _featureGrid = new byte[total];

            float megabytes = (total * 2f) / (1024f * 1024f);
            Debug.Log($"WorldGrid: allocating {total} cells ({megabytes:F1} MB) for GridSize {gridSize}");
            if (megabytes > 8f)
                Debug.LogWarning("WorldGrid: allocation exceeds 8 MB. Consider reducing GridSize.");
        }

        public byte GetBlock(int x, int y, int z)
        {
            AssertInitialised();
            if (!InBounds(x, y, z))
            {
                return BlockType.Solid;
            }

            return _blockGrid[GridIndex.ToIndex(x, y, z)];
        }

        public byte GetBlock(Vector3Int gridPos)
        {
            AssertInitialised();
            return GetBlock(gridPos.x, gridPos.y, gridPos.z);
        }

        public byte GetFeature(int x, int y, int z)
        {
            AssertInitialised();
            if (!InBounds(x, y, z))
            {
                return FeatureType.None;
            }

            return _featureGrid[GridIndex.ToIndex(x, y, z)];
        }

        public byte GetFeature(Vector3Int gridPos)
        {
            AssertInitialised();
            return GetFeature(gridPos.x, gridPos.y, gridPos.z);
        }

        public void SetBlock(int x, int y, int z, byte value)
        {
            AssertInitialised();
            if (!InBounds(x, y, z))
            {
                return;
            }

            int index = GridIndex.ToIndex(x, y, z);
            _blockGrid[index] = value;

            if (value == BlockType.Solid)
            {
                _featureGrid[index] = FeatureType.None;
            }
        }

        public void SetFeature(int x, int y, int z, byte value)
        {
            AssertInitialised();
            if (!InBounds(x, y, z))
            {
                return;
            }

            if (GetBlock(x, y, z) == BlockType.Solid)
            {
                return;
            }

            _featureGrid[GridIndex.ToIndex(x, y, z)] = value;
        }

        public bool InBounds(int x, int y, int z)
        {
            AssertInitialised();
            return x >= 0 && x < GridSession.GridSize &&
                   y >= 0 && y < GridSession.GridSize &&
                   z >= 0 && z < GridSession.GridSize;
        }

        public bool InBounds(Vector3Int gridPos)
        {
            AssertInitialised();
            return InBounds(gridPos.x, gridPos.y, gridPos.z);
        }

        public Vector3Int WorldToGrid(Vector3 worldPos)
        {
            AssertInitialised();
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / GridConfig.BlockSize),
                Mathf.RoundToInt(worldPos.y / GridConfig.BlockSize),
                Mathf.RoundToInt(worldPos.z / GridConfig.BlockSize));
        }

        public Vector3 GridToWorld(Vector3Int gridPos)
        {
            AssertInitialised();
            return new Vector3(
                gridPos.x * GridConfig.BlockSize,
                gridPos.y * GridConfig.BlockSize,
                gridPos.z * GridConfig.BlockSize);
        }

        public Vector3 GridToWorld(int x, int y, int z)
        {
            AssertInitialised();
            return new Vector3(
                x * GridConfig.BlockSize,
                y * GridConfig.BlockSize,
                z * GridConfig.BlockSize);
        }

        public Vector3Int[] GetNeighbours(Vector3Int gridPos)
        {
            AssertInitialised();
            List<Vector3Int> neighbours = new List<Vector3Int>(6);
            TryAddNeighbour(gridPos + Vector3Int.right, neighbours);
            TryAddNeighbour(gridPos + Vector3Int.left, neighbours);
            TryAddNeighbour(gridPos + Vector3Int.up, neighbours);
            TryAddNeighbour(gridPos + Vector3Int.down, neighbours);
            TryAddNeighbour(gridPos + new Vector3Int(0, 0, 1), neighbours);
            TryAddNeighbour(gridPos + new Vector3Int(0, 0, -1), neighbours);
            return neighbours.ToArray();
        }

        public bool IsSolid(Vector3Int gridPos)
        {
            AssertInitialised();
            return GetBlock(gridPos) == BlockType.Solid;
        }

        public Vector3Int GetCentreCell()
        {
            AssertInitialised();
            int c = GridSession.GridSize / 2;
            return new Vector3Int(c, c, c);
        }

        public byte[] CopyBlockGrid()
        {
            AssertInitialised();
            byte[] copy = new byte[_blockGrid.Length];
            _blockGrid.CopyTo(copy, 0);
            return copy;
        }

        public byte[] CopyFeatureGrid()
        {
            AssertInitialised();
            byte[] copy = new byte[_featureGrid.Length];
            _featureGrid.CopyTo(copy, 0);
            return copy;
        }

        public void LoadFromManaged(byte[] blockGrid, byte[] featureGrid)
        {
            AssertInitialised();
            int totalCells = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;

            if (blockGrid == null || blockGrid.Length != totalCells)
            {
                throw new ArgumentException("blockGrid must match current GridSession total cells.");
            }

            if (featureGrid == null || featureGrid.Length != totalCells)
            {
                throw new ArgumentException("featureGrid must match current GridSession total cells.");
            }

            blockGrid.CopyTo(_blockGrid, 0);
            featureGrid.CopyTo(_featureGrid, 0);
        }

        public void ResetForGeneration()
        {
            AssertInitialised();
            FillBlocks(BlockType.Air);
            FillFeatures(FeatureType.None);
        }

        private void AssertInitialised()
        {
            if (_blockGrid == null)
            {
                throw new InvalidOperationException(
                    "WorldGrid has not been initialised. Call Initialise(gridSize) first.");
            }
        }

        private void TryAddNeighbour(Vector3Int candidate, List<Vector3Int> neighbours)
        {
            if (InBounds(candidate))
            {
                neighbours.Add(candidate);
            }
        }

        private void FillBlocks(byte value)
        {
            for (int i = 0; i < _blockGrid.Length; i++)
            {
                _blockGrid[i] = value;
            }
        }

        private void FillFeatures(byte value)
        {
            for (int i = 0; i < _featureGrid.Length; i++)
            {
                _featureGrid[i] = value;
            }
        }
    }
}
