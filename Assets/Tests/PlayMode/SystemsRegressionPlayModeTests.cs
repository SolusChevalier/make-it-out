using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MakeItOut.Tests.PlayMode
{
    public class SystemsRegressionPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            Scene scene = SceneManager.CreateScene("PlayMode_Regression_Test");
            SceneManager.SetActiveScene(scene);
            GridSession.Reset();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
                Object.Destroy(go);

            yield return null;
            GridSession.Reset();
            SetStaticAutoPropertyBackingField(typeof(MazeGenerator), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(ChunkManager), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(PlayerController), "Instance", null);
            SetStaticField(typeof(CameraOrientation), "Instance", null);
        }

        [UnityTest]
        public IEnumerator GridSize15_MazeGeneration_ProducesConnectedMaze()
        {
            SetupGridAndGenerator(15, 100, out _, out _);
            AssertConnectedAir();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize15_ExitCount_IsThree()
        {
            SetupGridAndGenerator(15, 100, out _, out _);
            AssertExitCount(3);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize15_BoundaryIntegrity_OnlySolidOrExit()
        {
            SetupGridAndGenerator(15, 100, out _, out _);
            AssertBoundaryIntegrity();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize63_MazeGeneration_ProducesConnectedMaze()
        {
            SetupGridAndGenerator(63, 101, out _, out _);
            AssertConnectedAir();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize63_ExitCount_IsThree()
        {
            SetupGridAndGenerator(63, 101, out _, out _);
            AssertExitCount(3);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize63_BoundaryIntegrity_OnlySolidOrExit()
        {
            SetupGridAndGenerator(63, 101, out _, out _);
            AssertBoundaryIntegrity();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize63_ChunkCount_MatchesCeilingDivision()
        {
            SetupGridAndGenerator(63, 101, out ChunkManager chunkManager, out _);
            chunkManager.InitialiseAllChunks();
            yield return null;

            int maxChunks = GridConfig.ChunksPerAxis(63) * GridConfig.ChunksPerAxis(63) * GridConfig.ChunksPerAxis(63);
            Assert.LessOrEqual(chunkManager.transform.childCount, maxChunks);
        }

        [UnityTest]
        public IEnumerator GridSize95_MazeGeneration_ProducesConnectedMaze()
        {
            SetupGridAndGenerator(95, 102, out _, out _);
            AssertConnectedAir();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize95_ExitCount_IsThree()
        {
            SetupGridAndGenerator(95, 102, out _, out _);
            AssertExitCount(3);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridSize95_GenerationCompletesWithinThirtySeconds()
        {
            Stopwatch sw = Stopwatch.StartNew();
            SetupGridAndGenerator(95, 102, out _, out _);
            sw.Stop();
            Assert.Less(sw.Elapsed.TotalSeconds, 30d);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerController_FallsInNegativeCamUpDirection()
        {
            GridSession.Initialise(MakeLevel(31), 0);
            WorldGrid.Instance.Initialise(GridSession.GridSize);
            WorldGrid.Instance.ResetForGeneration();

            GameObject orientationGo = new GameObject("CameraOrientation");
            CameraOrientation orientation = orientationGo.AddComponent<CameraOrientation>();
            orientation.Up = Vector3.forward;
            orientation.Right = Vector3.right;
            orientation.Forward = Vector3.up;

            GameObject playerGo = new GameObject("Player");
            playerGo.AddComponent<CharacterController>();
            PlayerController player = playerGo.AddComponent<PlayerController>();
            playerGo.transform.position = WorldGrid.Instance.GridToWorld(15, 15, 15);
            Vector3 start = playerGo.transform.position;

            for (int i = 0; i < 10; i++)
                yield return null;

            Vector3 delta = playerGo.transform.position - start;
            Assert.Less(delta.z, 0f);
            Assert.That(Mathf.Abs(delta.y), Is.LessThan(0.1f));
            _ = player;
        }

        [UnityTest]
        public IEnumerator CameraSystem_FourRightPresses_ReturnsToOriginalOrientation()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                Quaternion target = ComputeTarget(controller, KeyCode.RightArrow);
                SetCurrentOrientation(controller, target);
            }

            AssertCardinalEquals(Quaternion.identity, GetCurrentOrientation(controller));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraSystem_FourUpPresses_ReturnsToOriginalOrientation()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                Quaternion target = ComputeTarget(controller, KeyCode.UpArrow);
                SetCurrentOrientation(controller, target);
            }

            AssertCardinalEquals(Quaternion.identity, GetCurrentOrientation(controller));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraSystem_UpsideDownOrientation_IsReachable()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);

            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.RightArrow));
            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.RightArrow));

            Assert.AreEqual(Vector3.down, GetCurrentOrientation(controller) * Vector3.up);
            yield return null;
        }

        private static void SetupGridAndGenerator(int gridSize, int seed, out ChunkManager chunkManager, out MazeGenerator generator)
        {
            GridSession.Initialise(MakeLevel(gridSize), seed);
            WorldGrid.Instance.Initialise(GridSession.GridSize);
            WorldGrid.Instance.ResetForGeneration();

            GameObject generatorGo = new GameObject("MazeGenerator");
            generator = generatorGo.AddComponent<MazeGenerator>();
            generator.GenerateSynchronouslyForTests(seed);

            GameObject chunkGo = new GameObject("ChunkManager");
            chunkManager = chunkGo.AddComponent<ChunkManager>();
            SetPrivateField(chunkManager, "_blockMaterial", new Material(Shader.Find("Standard")));
        }

        private static LevelDefinition MakeLevel(int gridSize)
        {
            return new GeneratedLevelDefinition
            {
                LevelId = $"test_{gridSize}",
                DisplayName = $"Grid {gridSize}",
                GridSize = gridSize,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 0,
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };
        }

        private static void AssertConnectedAir()
        {
            Vector3Int centre = WorldGrid.Instance.GetCentreCell();
            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(centre);
            visited.Add(centre);

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                if (WorldGrid.Instance.GetBlock(cell) != BlockType.Air)
                    continue;

                foreach (Vector3Int n in WorldGrid.Instance.GetNeighbours(cell))
                {
                    if (visited.Contains(n) || WorldGrid.Instance.GetBlock(n) != BlockType.Air)
                        continue;

                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }

            int air = 0;
            for (int z = 0; z < GridSession.GridSize; z++)
            for (int y = 0; y < GridSession.GridSize; y++)
            for (int x = 0; x < GridSession.GridSize; x++)
            {
                if (WorldGrid.Instance.GetBlock(x, y, z) == BlockType.Air)
                    air++;
            }

            Assert.AreEqual(air, visited.Count);
        }

        private static void AssertExitCount(int expected)
        {
            int exits = 0;
            for (int z = 0; z < GridSession.GridSize; z++)
            for (int y = 0; y < GridSession.GridSize; y++)
            for (int x = 0; x < GridSession.GridSize; x++)
            {
                if (WorldGrid.Instance.GetFeature(x, y, z) == FeatureType.Exit)
                    exits++;
            }

            Assert.AreEqual(expected, exits);
        }

        private static void AssertBoundaryIntegrity()
        {
            int max = GridSession.GridSize - 1;
            for (int z = 0; z <= max; z++)
            for (int y = 0; y <= max; y++)
            for (int x = 0; x <= max; x++)
            {
                if (!(x == 0 || x == max || y == 0 || y == max || z == 0 || z == max))
                    continue;

                byte block = WorldGrid.Instance.GetBlock(x, y, z);
                byte feature = WorldGrid.Instance.GetFeature(x, y, z);
                bool valid = block == BlockType.Solid || feature == FeatureType.Exit;
                if (!valid)
                    Debug.LogError($"Boundary breach at {x},{y},{z} block={block} feature={feature}");
                Assert.IsTrue(valid);
            }
        }

        private static CameraController CreateControllerForOrientationTests()
        {
            GameObject player = new GameObject("Player");
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerController>();

            GameObject camGo = new GameObject("Cam");
            Camera camera = camGo.AddComponent<Camera>();
            camera.orthographic = true;

            GameObject controllerGo = new GameObject("CameraController");
            CameraController controller = controllerGo.AddComponent<CameraController>();
            SetPrivateField(controller, "_playerTransform", player.transform);
            SetPrivateField(controller, "_cam", camera);
            return controller;
        }

        private static Quaternion ComputeTarget(CameraController controller, KeyCode key)
        {
            MethodInfo method = typeof(CameraController).GetMethod("ComputeTargetOrientation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (Quaternion)method.Invoke(controller, new object[] { key });
        }

        private static Quaternion GetCurrentOrientation(CameraController controller)
        {
            FieldInfo field = typeof(CameraController).GetField("_currentOrientation", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Quaternion)field.GetValue(controller);
        }

        private static void SetCurrentOrientation(CameraController controller, Quaternion value)
        {
            FieldInfo field = typeof(CameraController).GetField("_currentOrientation", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(controller, value);
        }

        private static void AssertCardinalEquals(Quaternion expected, Quaternion actual)
        {
            Assert.AreEqual(expected * Vector3.up, actual * Vector3.up);
            Assert.AreEqual(expected * Vector3.right, actual * Vector3.right);
            Assert.AreEqual(expected * Vector3.forward, actual * Vector3.forward);
        }

        private static void SetPrivateField<T>(T instance, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(instance, value);
        }

        private static void SetStaticAutoPropertyBackingField(System.Type type, string propertyName, object value)
        {
            string backingField = $"<{propertyName}>k__BackingField";
            FieldInfo field = type.GetField(backingField, BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(null, value);
        }

        private static void SetStaticField(System.Type type, string fieldName, object value)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(null, value);
        }
    }
}
