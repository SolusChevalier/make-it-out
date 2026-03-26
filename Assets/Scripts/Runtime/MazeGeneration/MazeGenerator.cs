using System;
using System.Collections;
using System.Collections.Generic;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using MakeItOut.Runtime.Player;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MakeItOut.Runtime.MazeGeneration
{
    public readonly struct MazeGenerationSnapshot
    {
        public readonly byte[] BlockGrid;
        public readonly byte[] FeatureGrid;
        public readonly List<Vector3> ExitWorldPositions;

        public MazeGenerationSnapshot(byte[] blockGrid, byte[] featureGrid, List<Vector3> exitWorldPositions)
        {
            BlockGrid = blockGrid;
            FeatureGrid = featureGrid;
            ExitWorldPositions = exitWorldPositions;
        }
    }

    public class MazeGenerator : MonoBehaviour
    {
        public static MazeGenerator Instance { get; private set; }
        public event Action OnGenerationComplete;
        public float Progress { get; private set; }

        public IReadOnlyList<Vector3> ExitWorldPositions => _exitWorldPositions;

        private readonly List<Vector3> _exitWorldPositions = new List<Vector3>();
        private Coroutine _generationCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void StartGeneration(LevelDefinition level, int resolvedSeed)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            if (GridSession.GridSize != level.GridSize)
            {
                Debug.LogWarning("GridSession.GridSize does not match level.GridSize. Ensure GridSession.Initialise is called before StartGeneration.");
            }

            if (_generationCoroutine != null)
            {
                StopCoroutine(_generationCoroutine);
            }

            _generationCoroutine = StartCoroutine(GenerationCoroutine(resolvedSeed));
        }

        public void ReportProgress(float value)
        {
            Progress = Mathf.Clamp01(Mathf.Max(Progress, value));
        }

        public MazeGenerationSnapshot GenerateSynchronouslyForTests(int seed)
        {
            return GenerateInternal(seed, false);
        }

        private IEnumerator GenerationCoroutine(int seed)
        {
            GenerateInternal(seed, true);
            yield break;
        }

        private MazeGenerationSnapshot GenerateInternal(int seed, bool runEditorValidation)
        {
            Progress = 0f;
            int totalCells = GridSession.GridSize * GridSession.GridSize * GridSession.GridSize;

            NativeArray<byte> blockGrid = default;
            NativeArray<byte> featureGrid = default;
            NativeArray<bool> visited = default;

            byte[] managedBlockGrid = null;
            byte[] managedFeatureGrid = null;
            int loopInjectionCount = 0;

            try
            {
                blockGrid = new NativeArray<byte>(totalCells, Allocator.TempJob);
                featureGrid = new NativeArray<byte>(totalCells, Allocator.TempJob);
                visited = new NativeArray<bool>(totalCells, Allocator.TempJob);

                InitialiseGridJob initJob = new InitialiseGridJob
                {
                    blockGrid = blockGrid,
                    featureGrid = featureGrid,
                    visited = visited,
                };
                JobHandle initHandle = initJob.Schedule(totalCells, 64);
                initHandle.Complete();
                Progress = 0.1f;

                CarveStartRoom(blockGrid, featureGrid, visited);

                CarveMazeJob carveJob = new CarveMazeJob
                {
                    blockGrid = blockGrid,
                    visited = visited,
                    GridSize = GridSession.GridSize,
                    Seed = (uint)seed,
                };
                JobHandle carveHandle = carveJob.Schedule();
                carveHandle.Complete();
                Progress = 0.5f;

                loopInjectionCount = InjectLoops(blockGrid, seed);
                Progress = 0.65f;

                PlaceFeatures(blockGrid, featureGrid);
                Progress = 0.8f;

                _exitWorldPositions.Clear();
                PlaceExits(blockGrid, featureGrid, seed, _exitWorldPositions);
                Progress = 0.9f;

                managedBlockGrid = new byte[totalCells];
                managedFeatureGrid = new byte[totalCells];
                for (int i = 0; i < totalCells; i++)
                {
                    managedBlockGrid[i] = blockGrid[i];
                    managedFeatureGrid[i] = featureGrid[i];
                }

                WorldGrid.Instance.LoadFromManaged(managedBlockGrid, managedFeatureGrid);

#if UNITY_EDITOR
                if (runEditorValidation)
                {
                    ValidateGeneration(managedBlockGrid, managedFeatureGrid, loopInjectionCount, _exitWorldPositions);
                }
#endif

                Progress = 0.95f;
                OnGenerationComplete?.Invoke();
            }
            finally
            {
                if (blockGrid.IsCreated)
                {
                    blockGrid.Dispose();
                }

                if (featureGrid.IsCreated)
                {
                    featureGrid.Dispose();
                }

                if (visited.IsCreated)
                {
                    visited.Dispose();
                }
            }

            return new MazeGenerationSnapshot(
                managedBlockGrid ?? Array.Empty<byte>(),
                managedFeatureGrid ?? Array.Empty<byte>(),
                new List<Vector3>(_exitWorldPositions));
        }

        private static void CarveStartRoom(
            NativeArray<byte> blockGrid,
            NativeArray<byte> featureGrid,
            NativeArray<bool> visited)
        {
            Vector3Int c = WorldGrid.Instance.GetCentreCell();
            Vector3Int[] cells =
            {
                c,
                c + Vector3Int.right,
                c + Vector3Int.left,
                c + Vector3Int.up,
                c + Vector3Int.down,
                c + new Vector3Int(0, 0, 1),
                c + new Vector3Int(0, 0, -1),
            };

            for (int i = 0; i < cells.Length; i++)
            {
                int index = GridIndex.ToIndex(cells[i].x, cells[i].y, cells[i].z);
                blockGrid[index] = BlockType.Air;
                visited[index] = true;
            }

            featureGrid[GridIndex.ToIndex(c.x, c.y, c.z)] = FeatureType.Start;
        }

        private static int InjectLoops(NativeArray<byte> blockGrid, int seed)
        {
            int carvedCount = 0;
            System.Random random = new System.Random(seed);
            int size = GridSession.GridSize;

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        int index = GridIndex.ToIndex(x, y, z);
                        if (blockGrid[index] != BlockType.Solid)
                        {
                            continue;
                        }

                        bool hasOppositeAirPair =
                            (GetBlock(blockGrid, x + 1, y, z) == BlockType.Air && GetBlock(blockGrid, x - 1, y, z) == BlockType.Air) ||
                            (GetBlock(blockGrid, x, y + 1, z) == BlockType.Air && GetBlock(blockGrid, x, y - 1, z) == BlockType.Air) ||
                            (GetBlock(blockGrid, x, y, z + 1) == BlockType.Air && GetBlock(blockGrid, x, y, z - 1) == BlockType.Air);

                        if (!hasOppositeAirPair)
                        {
                            continue;
                        }

                        if (random.NextDouble() <= 0.25d)
                        {
                            blockGrid[index] = BlockType.Air;
                            carvedCount++;
                        }
                    }
                }
            }

            return carvedCount;
        }

        private static void PlaceFeatures(NativeArray<byte> blockGrid, NativeArray<byte> featureGrid)
        {
            PlaceLadders(blockGrid, featureGrid);
            PlaceStairs(blockGrid, featureGrid);
            PlaceHoles(blockGrid, featureGrid);
        }

        private static void PlaceLadders(NativeArray<byte> blockGrid, NativeArray<byte> featureGrid)
        {
            int size = GridSession.GridSize;
            for (int z = 1; z < size - 1; z++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    int y = 1;
                    while (y < size - 1)
                    {
                        if (GetBlock(blockGrid, x, y, z) != BlockType.Air)
                        {
                            y++;
                            continue;
                        }

                        int start = y;
                        while (y < size - 1 && GetBlock(blockGrid, x, y, z) == BlockType.Air)
                        {
                            y++;
                        }

                        int endExclusive = y;
                        int runLength = endExclusive - start;
                        if (runLength < 3 || !RunHasLadderWall(blockGrid, x, z, start, endExclusive))
                        {
                            continue;
                        }

                        for (int yy = start + 1; yy <= endExclusive - 2; yy++)
                        {
                            int index = GridIndex.ToIndex(x, yy, z);
                            if (featureGrid[index] == FeatureType.None)
                            {
                                featureGrid[index] = FeatureType.Ladder;
                            }
                        }
                    }
                }
            }
        }

        private static bool RunHasLadderWall(
            NativeArray<byte> blockGrid,
            int x,
            int z,
            int startY,
            int endYExclusive)
        {
            for (int y = startY; y < endYExclusive; y++)
            {
                if (GetBlock(blockGrid, x + 1, y, z) == BlockType.Solid ||
                    GetBlock(blockGrid, x - 1, y, z) == BlockType.Solid ||
                    GetBlock(blockGrid, x, y, z + 1) == BlockType.Solid ||
                    GetBlock(blockGrid, x, y, z - 1) == BlockType.Solid)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PlaceStairs(NativeArray<byte> blockGrid, NativeArray<byte> featureGrid)
        {
            int size = GridSession.GridSize;
            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 2; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        int index = GridIndex.ToIndex(x, y, z);
                        if (blockGrid[index] != BlockType.Air || featureGrid[index] != FeatureType.None)
                        {
                            continue;
                        }

                        if (IsStairCandidate(blockGrid, x, y, z, 1, 0) ||
                            IsStairCandidate(blockGrid, x, y, z, -1, 0) ||
                            IsStairCandidate(blockGrid, x, y, z, 0, 1) ||
                            IsStairCandidate(blockGrid, x, y, z, 0, -1))
                        {
                            featureGrid[index] = FeatureType.Stair;
                        }
                    }
                }
            }
        }

        private static bool IsStairCandidate(
            NativeArray<byte> blockGrid,
            int x,
            int y,
            int z,
            int dx,
            int dz)
        {
            int upperX = x + dx;
            int upperY = y + 1;
            int upperZ = z + dz;

            return GetBlock(blockGrid, upperX, upperY, upperZ) == BlockType.Air &&
                   GetBlock(blockGrid, x + dx, y, z + dz) == BlockType.Solid &&
                   GetBlock(blockGrid, x, y + 1, z) == BlockType.Solid;
        }

        private static void PlaceHoles(NativeArray<byte> blockGrid, NativeArray<byte> featureGrid)
        {
            int size = GridSession.GridSize;
            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 2; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        int index = GridIndex.ToIndex(x, y, z);
                        if (blockGrid[index] != BlockType.Air || featureGrid[index] != FeatureType.None)
                        {
                            continue;
                        }

                        if (GetBlock(blockGrid, x, y - 1, z) == BlockType.Air &&
                            GetBlock(blockGrid, x, y - 2, z) == BlockType.Air)
                        {
                            featureGrid[index] = FeatureType.Hole;
                        }
                    }
                }
            }
        }

        private static void PlaceExits(
            NativeArray<byte> blockGrid,
            NativeArray<byte> featureGrid,
            int seed,
            List<Vector3> exitWorldPositions)
        {
            List<ExitCandidate> candidates = CollectExitCandidates(blockGrid);
            Shuffle(candidates, new System.Random(seed));

            List<ExitCandidate> selected = SelectExitCandidates(candidates);
            for (int i = 0; i < selected.Count; i++)
            {
                ExitCandidate exit = selected[i];
                int boundaryIndex = GridIndex.ToIndex(exit.Boundary.x, exit.Boundary.y, exit.Boundary.z);
                blockGrid[boundaryIndex] = BlockType.Air;
                featureGrid[boundaryIndex] = FeatureType.Exit;
                exitWorldPositions.Add(WorldGrid.Instance.GridToWorld(exit.Boundary));
            }
        }

        private static List<ExitCandidate> CollectExitCandidates(NativeArray<byte> blockGrid)
        {
            int size = GridSession.GridSize;
            List<ExitCandidate> candidates = new List<ExitCandidate>();

            for (int z = 1; z < size - 1; z++)
            {
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        if (GetBlock(blockGrid, x, y, z) != BlockType.Air)
                        {
                            continue;
                        }

                        if (x == 1)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(0, y, z), 0));
                        }

                        if (x == size - 2)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(size - 1, y, z), 1));
                        }

                        if (y == 1)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(x, 0, z), 2));
                        }

                        if (y == size - 2)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(x, size - 1, z), 3));
                        }

                        if (z == 1)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(x, y, 0), 4));
                        }

                        if (z == size - 2)
                        {
                            candidates.Add(new ExitCandidate(new Vector3Int(x, y, z), new Vector3Int(x, y, size - 1), 5));
                        }
                    }
                }
            }

            return candidates;
        }

        private static List<ExitCandidate> SelectExitCandidates(List<ExitCandidate> candidates)
        {
            int minDistance = GridSession.GridSize / 3;
            List<ExitCandidate> selected = new List<ExitCandidate>(3);
            HashSet<int> usedFaces = new HashSet<int>();

            TrySelect(candidates, selected, usedFaces, minDistance, true);
            if (selected.Count < 3)
            {
                TrySelect(candidates, selected, usedFaces, minDistance, false);
            }

            if (selected.Count < 3)
            {
                TrySelect(candidates, selected, usedFaces, 0, false);
            }

            return selected;
        }

        private static void TrySelect(
            List<ExitCandidate> candidates,
            List<ExitCandidate> selected,
            HashSet<int> usedFaces,
            int minDistance,
            bool requireUniqueFace)
        {
            for (int i = 0; i < candidates.Count && selected.Count < 3; i++)
            {
                ExitCandidate candidate = candidates[i];
                if (requireUniqueFace && usedFaces.Contains(candidate.FaceId))
                {
                    continue;
                }

                if (minDistance > 0 && !IsFarEnough(candidate, selected, minDistance))
                {
                    continue;
                }

                selected.Add(candidate);
                usedFaces.Add(candidate.FaceId);
            }
        }

        private static bool IsFarEnough(ExitCandidate candidate, List<ExitCandidate> selected, int minDistance)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                Vector3Int delta = selected[i].Boundary - candidate.Boundary;
                float distance = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (distance < minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(List<T> values, System.Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        private static byte GetBlock(NativeArray<byte> grid, int x, int y, int z)
        {
            if (x < 0 || x >= GridSession.GridSize ||
                y < 0 || y >= GridSession.GridSize ||
                z < 0 || z >= GridSession.GridSize)
            {
                return BlockType.Solid;
            }

            return grid[GridIndex.ToIndex(x, y, z)];
        }

#if UNITY_EDITOR
        private static void ValidateGeneration(
            byte[] blockGrid,
            byte[] featureGrid,
            int loopInjectionCount,
            List<Vector3> exitPositions)
        {
            Vector3Int centre = WorldGrid.Instance.GetCentreCell();
            int airCount = 0;
            int solidCount = 0;
            int[] featureCounts = new int[6];

            for (int i = 0; i < blockGrid.Length; i++)
            {
                if (blockGrid[i] == BlockType.Air) airCount++;
                else solidCount++;

                byte feature = featureGrid[i];
                if (feature >= 0 && feature < featureCounts.Length)
                {
                    featureCounts[feature]++;
                }
            }

            int reachableAir = FloodFillReachableAir(blockGrid, centre);
            if (reachableAir != airCount)
            {
                Debug.LogError($"Maze validation failed: reachableAir={reachableAir}, totalAir={airCount}.");
            }

            ValidateExits(blockGrid, featureGrid);
            ValidateBoundary(blockGrid, featureGrid);

            Debug.Log(
                $"Maze generation summary: Air={airCount}, Solid={solidCount}, Loops={loopInjectionCount}, " +
                $"Ladder={featureCounts[FeatureType.Ladder]}, Stair={featureCounts[FeatureType.Stair]}, " +
                $"Hole={featureCounts[FeatureType.Hole]}, Exit={featureCounts[FeatureType.Exit]}, " +
                $"Start={featureCounts[FeatureType.Start]}, ExitWorldPositions={exitPositions.Count}");
        }

        private static int FloodFillReachableAir(byte[] blockGrid, Vector3Int start)
        {
            int size = GridSession.GridSize;
            bool[] visited = new bool[blockGrid.Length];
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            visited[GridIndex.ToIndex(start.x, start.y, start.z)] = true;

            int reachedAir = 0;
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                int currentIndex = GridIndex.ToIndex(current.x, current.y, current.z);
                if (blockGrid[currentIndex] != BlockType.Air)
                {
                    continue;
                }

                reachedAir++;
                TryVisit(queue, visited, blockGrid, current + Vector3Int.right, size);
                TryVisit(queue, visited, blockGrid, current + Vector3Int.left, size);
                TryVisit(queue, visited, blockGrid, current + Vector3Int.up, size);
                TryVisit(queue, visited, blockGrid, current + Vector3Int.down, size);
                TryVisit(queue, visited, blockGrid, current + new Vector3Int(0, 0, 1), size);
                TryVisit(queue, visited, blockGrid, current + new Vector3Int(0, 0, -1), size);
            }

            return reachedAir;
        }

        private static void TryVisit(
            Queue<Vector3Int> queue,
            bool[] visited,
            byte[] blockGrid,
            Vector3Int pos,
            int size)
        {
            if (pos.x < 0 || pos.x >= size ||
                pos.y < 0 || pos.y >= size ||
                pos.z < 0 || pos.z >= size)
            {
                return;
            }

            int index = GridIndex.ToIndex(pos.x, pos.y, pos.z);
            if (visited[index] || blockGrid[index] != BlockType.Air)
            {
                return;
            }

            visited[index] = true;
            queue.Enqueue(pos);
        }

        private static void ValidateExits(byte[] blockGrid, byte[] featureGrid)
        {
            int exitCount = 0;
            HashSet<int> faces = new HashSet<int>();

            for (int i = 0; i < featureGrid.Length; i++)
            {
                if (featureGrid[i] != FeatureType.Exit)
                {
                    continue;
                }

                exitCount++;
                if (blockGrid[i] != BlockType.Air)
                {
                    Debug.LogError("Maze validation failed: exit feature is not carved to air.");
                }

                Vector3Int pos = GridIndex.FromIndex(i);
                int faceId = GetFaceId(pos);
                if (faceId < 0)
                {
                    Debug.LogError($"Maze validation failed: exit is not on boundary at {pos}.");
                    continue;
                }

                if (!faces.Add(faceId))
                {
                    Debug.LogError("Maze validation failed: duplicate exits on same cube face.");
                }
            }

            if (exitCount != 3)
            {
                Debug.LogError($"Maze validation failed: expected 3 exits but found {exitCount}.");
            }
        }

        private static int GetFaceId(Vector3Int pos)
        {
            int max = GridSession.GridSize - 1;
            if (pos.x == 0) return 0;
            if (pos.x == max) return 1;
            if (pos.y == 0) return 2;
            if (pos.y == max) return 3;
            if (pos.z == 0) return 4;
            if (pos.z == max) return 5;
            return -1;
        }

        private static void ValidateBoundary(byte[] blockGrid, byte[] featureGrid)
        {
            int max = GridSession.GridSize - 1;
            for (int z = 0; z <= max; z++)
            {
                for (int y = 0; y <= max; y++)
                {
                    for (int x = 0; x <= max; x++)
                    {
                        if (!(x == 0 || y == 0 || z == 0 || x == max || y == max || z == max))
                        {
                            continue;
                        }

                        int index = GridIndex.ToIndex(x, y, z);
                        bool isExit = featureGrid[index] == FeatureType.Exit;
                        if (!isExit && blockGrid[index] != BlockType.Solid)
                        {
                            Debug.LogError($"Maze validation failed: boundary opened at ({x},{y},{z}).");
                        }
                    }
                }
            }
        }
#endif

        private readonly struct ExitCandidate
        {
            public readonly Vector3Int Interior;
            public readonly Vector3Int Boundary;
            public readonly int FaceId;

            public ExitCandidate(Vector3Int interior, Vector3Int boundary, int faceId)
            {
                Interior = interior;
                Boundary = boundary;
                FaceId = faceId;
            }
        }
    }
}
