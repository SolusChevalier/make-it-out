using UnityEngine;

namespace MakeItOut.Runtime.GridSystem
{
    public class FogController : MonoBehaviour
    {
        private void OnEnable()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.05f);

            float worldSize = GridSession.GridSize * GridConfig.BlockSize;
            RenderSettings.fogStartDistance = worldSize * 0.25f;
            RenderSettings.fogEndDistance = worldSize * 0.6f;
        }

        private void OnDisable()
        {
            RenderSettings.fog = false;
        }
    }
}
