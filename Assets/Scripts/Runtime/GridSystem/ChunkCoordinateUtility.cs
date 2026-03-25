using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public static class ChunkCoordinateUtility
    {
        public static Vector3Int GridToChunk(Vector3Int gridPos)
        {
            return new Vector3Int(
                gridPos.x / GridConfig.ChunkSize,
                gridPos.y / GridConfig.ChunkSize,
                gridPos.z / GridConfig.ChunkSize);
        }

        public static Vector3Int ChunkOrigin(Vector3Int chunkCoord)
        {
            return chunkCoord * GridConfig.ChunkSize;
        }
    }
}
