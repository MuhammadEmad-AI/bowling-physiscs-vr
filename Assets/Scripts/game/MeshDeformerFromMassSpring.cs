using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(VoxelMassSpringBuilder))]
public class MeshDeformerFromMassSpring : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] baseVertices;
    private VoxelMassSpringBuilder builder;

    // قائمة لأقرب نقطة MassSpring لكل vertex
    private int[] closestMassPointIndices;

    void Start()
    {
        builder = GetComponent<VoxelMassSpringBuilder>();
        mesh = GetComponent<MeshFilter>().mesh;

        if (mesh == null)
        {
            Debug.LogError("❌ Mesh not found!");
            return;
        }

        baseVertices = mesh.vertices;

        if (builder.massSpring == null || builder.massSpring.Points.Count == 0)
        {
            Debug.LogWarning("⚠️ MassSpring not ready.");
            return;
        }

        // بناء خريطة أقرب نقطة لكل رأس من رؤوس الـ Mesh
        closestMassPointIndices = new int[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 worldV = transform.TransformPoint(baseVertices[i]);
            float minDist = float.MaxValue;
            int closest = 0;

            for (int j = 0; j < builder.massSpring.Points.Count; j++)
            {
                float dist = Vector3.Distance(worldV, builder.massSpring.Points[j].Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = j;
                }
            }

            closestMassPointIndices[i] = closest;
        }

        Debug.Log("✅ Mapped mesh vertices to closest MassSpring points.");
    }

    void Update()
    {
        if (mesh == null || builder == null || builder.massSpring == null)
            return;

        var points = builder.massSpring.Points;
        Vector3[] deformedVertices = new Vector3[baseVertices.Length];

        for (int i = 0; i < baseVertices.Length; i++)
        {
            int pointIndex = closestMassPointIndices[i];
            if (pointIndex < points.Count)
            {
                // نعيد النقطة إلى local space من world space
                deformedVertices[i] = transform.InverseTransformPoint(points[pointIndex].Position);
            }
            else
            {
                deformedVertices[i] = baseVertices[i];
            }
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
    }
}
