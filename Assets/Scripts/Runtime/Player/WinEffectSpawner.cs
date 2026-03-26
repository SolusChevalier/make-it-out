using UnityEngine;

namespace MakeItOut.Runtime.Player
{
    public class WinEffectSpawner : MonoBehaviour
    {
        public static WinEffectSpawner Instance { get; private set; }

        [SerializeField] private ParticleSystem _winBurstPrefab;

        public static void EnsureInstance()
        {
            if (Instance != null)
                return;

            WinEffectSpawner existing = FindObjectOfType<WinEffectSpawner>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            GameObject go = new GameObject("WinEffectSpawner");
            go.AddComponent<WinEffectSpawner>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SpawnAt(Vector3 worldPosition)
        {
            ParticleSystem ps = _winBurstPrefab != null
                ? Instantiate(_winBurstPrefab, worldPosition, Quaternion.identity)
                : CreateFallbackBurst(worldPosition);

            Destroy(ps.gameObject, 3f);
        }

        private static ParticleSystem CreateFallbackBurst(Vector3 worldPosition)
        {
            GameObject go = new GameObject("WinBurstFallback");
            go.transform.position = worldPosition;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.gravityModifier = 0f;
            main.maxParticles = 128;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.6f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = g;

            ps.Play();
            return ps;
        }
    }
}
