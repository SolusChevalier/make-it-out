using MakeItOut.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class HudManagerCanvasTests
    {
        [TearDown]
        public void TearDown()
        {
            if (HudManager.Instance != null)
            {
                Object.DestroyImmediate(HudManager.Instance.gameObject);
            }

            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Object.DestroyImmediate(canvases[i].gameObject);
            }
        }

        [Test]
        public void Awake_AssignsHudManagerInstance()
        {
            GameObject go = new GameObject("HudManager_Test");
            HudManager manager = go.AddComponent<HudManager>();
            Assert.AreSame(manager, HudManager.Instance);
        }
    }
}
