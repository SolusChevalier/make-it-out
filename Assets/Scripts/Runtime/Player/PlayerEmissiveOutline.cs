using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    [RequireComponent(typeof(Renderer))]
    public class PlayerEmissiveOutline : MonoBehaviour
    {
        private static readonly int s_emissionId = Shader.PropertyToID("_EmissionColor");

        private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        private Renderer _renderer;

        [SerializeField] private Color _emission = new Color(0.4f, 0.6f, 1f) * 0.3f;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void LateUpdate()
        {
            if (_renderer == null)
                return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(s_emissionId, _emission);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
