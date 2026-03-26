using MakeItOut.Runtime.Dev;
using NUnit.Framework;
using UnityEngine;

namespace MakeItOut.Tests.EditMode
{
    public class DevHudFormatterTests
    {
        [Test]
        public void BuildStatusText_IncludesSeedAndPlayerState()
        {
            string hud = DevHudFormatter.BuildStatusText(
                sceneName: "DevBoot",
                seedLabel: "12345",
                generationProgress: 0.5f,
                gridPosition: new Vector3Int(10, 11, 12),
                velocity: new Vector3(1f, -2f, 3f),
                isSwitching: true,
                camUp: Vector3.up,
                camRight: Vector3.right,
                camForward: Vector3.forward);

            StringAssert.Contains("Scene: DevBoot", hud);
            StringAssert.Contains("Seed: 12345", hud);
            StringAssert.Contains("Progress: 50%", hud);
            StringAssert.Contains("Grid: (10, 11, 12)", hud);
            StringAssert.Contains("Switching: YES", hud);
            StringAssert.Contains("Vel: (1.00, -2.00, 3.00)", hud);
        }

        [Test]
        public void BuildStatusText_HandlesMissingOrientation()
        {
            string hud = DevHudFormatter.BuildStatusText(
                sceneName: "DevCorridor",
                seedLabel: "n/a",
                generationProgress: 1f,
                gridPosition: Vector3Int.zero,
                velocity: Vector3.zero,
                isSwitching: false,
                camUp: null,
                camRight: null,
                camForward: null);

            StringAssert.Contains("Up: n/a", hud);
            StringAssert.Contains("Right: n/a", hud);
            StringAssert.Contains("Forward: n/a", hud);
        }
    }
}
