using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    public sealed class CameraOrientation : MonoBehaviour
    {
        public static CameraOrientation Instance;

        // All three are recomputed by System 5 after every camera switch.
        // For now they are set manually in the inspector or left at defaults.
        public Vector3 Up = Vector3.up;
        public Vector3 Right = Vector3.right;
        public Vector3 Forward = Vector3.forward;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
