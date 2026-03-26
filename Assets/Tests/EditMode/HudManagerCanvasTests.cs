using MakeItOut.Runtime.Player;
using NUnit.Framework;
using System.Reflection;
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
        public void Awake_WhenReferencesMissing_BuildsMinimalCanvasHierarchy()
        {
            GameObject go = new GameObject("HudManager_Test");
            HudManager manager = go.AddComponent<HudManager>();
            MethodInfo awakeMethod = typeof(HudManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(awakeMethod);
            awakeMethod.Invoke(manager, null);

            string[] expectedFields =
            {
                "LoadingPanel",
                "HudPanel",
                "WinPanel",
                "FailPanel",
                "LoadingBar",
                "LoadingLabel",
                "TimerDisplay",
                "OrientationDisplay",
                "WinTimeDisplay",
                "WinRestartButton",
                "FailReasonDisplay",
                "FailRestartButton",
            };

            for (int i = 0; i < expectedFields.Length; i++)
            {
                FieldInfo field = typeof(HudManager).GetField(expectedFields[i], BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(field, $"Expected field not found: {expectedFields[i]}");
                Assert.IsNotNull(field.GetValue(manager), $"Expected value for field {expectedFields[i]} to be assigned.");
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            Assert.IsNotNull(canvas);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);

            Assert.AreSame(canvas.transform, manager.LoadingPanel.transform.parent);
            Assert.AreSame(canvas.transform, manager.HudPanel.transform.parent);
            Assert.AreSame(canvas.transform, manager.WinPanel.transform.parent);
            Assert.AreSame(canvas.transform, manager.FailPanel.transform.parent);
        }
    }
}
