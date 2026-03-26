using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MakeItOut.Tests.PlayMode
{
    public class GameFlowTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            PlayerPrefs.DeleteAll();
            GridSession.Reset();
            yield return BuildMinimalRuntimeWorld();
            yield return WaitForState(GameState.MainMenu, 5f);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                Object.Destroy(go);
            }

            yield return null;
            ResetSingletons();
            PlayerPrefs.DeleteAll();
            GridSession.Reset();
        }

        [UnityTest]
        public IEnumerator Boot_TransitionsToMainMenu_AfterOneFrame()
        {
            yield return WaitForState(GameState.MainMenu, 5f);
            Assert.AreEqual(GameState.MainMenu, GameManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator SelectLevel_UnlockedLevel_TransitionsToLevelIntro()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;

            GameManager.Instance.SelectLevel(0);
            yield return null;

            Assert.AreEqual(GameState.LevelIntro, GameManager.Instance.CurrentState);
            Assert.AreEqual(0, GameManager.Instance.ActiveLevel.LevelIndex);
        }

        [UnityTest]
        public IEnumerator SelectLevel_LockedLevel_DoesNotTransition()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;

            GameManager.Instance.SelectLevel(1);
            yield return null;

            Assert.AreEqual(GameState.LevelSelect, GameManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator ConfirmLevelStart_TransitionsToLoadingLevel()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;
            GameManager.Instance.SelectLevel(0);
            yield return null;

            GameManager.Instance.ConfirmLevelStart();
            yield return null;

            Assert.AreEqual(GameState.LoadingLevel, GameManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator FullLevelLoad_TransitionsToPlaying()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;
            GameManager.Instance.SelectLevel(0);
            yield return null;
            GameManager.Instance.ConfirmLevelStart();

            yield return WaitForState(GameState.Playing, 30f);
            Assert.AreEqual(GameState.Playing, GameManager.Instance.CurrentState);
            Assert.IsTrue(WorldGrid.Instance.InBounds(WorldGrid.Instance.WorldToGrid(PlayerController.Instance.transform.position)));
        }

        [UnityTest]
        public IEnumerator TriggerWin_FromPlaying_TransitionsToLevelResult()
        {
            yield return EnterPlaying();
            GameManager.Instance.TriggerWin();
            yield return null;

            Assert.AreEqual(GameState.LevelResult, GameManager.Instance.CurrentState);
            Assert.IsTrue(GameManager.Instance.ActiveLevel.IsComplete);
            Assert.GreaterOrEqual(GameManager.Instance.ActiveLevel.StarsEarned, 1);
        }

        [UnityTest]
        public IEnumerator TriggerWin_FromNotPlaying_DoesNotTransition()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;
            GameManager.Instance.SelectLevel(0);
            yield return null;
            Assert.AreEqual(GameState.LevelIntro, GameManager.Instance.CurrentState);

            GameManager.Instance.TriggerWin();
            yield return null;

            Assert.AreEqual(GameState.LevelIntro, GameManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator TriggerFail_FromPlaying_TransitionsToLevelResult()
        {
            yield return EnterPlaying();
            GameManager.Instance.TriggerFail();
            yield return null;

            Assert.AreEqual(GameState.LevelResult, GameManager.Instance.CurrentState);
            Assert.IsFalse(GameManager.Instance.ActiveLevel.IsComplete);
            Assert.AreEqual(0, GameManager.Instance.ActiveLevel.StarsEarned);
        }

        [UnityTest]
        public IEnumerator RetryLevel_UsesIdenticalSeed()
        {
            yield return EnterPlaying();
            int firstSeed = GameManager.Instance.ActiveLevel.ResolvedSeed;
            GameManager.Instance.TriggerWin();
            yield return null;

            GameManager.Instance.RetryLevel();
            yield return WaitForState(GameState.Playing, 30f);

            Assert.AreEqual(firstSeed, GameManager.Instance.ActiveLevel.ResolvedSeed);
        }

        [UnityTest]
        public IEnumerator PauseRun_FreezesTimer()
        {
            yield return EnterPlaying();
            yield return new WaitForSeconds(0.25f);
            GameManager.Instance.PauseRun();
            float before = (float)GameManager.Instance.RunElapsed.TotalSeconds;
            yield return new WaitForSeconds(0.5f);
            float after = (float)GameManager.Instance.RunElapsed.TotalSeconds;

            Assert.AreEqual(GameState.Paused, GameManager.Instance.CurrentState);
            Assert.That(after, Is.EqualTo(before).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator ResumeRun_ResumesTimer()
        {
            yield return EnterPlaying();
            GameManager.Instance.PauseRun();
            yield return null;
            float before = (float)GameManager.Instance.RunElapsed.TotalSeconds;
            GameManager.Instance.ResumeRun();
            yield return new WaitForSeconds(0.5f);
            float after = (float)GameManager.Instance.RunElapsed.TotalSeconds;

            Assert.AreEqual(GameState.Playing, GameManager.Instance.CurrentState);
            Assert.Greater(after, before);
        }

        private static IEnumerator EnterPlaying()
        {
            GameManager.Instance.GoToLevelSelect();
            yield return null;
            GameManager.Instance.SelectLevel(0);
            yield return null;
            GameManager.Instance.ConfirmLevelStart();
            yield return WaitForState(GameState.Playing, 30f);
        }

        private static IEnumerator BuildMinimalRuntimeWorld()
        {
            Scene scene = SceneManager.CreateScene("PlayMode_Flow_Test");
            SceneManager.SetActiveScene(scene);

            LevelRegistryAsset registryAsset = ScriptableObject.CreateInstance<LevelRegistryAsset>();
            registryAsset.Registry = new LevelRegistry
            {
                CampaignLevels = new List<LevelDefinitionAsset>
                {
                    CreateLevelAsset("level_001", 15),
                    CreateLevelAsset("level_002", 23),
                }
            };

            GameObject serviceGo = new GameObject("ServiceLocator");
            serviceGo.SetActive(false);
            ServiceLocator service = serviceGo.AddComponent<ServiceLocator>();
            service.RegistryAsset = registryAsset;
            serviceGo.SetActive(true);

            new GameObject("CameraOrientation").AddComponent<CameraOrientation>();
            new GameObject("MazeGenerator").AddComponent<MazeGenerator>();

            GameObject chunkGo = new GameObject("ChunkManager");
            ChunkManager chunkManager = chunkGo.AddComponent<ChunkManager>();
            SetPrivateField(chunkManager, "_blockMaterial", new Material(Shader.Find("Standard")));

            GameObject player = new GameObject("Player");
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerController>();

            GameObject loaderGo = new GameObject("LevelLoader");
            loaderGo.AddComponent<LevelLoader>();

            GameObject gmGo = new GameObject("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            string sceneName = SceneManager.GetActiveScene().name;
            SetPrivateField(gm, "_mainMenuSceneName", sceneName);
            SetPrivateField(gm, "_gameSceneName", sceneName);

            yield return null;
            yield return null;
        }

        private static LevelDefinitionAsset CreateLevelAsset(string id, int size)
        {
            LevelDefinitionAsset asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
            asset.Definition = new GeneratedLevelDefinition
            {
                LevelId = id,
                DisplayName = id,
                GridSize = size,
                IsCampaignLevel = true,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 12345,
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };
            return asset;
        }

        private static IEnumerator WaitForState(GameState state, float timeoutSeconds)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == state)
                    yield break;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for state {state}");
        }

        private static void SetPrivateField<T>(T instance, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName}");
            field.SetValue(instance, value);
        }

        private static void ResetSingletons()
        {
            SetStaticAutoPropertyBackingField(typeof(ServiceLocator), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(ServiceLocator), "Progression", null);
            SetStaticAutoPropertyBackingField(typeof(ServiceLocator), "Scoring", null);
            SetStaticAutoPropertyBackingField(typeof(ServiceLocator), "Persistence", null);
            SetStaticAutoPropertyBackingField(typeof(GameManager), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(LevelLoader), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(PlayerController), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(ChunkManager), "Instance", null);
            SetStaticAutoPropertyBackingField(typeof(MazeGenerator), "Instance", null);
            SetStaticField(typeof(CameraOrientation), "Instance", null);
        }

        private static void SetStaticAutoPropertyBackingField(System.Type type, string propertyName, object value)
        {
            string backingField = $"<{propertyName}>k__BackingField";
            FieldInfo field = type.GetField(backingField, BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(null, value);
        }

        private static void SetStaticField(System.Type type, string fieldName, object value)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(null, value);
        }
    }
}
