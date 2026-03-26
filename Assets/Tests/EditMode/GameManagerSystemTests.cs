using System;
using MakeItOut.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class GameManagerSystemTests
    {
        [TearDown]
        public void TearDown()
        {
            if (GameManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(GameManager.Instance.gameObject);
            }
        }

        [Test]
        public void NotifyGenerationComplete_TransitionsToPlaying_AndStartsRunTimer()
        {
            GameObject go = new GameObject("GameManager_Test");
            GameManager manager = go.AddComponent<GameManager>();

            manager.NotifyGenerationComplete();

            Assert.AreEqual(GameState.Playing, manager.CurrentState);
            Assert.GreaterOrEqual(manager.RunElapsed, TimeSpan.Zero);
        }

        [Test]
        public void TriggerWin_DuringLoading_IsIgnored()
        {
            GameObject go = new GameObject("GameManager_Test");
            GameManager manager = go.AddComponent<GameManager>();

            Assert.AreEqual(GameState.Loading, manager.CurrentState);
            manager.TriggerWin();

            Assert.AreEqual(GameState.Loading, manager.CurrentState);
        }

        [Test]
        public void TriggerFail_DuringLoading_IsIgnored()
        {
            GameObject go = new GameObject("GameManager_Test");
            GameManager manager = go.AddComponent<GameManager>();

            Assert.AreEqual(GameState.Loading, manager.CurrentState);
            manager.TriggerFail();

            Assert.AreEqual(GameState.Loading, manager.CurrentState);
        }
    }
}
