using System;
using MakeItOut.Runtime.Progression;

namespace MakeItOut.Runtime.GridSystem
{
    public static class GridSession
    {
        private static int _gridSize = 63;

        public static int GridSize
        {
            get => _gridSize;
            private set
            {
                if (value < 15 || value > 127)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"GridSize must be between 15 and 127. Got {value}.");
                if (value % 2 == 0)
                    throw new ArgumentException(
                        $"GridSize must be odd for a true centre cell. Got {value}.");
                _gridSize = value;
            }
        }

        public static int Seed { get; private set; }
        public static float BlockSize => GridConfig.BlockSize;
        public static int ChunkSize => GridConfig.ChunkSize;
        public static int ChunksPerAxis => GridConfig.ChunksPerAxis(_gridSize);

        public static void Initialise(LevelDefinition level, int resolvedSeed)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            GridSize = level.GridSize;
            Seed = resolvedSeed;
        }

        public static void Reset()
        {
            _gridSize = 63;
            Seed = 0;
        }
    }
}
