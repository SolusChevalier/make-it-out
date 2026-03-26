namespace MakeItOut.Runtime.GridSystem
{
    public static class GridConfig
    {
        public const float BlockSize = 2f;
        public const int ChunkSize = 8;

        public static int ChunksPerAxis(int gridSize)
            => (gridSize + ChunkSize - 1) / ChunkSize;
    }
}
