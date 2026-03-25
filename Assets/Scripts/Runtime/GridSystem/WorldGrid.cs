using System.Collections.Generic;
using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public sealed class WorldGrid
    {
        private static WorldGrid s_instance;

        private readonly byte[] _blockGrid;
        private readonly byte[] _featureGrid;

        public static WorldGrid Instance => s_instance ??= new WorldGrid();

        private WorldGrid()
        {
            _blockGrid = new byte[GridConfig.TotalCells];
            _featureGrid = new byte[GridConfig.TotalCells];
            FillBlocks(BlockType.Solid);
        }

        public byte GetBlock(int x, int y, int z)
        {
            if (!InBounds(x, y, z))
            {
                return BlockType.Solid;
            }

            return _blockGrid[GridIndex.ToIndex(x, y, z)];
        }

        public byte GetBlock(Vector3Int gridPos)
        {
            return GetBlock(gridPos.x, gridPos.y, gridPos.z);
        }

        public byte GetFeature(int x, int y, int z)
        {
            if (!InBounds(x, y, z))
            {
                return FeatureType.None;
            }

            return _featureGrid[GridIndex.ToIndex(x, y, z)];
        }

        public byte GetFeature(Vector3Int gridPos)
        {
            return GetFeature(gridPos.x, gridPos.y, gridPos.z);
        }

        public void SetBlock(int x, int y, int z, byte value)
        {
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
            return x >= 0 && x < GridConfig.GridSize &&
                   y >= 0 && y < GridConfig.GridSize &&
                   z >= 0 && z < GridConfig.GridSize;
        }

        public bool InBounds(Vector3Int gridPos)
        {
            return InBounds(gridPos.x, gridPos.y, gridPos.z);
        }

        public Vector3Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / GridConfig.BlockSize),
                Mathf.RoundToInt(worldPos.y / GridConfig.BlockSize),
                Mathf.RoundToInt(worldPos.z / GridConfig.BlockSize));
        }

        public Vector3 GridToWorld(Vector3Int gridPos)
        {
            return new Vector3(
                gridPos.x * GridConfig.BlockSize,
                gridPos.y * GridConfig.BlockSize,
                gridPos.z * GridConfig.BlockSize);
        }

        public Vector3 GridToWorld(int x, int y, int z)
        {
            return new Vector3(
                x * GridConfig.BlockSize,
                y * GridConfig.BlockSize,
                z * GridConfig.BlockSize);
        }

        public Vector3Int[] GetNeighbours(Vector3Int gridPos)
        {
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
            return GetBlock(gridPos) == BlockType.Solid;
        }

        public Vector3Int GetCentreCell()
        {
            int c = GridConfig.GridSize / 2;
            return new Vector3Int(c, c, c);
        }

        public byte[] CopyBlockGrid()
        {
            byte[] copy = new byte[_blockGrid.Length];
            _blockGrid.CopyTo(copy, 0);
            return copy;
        }

        public byte[] CopyFeatureGrid()
        {
            byte[] copy = new byte[_featureGrid.Length];
            _featureGrid.CopyTo(copy, 0);
            return copy;
        }

        public void LoadFromManaged(byte[] blockGrid, byte[] featureGrid)
        {
            if (blockGrid == null || blockGrid.Length != GridConfig.TotalCells)
            {
                throw new System.ArgumentException("blockGrid must match GridConfig.TotalCells.");
            }

            if (featureGrid == null || featureGrid.Length != GridConfig.TotalCells)
            {
                throw new System.ArgumentException("featureGrid must match GridConfig.TotalCells.");
            }

            blockGrid.CopyTo(_blockGrid, 0);
            featureGrid.CopyTo(_featureGrid, 0);
        }

        public void ResetForGeneration()
        {
            FillBlocks(BlockType.Air);
            FillFeatures(FeatureType.None);
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
