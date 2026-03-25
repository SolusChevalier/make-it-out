namespace MakeItOut.Runtime.GridSystem
{
    public static class GridConfig
    {
        public const int GridSize = 63;
        public const float BlockSize = 2f;
        public const int ChunkSize = 8;
        public const int ChunksPerAxis = (GridSize + ChunkSize - 1) / ChunkSize;
        public const int TotalCells = GridSize * GridSize * GridSize;
    }
}
