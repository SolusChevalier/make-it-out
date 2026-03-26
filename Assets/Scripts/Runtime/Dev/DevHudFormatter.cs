using System.Globalization;
using UnityEngine;

namespace MakeItOut.Runtime.Dev
{
    public static class DevHudFormatter
    {
        public static string BuildStatusText(
            string sceneName,
            string seedLabel,
            float generationProgress,
            Vector3Int gridPosition,
            Vector3 velocity,
            bool isSwitching,
            Vector3? camUp,
            Vector3? camRight,
            Vector3? camForward)
        {
            string progressText = Mathf.RoundToInt(Mathf.Clamp01(generationProgress) * 100f).ToString(CultureInfo.InvariantCulture);
            string switchingText = isSwitching ? "YES" : "NO";

            return
                $"Scene: {sceneName}\n" +
                $"Seed: {seedLabel}\n" +
                $"Progress: {progressText}%\n" +
                $"Grid: ({gridPosition.x}, {gridPosition.y}, {gridPosition.z})\n" +
                $"Switching: {switchingText}\n" +
                $"Vel: ({velocity.x:0.00}, {velocity.y:0.00}, {velocity.z:0.00})\n" +
                $"Up: {FormatVector(camUp)}\n" +
                $"Right: {FormatVector(camRight)}\n" +
                $"Forward: {FormatVector(camForward)}";
        }

        private static string FormatVector(Vector3? value)
        {
            if (!value.HasValue)
            {
                return "n/a";
            }

            Vector3 v = value.Value;
            return $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";
        }
    }
}
