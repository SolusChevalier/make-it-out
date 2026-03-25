using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGen : MonoBehaviour
{
    public GameObject CubePrefab, StairsPrefab;
    public int MapSizeX, MapSizeY, MapSizeZ;

    void Start()
    {
        // Determine the cell size to use so prefabs don't overlap.
        Vector3 cellSize = Vector3.one;
        if (CubePrefab != null)
        {
            cellSize = GetPrefabBoundsSize(CubePrefab);
        }

        if (StairsPrefab != null)
        {
            var stairsSize = GetPrefabBoundsSize(StairsPrefab);
            cellSize = Vector3.Max(cellSize, stairsSize);
        }

        // Avoid zero sizes and align to integer grid to prevent fractional overlap
        if (cellSize.x <= 0f) cellSize.x = 1f;
        if (cellSize.y <= 0f) cellSize.y = 1f;
        if (cellSize.z <= 0f) cellSize.z = 1f;
        cellSize = new Vector3(Mathf.Ceil(cellSize.x), Mathf.Ceil(cellSize.y), Mathf.Ceil(cellSize.z));

        for (int x = 0; x < MapSizeX; x++)
        {
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int z = 0; z < MapSizeZ; z++)
                {
                    Vector3 pos = new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);

                    // Always place a block at every layer. Optionally replace some with stairs.
                    if (y == 0)
                    {
                        if (CubePrefab != null) Instantiate(CubePrefab, pos, Quaternion.identity);
                    }
                    else
                    {
                        // Example: 50% chance to spawn something on upper layers.
                        if (Random.value > 0.5f)
                        {
                            // 50/50 chance between stairs and block when spawning.
                            if (Random.value > 0.5f)
                            {
                                if (StairsPrefab != null) Instantiate(StairsPrefab, pos, Quaternion.identity);
                            }
                            else
                            {
                                if (CubePrefab != null) Instantiate(CubePrefab, pos, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }
    }

    private Vector3 GetPrefabBoundsSize(GameObject prefab)
    {
        // Combine bounds of all renderers in the prefab
        var renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            // Fallback to transform.localScale if no renderer is present
            return prefab != null ? prefab.transform.localScale : Vector3.one;
        }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        return combined.size;
    }
}
