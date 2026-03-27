using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    [RequireComponent(typeof(Renderer))]
    public class PlayerEmissiveOutline : MonoBehaviour
    {
        private static readonly int s_emissionId = Shader.PropertyToID("_EmissionColor");

        // Initialised in Awake() — Unity forbids creating engine objects in field initializers.
        private MaterialPropertyBlock _mpb;
        private Renderer _renderer;

        [SerializeField] private Color _emission = new Color(0.4f, 0.6f, 1f) * 0.3f;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
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
