using System.Reflection;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class PlayerControllerSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            WorldGrid.Instance.ResetForGeneration();
        }

        [Test]
        public void GroundGridBelow_MatchesCameraUpNegation_ForAxisAlignedUps()
        {
            Vector3Int grid = new Vector3Int(10, 10, 10);

            Assert.AreEqual(
                new Vector3Int(10, 9, 10),
                grid + Vector3Int.RoundToInt(-new Vector3(0f, 1f, 0f)));

            Assert.AreEqual(
                new Vector3Int(10, 11, 10),
                grid + Vector3Int.RoundToInt(-new Vector3(0f, -1f, 0f)));

            Assert.AreEqual(
                new Vector3Int(9, 10, 10),
                grid + Vector3Int.RoundToInt(-new Vector3(1f, 0f, 0f)));

            Assert.AreEqual(
                new Vector3Int(10, 10, 9),
                grid + Vector3Int.RoundToInt(-new Vector3(0f, 0f, 1f)));
        }

        [Test]
        public void GameManager_Instance_SetAfterAwake()
        {
            GameObject go = new GameObject("GameManagerTest");
            GameManager gm = go.AddComponent<GameManager>();

            Assert.AreSame(gm, GameManager.Instance);

            Object.DestroyImmediate(go);
            Assert.IsNull(GameManager.Instance);
        }

        [Test]
        public void CameraOrientation_Instance_SetAfterAwake()
        {
            GameObject go = new GameObject("CameraOrientationTest");
            CameraOrientation co = go.AddComponent<CameraOrientation>();

            Assert.AreSame(co, CameraOrientation.Instance);

            Object.DestroyImmediate(go);
            Assert.IsNull(CameraOrientation.Instance);
        }

        [Test]
        public void PlayerController_OnCameraSwitchStart_ClearsVelocityAndLocks()
        {
            GameObject go = new GameObject("PlayerTest");
            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            PlayerController pc = go.AddComponent<PlayerController>();

            FieldInfo velocityField = typeof(PlayerController).GetField(
                "_velocity",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(velocityField);
            velocityField.SetValue(pc, new Vector3(3f, -2f, 1f));

            pc.OnCameraSwitchStart();

            Assert.IsTrue(pc.IsSwitching);
            Assert.AreEqual(Vector3.zero, pc.Velocity);

            pc.OnCameraSwitchComplete();
            Assert.IsFalse(pc.IsSwitching);

            Object.DestroyImmediate(go);
        }
    }
}
