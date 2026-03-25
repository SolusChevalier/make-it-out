using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MakeItOut.Runtime.MazeGeneration
{
    [BurstCompile]
    public struct InitialiseGridJob : IJobParallelFor
    {
        public NativeArray<byte> blockGrid;
        public NativeArray<byte> featureGrid;
        public NativeArray<bool> visited;

        public void Execute(int index)
        {
            blockGrid[index] = GridSystem.BlockType.Solid;
            featureGrid[index] = GridSystem.FeatureType.None;
            visited[index] = false;
        }
    }

    [BurstCompile]
    public struct CarveMazeJob : IJob
    {
        public NativeArray<byte> blockGrid;
        public NativeArray<bool> visited;
        public int GridSize;
        public uint Seed;

        public void Execute()
        {
            uint seed = Seed == 0u ? 1u : Seed;
            Random rng = new Random(seed);

            NativeList<int> stack = new NativeList<int>(Allocator.Temp);
            NativeList<int> neighbours = new NativeList<int>(Allocator.Temp);

            int c = GridSize / 2;
            stack.Add(ToIndex(c, c, c, GridSize));

            while (stack.Length > 0)
            {
                int currentIndex = stack[stack.Length - 1];
                int3 current = FromIndex(currentIndex, GridSize);
                neighbours.Clear();

                TryAddNeighbour(current, new int3(2, 0, 0), neighbours);
                TryAddNeighbour(current, new int3(-2, 0, 0), neighbours);
                TryAddNeighbour(current, new int3(0, 2, 0), neighbours);
                TryAddNeighbour(current, new int3(0, -2, 0), neighbours);
                TryAddNeighbour(current, new int3(0, 0, 2), neighbours);
                TryAddNeighbour(current, new int3(0, 0, -2), neighbours);

                if (neighbours.Length == 0)
                {
                    stack.RemoveAt(stack.Length - 1);
                    continue;
                }

                Shuffle(neighbours, ref rng);
                int nextIndex = neighbours[0];
                int3 next = FromIndex(nextIndex, GridSize);
                int3 mid = new int3(
                    (current.x + next.x) / 2,
                    (current.y + next.y) / 2,
                    (current.z + next.z) / 2);

                int midIndex = ToIndex(mid.x, mid.y, mid.z, GridSize);
                blockGrid[midIndex] = GridSystem.BlockType.Air;
                blockGrid[nextIndex] = GridSystem.BlockType.Air;
                visited[midIndex] = true;
                visited[nextIndex] = true;
                stack.Add(nextIndex);
            }

            neighbours.Dispose();
            stack.Dispose();
        }

        private void TryAddNeighbour(int3 current, int3 delta, NativeList<int> neighbours)
        {
            int3 candidate = current + delta;
            if (!IsCarvable(candidate))
            {
                return;
            }

            int index = ToIndex(candidate.x, candidate.y, candidate.z, GridSize);
            if (!visited[index])
            {
                neighbours.Add(index);
            }
        }

        private bool IsCarvable(int3 pos)
        {
            return pos.x > 0 && pos.x < GridSize - 1 &&
                   pos.y > 0 && pos.y < GridSize - 1 &&
                   pos.z > 0 && pos.z < GridSize - 1;
        }

        private static void Shuffle(NativeList<int> values, ref Random rng)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int swapIndex = rng.NextInt(0, i + 1);
                int temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private static int ToIndex(int x, int y, int z, int gridSize)
        {
            return x + gridSize * (y + gridSize * z);
        }

        private static int3 FromIndex(int index, int gridSize)
        {
            int x = index % gridSize;
            int y = (index / gridSize) % gridSize;
            int z = index / (gridSize * gridSize);
            return new int3(x, y, z);
        }
    }
}
