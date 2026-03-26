#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MakeItOut.Runtime.Dev;
using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.MazeGeneration;
using MakeItOut.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MakeItOut.EditorTools
{
    public static class DevEnvSceneGenerator
    {
        private const string RootFolder = "Assets/Scenes/DevEnv";
        private const string MaterialsFolder = "Assets/Scenes/DevEnv/Materials";
        private const string DevBootScenePath = "Assets/Scenes/DevEnv/DevBoot.unity";
        private const string DevCorridorScenePath = "Assets/Scenes/DevEnv/DevCorridor.unity";

        [MenuItem("Tools/Make It Out/Create DevEnv Scenes")]
        public static void CreateDevEnvScenes()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(RootFolder);
            EnsureFolder(MaterialsFolder);

            Material blockMaterial = LoadOrCreateMaterial($"{MaterialsFolder}/DevBlock.mat", Color.gray);
            Material ladderMaterial = LoadOrCreateMaterial($"{MaterialsFolder}/DevLadder.mat", new Color(0.95f, 0.8f, 0.2f));
            Material stairMaterial = LoadOrCreateMaterial($"{MaterialsFolder}/DevStair.mat", new Color(0.2f, 0.8f, 0.95f));
            Material exitMaterial = LoadOrCreateMaterial($"{MaterialsFolder}/DevExit.mat", new Color(0.2f, 0.95f, 0.25f));
            Mesh cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);

            CreateDevBootScene(blockMaterial, ladderMaterial, stairMaterial, exitMaterial, cubeMesh);
            CreateDevCorridorScene(blockMaterial, ladderMaterial, stairMaterial, exitMaterial, cubeMesh);
            AddScenesToBuildSettings(DevBootScenePath, DevCorridorScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DevEnv scenes created: DevBoot and DevCorridor.");
        }

        private static void CreateDevBootScene(
            Material blockMaterial,
            Material ladderMaterial,
            Material stairMaterial,
            Material exitMaterial,
            Mesh cubeMesh)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject systemsRoot = new GameObject("Systems");

            systemsRoot.AddComponent<GameManager>();
            systemsRoot.AddComponent<CameraOrientation>();
            MazeGenerator mazeGenerator = systemsRoot.AddComponent<MazeGenerator>();
            ChunkManager chunkManager = systemsRoot.AddComponent<ChunkManager>();
            FeaturePropRenderer featureRenderer = systemsRoot.AddComponent<FeaturePropRenderer>();
            TransparencyManager transparencyManager = systemsRoot.AddComponent<TransparencyManager>();
            DevSceneBootstrap bootstrap = systemsRoot.AddComponent<DevSceneBootstrap>();
            Component hudOverlay = AddComponentByName(systemsRoot, "MakeItOut.Runtime.Dev.DevHudOverlay");

            GameObject player = CreatePlayerObject();
            PlayerController playerController = player.GetComponent<PlayerController>();

            (_, Camera cam) = CreateCameraObject(player.transform, transparencyManager);

            ConfigureChunkManager(chunkManager, blockMaterial, featureRenderer);
            ConfigureFeatureRenderer(featureRenderer, cubeMesh, ladderMaterial, stairMaterial, exitMaterial);
            ConfigureDevSceneBootstrap(bootstrap, mazeGenerator, chunkManager, playerController);
            ConfigureHud(hudOverlay, playerController, mazeGenerator, bootstrap);

            player.transform.position = new Vector3(
                GridSession.GridSize * GridConfig.BlockSize * 0.5f,
                GridSession.GridSize * GridConfig.BlockSize * 0.5f,
                GridSession.GridSize * GridConfig.BlockSize * 0.5f);

            cam.transform.position = player.transform.position - Vector3.forward * 20f;
            cam.transform.rotation = Quaternion.identity;

            EditorSceneManager.SaveScene(scene, DevBootScenePath);
        }

        private static void CreateDevCorridorScene(
            Material blockMaterial,
            Material ladderMaterial,
            Material stairMaterial,
            Material exitMaterial,
            Mesh cubeMesh)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject systemsRoot = new GameObject("Systems");

            systemsRoot.AddComponent<GameManager>();
            systemsRoot.AddComponent<CameraOrientation>();
            ChunkManager chunkManager = systemsRoot.AddComponent<ChunkManager>();
            FeaturePropRenderer featureRenderer = systemsRoot.AddComponent<FeaturePropRenderer>();
            TransparencyManager transparencyManager = systemsRoot.AddComponent<TransparencyManager>();
            DevCorridorBootstrap corridorBootstrap = systemsRoot.AddComponent<DevCorridorBootstrap>();
            Component hudOverlay = AddComponentByName(systemsRoot, "MakeItOut.Runtime.Dev.DevHudOverlay");

            GameObject player = CreatePlayerObject();
            PlayerController playerController = player.GetComponent<PlayerController>();

            (_, Camera cam) = CreateCameraObject(player.transform, transparencyManager);

            ConfigureChunkManager(chunkManager, blockMaterial, featureRenderer);
            ConfigureFeatureRenderer(featureRenderer, cubeMesh, ladderMaterial, stairMaterial, exitMaterial);
            ConfigureDevCorridorBootstrap(corridorBootstrap, chunkManager, playerController);
            ConfigureHud(hudOverlay, playerController, null, null);

            player.transform.position = new Vector3(
                GridSession.GridSize * GridConfig.BlockSize * 0.5f,
                GridSession.GridSize * GridConfig.BlockSize * 0.5f,
                GridSession.GridSize * GridConfig.BlockSize * 0.5f);

            cam.transform.position = player.transform.position - Vector3.forward * 20f;
            cam.transform.rotation = Quaternion.identity;

            EditorSceneManager.SaveScene(scene, DevCorridorScenePath);
        }

        private static GameObject CreatePlayerObject()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";

            Collider primitiveCollider = player.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Object.DestroyImmediate(primitiveCollider);
            }

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            player.AddComponent<PlayerController>();
            return player;
        }

        private static (CameraController controller, Camera cam) CreateCameraObject(
            Transform playerTransform,
            TransparencyManager transparencyManager)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            Camera cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cameraGo.AddComponent<AudioListener>();

            CameraController cameraController = cameraGo.AddComponent<CameraController>();
            SetObjectReference(cameraController, "_playerTransform", playerTransform);
            SetObjectReference(cameraController, "_cam", cam);
            SetObjectReference(cameraController, "_transparencyManager", transparencyManager);
            return (cameraController, cam);
        }

        private static void ConfigureChunkManager(
            ChunkManager chunkManager,
            Material blockMaterial,
            FeaturePropRenderer featureRenderer)
        {
            SetObjectReference(chunkManager, "_blockMaterial", blockMaterial);
            SetObjectReference(chunkManager, "_featurePropRenderer", featureRenderer);
        }

        private static void ConfigureFeatureRenderer(
            FeaturePropRenderer featureRenderer,
            Mesh cubeMesh,
            Material ladderMaterial,
            Material stairMaterial,
            Material exitMaterial)
        {
            SetObjectReference(featureRenderer, "_ladderMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_stairMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_exitMesh", cubeMesh);
            SetObjectReference(featureRenderer, "_ladderMaterial", ladderMaterial);
            SetObjectReference(featureRenderer, "_stairMaterial", stairMaterial);
            SetObjectReference(featureRenderer, "_exitMaterial", exitMaterial);
        }

        private static void ConfigureDevSceneBootstrap(
            DevSceneBootstrap bootstrap,
            MazeGenerator mazeGenerator,
            ChunkManager chunkManager,
            PlayerController playerController)
        {
            SetObjectReference(bootstrap, "_mazeGenerator", mazeGenerator);
            SetObjectReference(bootstrap, "_chunkManager", chunkManager);
            SetObjectReference(bootstrap, "_playerController", playerController);
            SetBool(bootstrap, "_useRandomSeed", true);
        }

        private static void ConfigureDevCorridorBootstrap(
            DevCorridorBootstrap bootstrap,
            ChunkManager chunkManager,
            PlayerController playerController)
        {
            SetObjectReference(bootstrap, "_chunkManager", chunkManager);
            SetObjectReference(bootstrap, "_playerController", playerController);
            SetInt(bootstrap, "_corridorLength", 10);
        }

        private static void ConfigureHud(
            Component hudOverlay,
            PlayerController playerController,
            MazeGenerator mazeGenerator,
            DevSceneBootstrap bootstrap)
        {
            if (hudOverlay == null)
            {
                return;
            }

            SetObjectReference(hudOverlay, "_playerController", playerController);
            SetObjectReference(hudOverlay, "_mazeGenerator", mazeGenerator);
            SetObjectReference(hudOverlay, "_devSceneBootstrap", bootstrap);
        }

        private static Material LoadOrCreateMaterial(string assetPath, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                if (!existing.enableInstancing)
                {
                    existing.enableInstancing = true;
                    EditorUtility.SetDirty(existing);
                }

                return existing;
            }

            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color,
            };
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        private static Component AddComponentByName(GameObject target, string fullTypeName)
        {
            for (int i = 0; i < AppDomain.CurrentDomain.GetAssemblies().Length; i++)
            {
                System.Reflection.Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()[i];
                System.Type type = assembly.GetType(fullTypeName);
                if (type == null)
                {
                    continue;
                }

                if (!typeof(Component).IsAssignableFrom(type))
                {
                    return null;
                }

                return target.AddComponent(type);
            }

            Debug.LogWarning($"Could not resolve component type '{fullTypeName}'.");
            return null;
        }

        private static void AddScenesToBuildSettings(params string[] scenePaths)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string path = scenePaths[i];
                bool alreadyAdded = false;
                for (int j = 0; j < scenes.Count; j++)
                {
                    if (scenes[j].path == path)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int separatorIndex = folderPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return;
            }

            string parent = folderPath.Substring(0, separatorIndex);
            string leaf = folderPath.Substring(separatorIndex + 1);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
