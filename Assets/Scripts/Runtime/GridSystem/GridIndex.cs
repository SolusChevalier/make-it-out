using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public static class GridIndex
    {
        public static int ToIndex(int x, int y, int z)
        {
            return x + GridSession.GridSize * (y + GridSession.GridSize * z);
        }

        public static Vector3Int FromIndex(int index)
        {
            int x = index % GridSession.GridSize;
            int y = (index / GridSession.GridSize) % GridSession.GridSize;
            int z = index / (GridSession.GridSize * GridSession.GridSize);
            return new Vector3Int(x, y, z);
        }
    }
}
