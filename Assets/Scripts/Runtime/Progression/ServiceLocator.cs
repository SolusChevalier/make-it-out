using UnityEngine;

namespace MakeItOut.Runtime.Progression
{
    public class ServiceLocator : MonoBehaviour
    {
        public static ServiceLocator Instance { get; private set; }

        [Header("Data")]
        public LevelRegistryAsset RegistryAsset;

        public static ProgressionService Progression { get; private set; }
        public static ScoringService Scoring { get; private set; }
        public static PersistenceService Persistence { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Persistence = new PersistenceService();

            LevelRegistry registry = RegistryAsset != null && RegistryAsset.Registry != null
                ? RegistryAsset.Registry
                : new LevelRegistry();

            if (RegistryAsset == null)
                Debug.LogError("ServiceLocator requires a LevelRegistryAsset assignment.");

            Progression = new ProgressionService(registry, Persistence);
            Scoring = new ScoringService(Persistence);
        }
    }
}
