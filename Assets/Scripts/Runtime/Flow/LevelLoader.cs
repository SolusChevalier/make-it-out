using System;
using System.Collections;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using UnityEngine;

namespace MakeItOut.Runtime.Flow
{
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        public event Action OnLevelReady;

        public float LoadProgress { get; private set; }

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

        public void LoadLevel(LevelDefinition level, int levelIndex, int resolvedSeed)
        {
            StartCoroutine(LoadCoroutine(level, levelIndex, resolvedSeed));
        }

        private IEnumerator LoadCoroutine(LevelDefinition level, int levelIndex, int resolvedSeed)
        {
            LoadProgress = 0f;

            GridSession.Initialise(level, resolvedSeed);
            GameManager.Instance.ActiveLevel.Definition = level;
            GameManager.Instance.ActiveLevel.LevelIndex = levelIndex;
            GameManager.Instance.ActiveLevel.ResolvedSeed = resolvedSeed;
            GameManager.Instance.ActiveLevel.Reset();
            LoadProgress = 0.05f;
            yield return null;

            WorldGrid.Instance.Initialise(GridSession.GridSize);
            LoadProgress = 0.1f;
            yield return null;

            ChunkManager.Instance.Clear();
            LoadProgress = 0.15f;
            yield return null;

            bool generationDone = false;
            void OnGenerated() => generationDone = true;
            MazeGenerator.Instance.OnGenerationComplete += OnGenerated;
            MazeGenerator.Instance.StartGeneration(level, resolvedSeed);

            while (!generationDone)
            {
                LoadProgress = 0.15f + MazeGenerator.Instance.Progress * 0.55f;
                yield return null;
            }

            MazeGenerator.Instance.OnGenerationComplete -= OnGenerated;
            LoadProgress = 0.7f;
            yield return null;

            yield return ChunkManager.Instance.InitialiseAllChunksCoroutine(
                progress => LoadProgress = 0.7f + progress * 0.25f);

            LoadProgress = 0.95f;
            yield return null;

            EnsureFogController();

            Vector3 spawnPos = WorldGrid.Instance.GridToWorld(WorldGrid.Instance.GetCentreCell())
                             + (CameraOrientation.Instance?.Up ?? Vector3.up) * GridConfig.BlockSize;
            PlayerController.Instance.Teleport(spawnPos);
            LoadProgress = 1f;
            yield return null;

            OnLevelReady?.Invoke();
        }

        private static void EnsureFogController()
        {
            FogController fog = FindObjectOfType<FogController>();
            if (fog != null)
            {
                fog.enabled = true;
                return;
            }

            GameObject go = new GameObject("FogController");
            go.AddComponent<FogController>();
        }
    }
}
