using MakeItOut.Runtime.GridSystem;
using MakeItOut.Runtime.Progression;
using MakeItOut.Runtime.Player;
using UnityEngine;

namespace MakeItOut.Runtime.Dev
{
    public sealed class DevCorridorBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ChunkManager _chunkManager;
        [SerializeField] private PlayerController _playerController;

        [Header("Layout")]
        [Min(6)]
        [SerializeField] private int _corridorLength = 10;
        [Min(0f)]
        [SerializeField] private float _spawnClearance = 0.1f;

        private void Awake()
        {
            if (_chunkManager == null)
            {
                _chunkManager = FindObjectOfType<ChunkManager>();
            }

            if (_playerController == null)
            {
                _playerController = FindObjectOfType<PlayerController>();
            }
        }

        private void Start()
        {
            LevelDefinition level = new GeneratedLevelDefinition
            {
                LevelId = "dev_corridor",
                DisplayName = "Dev Corridor",
                GridSize = 63,
                SeedMode = SeedMode.Fixed,
                FixedSeed = 0,
                IsCampaignLevel = false,
                StarThresholds = new[] { 60f, 120f, 180f, 240f }
            };

            GridSession.Initialise(level, 0);
            WorldGrid.Instance.Initialise(GridSession.GridSize);
            _chunkManager?.Clear();

            Vector3Int spawnCell = BuildLayout(_corridorLength);
            _chunkManager?.InitialiseAllChunks();
            PlacePlayerAtGridCell(spawnCell);
            _chunkManager?.UpdateActiveChunks(spawnCell);
        }

        public static Vector3Int BuildLayout(int corridorLength)
        {
            int clampedLength = Mathf.Max(6, corridorLength);
            WorldGrid.Instance.ResetForGeneration();

            for (int z = 0; z < GridSession.GridSize; z++)
            {
                for (int y = 0; y < GridSession.GridSize; y++)
                {
                    for (int x = 0; x < GridSession.GridSize; x++)
                    {
                        WorldGrid.Instance.SetBlock(x, y, z, BlockType.Solid);
                    }
                }
            }

            Vector3Int centre = WorldGrid.Instance.GetCentreCell();
            int yBase = centre.y;
            int zBase = centre.z;

            for (int x = centre.x; x <= centre.x + clampedLength; x++)
            {
                SetAir(x, yBase, zBase);
                SetAir(x, yBase + 1, zBase);
            }

            int ladderX = centre.x + 2;
            for (int y = yBase; y <= yBase + 3; y++)
            {
                SetAir(ladderX, y, zBase);
            }

            WorldGrid.Instance.SetFeature(ladderX, yBase + 1, zBase, FeatureType.Ladder);
            WorldGrid.Instance.SetFeature(ladderX, yBase + 2, zBase, FeatureType.Ladder);

            int stepX = centre.x + 5;
            WorldGrid.Instance.SetBlock(stepX, yBase, zBase, BlockType.Solid);
            SetAir(stepX, yBase + 1, zBase);

            for (int x = stepX + 1; x <= centre.x + clampedLength; x++)
            {
                SetAir(x, yBase + 1, zBase);
                SetAir(x, yBase + 2, zBase);
            }

            WorldGrid.Instance.SetFeature(centre.x, yBase, zBase, FeatureType.Start);
            WorldGrid.Instance.SetFeature(centre.x + clampedLength, yBase + 1, zBase, FeatureType.Exit);

            return new Vector3Int(centre.x, yBase, zBase);
        }

        private static void SetAir(int x, int y, int z)
        {
            WorldGrid.Instance.SetBlock(x, y, z, BlockType.Air);
        }

        private void PlacePlayerAtGridCell(Vector3Int gridCell)
        {
            if (_playerController == null)
            {
                return;
            }

            Transform playerTransform = _playerController.transform;
            Vector3 camUp = CameraOrientation.Instance != null ? CameraOrientation.Instance.Up : Vector3.up;
            Vector3 spawnWorld = WorldGrid.Instance.GridToWorld(gridCell);

            CharacterController cc = _playerController.GetComponent<CharacterController>();
            float halfHeight = cc != null ? cc.height * 0.5f : 0.9f;

            playerTransform.position = spawnWorld + camUp * (halfHeight + _spawnClearance);
        }
    }
}
