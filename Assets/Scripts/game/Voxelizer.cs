using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class Voxelizer : MonoBehaviour
{
    [Header("Voxel Settings")]
    [Tooltip("Size of each voxel (smaller size = higher resolution)")]
    public float voxelSize = 0.1f;

    [Tooltip("Draw voxel wireframes in the scene view")]
    public bool drawGizmos = true;

    [HideInInspector]
    public List<Vector3> voxelPoints = new();

    private MeshCollider meshCollider;

    /// <summary>
    /// Generate a voxel grid inside the mesh bounds.
    /// </summary>
    public void GenerateVoxels()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Generating voxels during play mode is not recommended.");
            return;
        }

        voxelPoints.Clear();

        Mesh mesh = GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh == null)
        {
            Debug.LogWarning("❌ Mesh not found!");
            return;
        }

        meshCollider = GetComponent<MeshCollider>();
        bool addedTempCollider = false;

        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            addedTempCollider = true;
        }

        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        // Force collider update
        meshCollider.enabled = false;
        meshCollider.enabled = true;

        Bounds bounds = mesh.bounds;
        Vector3 padding = Vector3.one * voxelSize * 0.5f;
        Vector3 min = bounds.min + padding;
        Vector3 max = bounds.max - padding;

        int count = 0;

        for (float x = min.x; x <= max.x; x += voxelSize)
        {
            for (float y = min.y; y <= max.y; y += voxelSize)
            {
                for (float z = min.z; z <= max.z; z += voxelSize)
                {
                    Vector3 localPoint = new Vector3(x, y, z);
                    Vector3 worldPoint = transform.TransformPoint(localPoint);

                    if (IsInsideMesh(worldPoint))
                    {
                        voxelPoints.Add(localPoint);
                        count++;
                    }
                }
            }
        }

        if (addedTempCollider)
        {
            DestroyImmediate(meshCollider); // Clean up temporary collider
        }

        Debug.Log($"✔ Generated {count} voxel points.");
    }

    /// <summary>
    /// Check if a point is inside the mesh using physics overlap.
    /// </summary>
    private bool IsInsideMesh(Vector3 point)
    {
        return Physics.OverlapSphere(point, voxelSize * 0.25f)
                      .Any(c => c == meshCollider);
    }

    /// <summary>
    /// Display voxel gizmos in the scene view (Editor only).
    /// </summary>
    void OnDrawGizmos()
    {
        if (!drawGizmos || voxelPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (var pt in voxelPoints)
        {
            Gizmos.DrawWireCube(transform.TransformPoint(pt), Vector3.one * voxelSize * 0.9f);
        }
    }
}
