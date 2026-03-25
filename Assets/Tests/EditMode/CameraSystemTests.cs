using System.Reflection;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class CameraSystemTests
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void RightArrow_FourTurns_ReturnsToStartingOrientation()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);

            for (int i = 0; i < 4; i++)
            {
                Quaternion target = ComputeTarget(controller, KeyCode.RightArrow);
                SetCurrentOrientation(controller, target);
            }

            AssertCardinalEquals(Quaternion.identity, GetCurrentOrientation(controller));
            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void UpArrow_FourTurns_ReturnsToStartingOrientation()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);

            for (int i = 0; i < 4; i++)
            {
                Quaternion target = ComputeTarget(controller, KeyCode.UpArrow);
                SetCurrentOrientation(controller, target);
            }

            AssertCardinalEquals(Quaternion.identity, GetCurrentOrientation(controller));
            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void RightTwiceThenUpTwice_StaysCardinalAndOrthonormal()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);

            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.RightArrow));
            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.RightArrow));
            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.UpArrow));
            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.UpArrow));

            Quaternion orientation = GetCurrentOrientation(controller);
            Vector3 up = orientation * Vector3.up;
            Vector3 right = orientation * Vector3.right;
            Vector3 forward = orientation * Vector3.forward;

            AssertAxisAlignedUnit(up);
            AssertAxisAlignedUnit(right);
            AssertAxisAlignedUnit(forward);
            Assert.AreEqual(0f, Vector3.Dot(up, right), 0.001f);
            Assert.AreEqual(0f, Vector3.Dot(up, forward), 0.001f);
            Assert.AreEqual(0f, Vector3.Dot(right, forward), 0.001f);

            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void UpArrowTwice_CanReachUpsideDownOrientation()
        {
            CameraController controller = CreateControllerForOrientationTests();
            SetCurrentOrientation(controller, Quaternion.identity);

            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.UpArrow));
            SetCurrentOrientation(controller, ComputeTarget(controller, KeyCode.UpArrow));

            Vector3 up = GetCurrentOrientation(controller) * Vector3.up;
            Assert.AreEqual(Vector3.down, up);

            Object.DestroyImmediate(controller.gameObject);
        }

        [Test]
        public void ChunkManager_GetChunkObject_ReturnsRegisteredChunkObject()
        {
            WorldGrid.Instance.ResetForGeneration();

            GameObject root = new GameObject("ChunkManager_Test");
            ChunkManager manager = root.AddComponent<ChunkManager>();

            Vector3Int coord = new Vector3Int(1, 2, 3);
            ChunkData data = new ChunkData
            {
                ChunkCoord = coord,
                IsActive = true,
            };
            GameObject chunkObject = new GameObject("ChunkObject_Test");

            manager.RegisterChunk(coord, data, chunkObject);

            Assert.AreSame(chunkObject, manager.GetChunkObject(coord));
            Assert.IsNull(manager.GetChunkObject(new Vector3Int(99, 99, 99)));

            Object.DestroyImmediate(chunkObject);
            Object.DestroyImmediate(root);
        }

        private static CameraController CreateControllerForOrientationTests()
        {
            GameObject go = new GameObject("CameraController_Test");
            CameraController controller = go.AddComponent<CameraController>();
            return controller;
        }

        private static Quaternion ComputeTarget(CameraController controller, KeyCode key)
        {
            MethodInfo method = typeof(CameraController).GetMethod("ComputeTargetOrientation", PrivateInstance);
            Assert.IsNotNull(method);
            return (Quaternion)method.Invoke(controller, new object[] { key });
        }

        private static Quaternion GetCurrentOrientation(CameraController controller)
        {
            FieldInfo field = typeof(CameraController).GetField("_currentOrientation", PrivateInstance);
            Assert.IsNotNull(field);
            return (Quaternion)field.GetValue(controller);
        }

        private static void SetCurrentOrientation(CameraController controller, Quaternion value)
        {
            FieldInfo field = typeof(CameraController).GetField("_currentOrientation", PrivateInstance);
            Assert.IsNotNull(field);
            field.SetValue(controller, value);
        }

        private static void AssertAxisAlignedUnit(Vector3 axis)
        {
            Assert.AreEqual(1f, axis.magnitude, 0.001f);

            int nonZeroCount = 0;
            if (Mathf.Abs(axis.x) > 0.5f) nonZeroCount++;
            if (Mathf.Abs(axis.y) > 0.5f) nonZeroCount++;
            if (Mathf.Abs(axis.z) > 0.5f) nonZeroCount++;
            Assert.AreEqual(1, nonZeroCount);
        }

        private static void AssertCardinalEquals(Quaternion expected, Quaternion actual)
        {
            Assert.AreEqual(expected * Vector3.up, actual * Vector3.up);
            Assert.AreEqual(expected * Vector3.right, actual * Vector3.right);
            Assert.AreEqual(expected * Vector3.forward, actual * Vector3.forward);
        }
    }
}
