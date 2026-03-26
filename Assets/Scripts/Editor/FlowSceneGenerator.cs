#if UNITY_EDITOR
using System.Collections.Generic;
using MakeItOut.Runtime.Dev;
using MakeItOut.Runtime.Flow;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using MakeItOut.Runtime.Progression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MakeItOut.EditorTools
{
    public static class FlowSceneGenerator
    {
        private const string SceneRoot = "Assets/Scenes/Flow";
        private const string BootstrapScenePath = "Assets/Scenes/Flow/Bootstrap.unity";
        private const string MainMenuScenePath = "Assets/Scenes/Flow/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/Flow/Game.unity";

        public static void CreateFlowScenes()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(SceneRoot);

            CreateBootstrapScene();
            CreateMainMenuScene();
            CreateGameScene();
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Flow scenes generated: Bootstrap, MainMenu, Game.");
        }

        [MenuItem("Tools/Make It Out/Create Flow Scenes")]
        public static void CreateFlowScenesMenu()
        {
            CreateFlowScenes();
        }

        private static void CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject serviceLocatorGo = new GameObject("ServiceLocator");
            ServiceLocator locator = serviceLocatorGo.AddComponent<ServiceLocator>();
            locator.RegistryAsset = AssetDatabase.LoadAssetAtPath<LevelRegistryAsset>("Assets/Data/LevelRegistry.asset");

            GameObject gameManagerGo = new GameObject("GameManager");
            gameManagerGo.AddComponent<GameManager>();

            GameObject levelLoaderGo = new GameObject("LevelLoader");
            levelLoaderGo.AddComponent<LevelLoader>();

            GameObject bootstrapLoaderGo = new GameObject("BootstrapSceneLoader");
            BootstrapSceneLoader loader = bootstrapLoaderGo.AddComponent<BootstrapSceneLoader>();
            loader.MainMenuSceneName = "MainMenu";

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject uiRoot = new GameObject("UIRoot");
            uiRoot.transform.SetParent(canvasGo.transform, false);
            uiRoot.AddComponent<HudManager>();

            GameObject devBootstrap = new GameObject("DevSceneBootstrap");
            devBootstrap.AddComponent<DevSceneBootstrap>();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material blockMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevBlock.mat");
            Material ladderMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevLadder.mat");
            Material stairMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevStair.mat");
            Material exitMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/DevEnv/Materials/DevExit.mat");
            Mesh cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);

            GameObject systems = new GameObject("Systems");
            systems.AddComponent<CameraOrientation>();
            MazeGenerator mazeGenerator = systems.AddComponent<MazeGenerator>();
            ChunkManager chunkManager = systems.AddComponent<ChunkManager>();
            FeaturePropRenderer featureRenderer = systems.AddComponent<FeaturePropRenderer>();
            TransparencyManager transparencyManager = systems.AddComponent<TransparencyManager>();
            systems.AddComponent<HudManager>();

            SetObjectReference(chunkManager, "_blockMaterial", blockMaterial);
            SetObjectReference(chunkManager, "_featurePropRenderer", featureRenderer);

            SetObjectReference(featureRenderer, "_ladderMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_stairMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_exitMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_ladderMaterial", ladderMaterial);
            SetObjectReference(featureRenderer, "_stairMaterial", stairMaterial);
            SetObjectReference(featureRenderer, "_exitMaterial", exitMaterial);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            PlayerController playerController = player.AddComponent<PlayerController>();

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cameraGo.AddComponent<AudioListener>();
            CameraController cameraController = cameraGo.AddComponent<CameraController>();
            SetObjectReference(cameraController, "_playerTransform", player.transform);
            SetObjectReference(cameraController, "_cam", cam);
            SetObjectReference(cameraController, "_transparencyManager", transparencyManager);

            player.transform.position = new Vector3(63f, 63f, 63f);
            cameraGo.transform.position = player.transform.position - Vector3.forward * 20f;

            // keep references alive to satisfy analyzer
            _ = mazeGenerator;
            _ = playerController;

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            int separator = folderPath.LastIndexOf('/');
            if (separator <= 0)
                return;

            string parent = folderPath.Substring(0, separator);
            string leaf = folderPath.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                return;

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
#endif
