using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Voxelizer))]
public class VoxelMassSpringBuilder : MonoBehaviour
{
    [Header("Mass-Spring Settings")]
    public float pointMass = 0.2f;
    public float springStiffness = 200f;
    public float springDamping = 2f;
    public float connectDistance = 0.12f;

    public MassSpring massSpring;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    /// <summary>
    /// Build a Mass-Spring system from voxel points.
    /// </summary>
    public void BuildMassSpring()
    {
        Voxelizer voxelizer = GetComponent<Voxelizer>();
        if (voxelizer == null || voxelizer.voxelPoints == null || voxelizer.voxelPoints.Count == 0)
        {
            Debug.LogError("Voxel data is missing or empty!");
            return;
        }

        // Convert local voxel positions to world space
        List<Vector3> worldPoints = new List<Vector3>();
        foreach (var local in voxelizer.voxelPoints)
            worldPoints.Add(transform.TransformPoint(local));

        // Initialize and build the physics system
        massSpring = new MassSpring();
        massSpring.BuildFromVoxelPoints(worldPoints, pointMass, springStiffness, springDamping, connectDistance);

        Debug.Log($"✔ MassSpring created: {massSpring.Points.Count} points, {massSpring.Springs.Count} springs.");
    }

    void FixedUpdate()
    {
        if (massSpring == null) return;

        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        // Offset points if object has moved
        if (delta != Vector3.zero)
            massSpring.OffsetAllPoints(delta);

        massSpring.Simulate(Time.fixedDeltaTime);
    }

    void OnDrawGizmos()
    {
        if (massSpring == null || massSpring.Springs == null) return;

        Gizmos.color = Color.yellow;

        foreach (var spring in massSpring.Springs)
        {
            if (spring.PointA < 0 || spring.PointA >= massSpring.Points.Count) continue;
            if (spring.PointB < 0 || spring.PointB >= massSpring.Points.Count) continue;

            Vector3 pA = massSpring.Points[spring.PointA].Position;
            Vector3 pB = massSpring.Points[spring.PointB].Position;
            Gizmos.DrawLine(pA, pB);
        }
    }
}
