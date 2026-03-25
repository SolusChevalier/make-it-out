using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public static class GridIndex
    {
        public static int ToIndex(int x, int y, int z)
        {
            return x + GridConfig.GridSize * (y + GridConfig.GridSize * z);
        }

        public static Vector3Int FromIndex(int index)
        {
            int x = index % GridConfig.GridSize;
            int y = (index / GridConfig.GridSize) % GridConfig.GridSize;
            int z = index / (GridConfig.GridSize * GridConfig.GridSize);
            return new Vector3Int(x, y, z);
        }
    }
}
