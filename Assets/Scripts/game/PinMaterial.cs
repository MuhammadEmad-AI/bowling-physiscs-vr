

using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(Voxelizer))]
public class ProceduralBowlingPin : MonoBehaviour
{
    public enum PhysicsType { Solid, Rubber, Glass }

    [Header("Pin Profile")]
    public int radialSegments = 24;
    public int heightSegments = 32;
    public float pinHeight = 1.2f;
    public float maxRadius = 0.3f;
    public float bottomRadius = 0.15f;
    public float neckRadius = 0.05f;

    [Header("Mass-Spring Physics")]
    public float pointMass = 0.2f;
    public float springStiffness = 200f;
    public float springDamping = 2f;
    public float voxelConnectDistance = 0.12f;

    [HideInInspector]
    public MassSpring massSpring;

    [Header("Rendering Materials")]
    public PhysicsType physicsType = PhysicsType.Solid;
    public Material solidVisual;
    public Material rubberVisual;
    public Material glassVisual;

    [Header("Glass Break Effects")]
    public AudioClip glassBreakClip;
    public float breakSoundThreshold = 5f;
    public GameObject glassShatterPrefab;

    [HideInInspector]
    public Mesh pinMesh;

    private Voxelizer voxelizer;

    void Awake()
    {
        GetComponent<Voxelizer>()?.GenerateVoxels();
        GetComponent<VoxelMassSpringBuilder>()?.BuildMassSpring();
    }

    void Start()
    {
        BuildMesh();
        SetupVisual();

        voxelizer = GetComponent<Voxelizer>();
        voxelizer.GenerateVoxels();

        BuildMassSpringFromVoxels();
    }

    void FixedUpdate()
    {
        if (Application.isPlaying && massSpring != null)
        {
            massSpring.Simulate(Time.fixedDeltaTime);
            UpdateMeshVertices();
        }
    }
    void Update()
    {
        if (!Application.isPlaying)
        {
            BuildMesh();
            SetupVisual();
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            BuildMesh();
            SetupVisual();
        }
    }
    void BuildMassSpringFromVoxels()
    {
        massSpring = new MassSpring();

        List<Vector3> voxels = voxelizer.voxelPoints;
        int count = voxels.Count;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(voxels[i]);
            massSpring.AddMassPoint(worldPos, pointMass);
        }

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                float dist = Vector3.Distance(voxels[i], voxels[j]);
                if (dist <= voxelConnectDistance)
                {
                    massSpring.AddSpring(i, j, springStiffness, springDamping);
                }
            }
        }

        Debug.Log($"✅ Voxel-based MassSpring created: {massSpring.Points.Count} points, {massSpring.Springs.Count} springs.");
    }

    void UpdateMeshVertices()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        var verts = mesh.vertices;
        var updatedPositions = massSpring.GetPositions();

        if (updatedPositions.Count != verts.Length) return;

        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = transform.InverseTransformPoint(updatedPositions[i]);
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }

    public void ApplyImpact(Vector3 position, Vector3 direction, float magnitude)
    {
        massSpring?.ApplyImpulse(position, direction, magnitude, influenceRadius: 0.3f);

        if (physicsType == PhysicsType.Glass && magnitude > breakSoundThreshold)
        {
            if (glassBreakClip)
                AudioSource.PlayClipAtPoint(glassBreakClip, transform.position);
            if (glassShatterPrefab)
                Instantiate(glassShatterPrefab, transform.position, transform.rotation);

            Destroy(gameObject);
        }
    }

    void BuildMesh()
    {
        pinMesh = new Mesh { name = "ProceduralBowlingPin" };
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector2> uvs = new();

        Vector2[] profile = new Vector2[heightSegments + 1];
        for (int i = 0; i <= heightSegments; i++)
        {
            float t = (float)i / heightSegments;
            float y = t * pinHeight;
            float r = Mathf.Lerp(bottomRadius, maxRadius, Mathf.Sin(t * Mathf.PI));
            r = Mathf.Lerp(r, neckRadius, t * t);
            profile[i] = new Vector2(y, r);
        }

        int rings = radialSegments + 1;
        for (int i = 0; i <= heightSegments; i++)
        {
            Vector2 p = profile[i];
            for (int j = 0; j < rings; j++)
            {
                float theta = (float)j / radialSegments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(theta) * p.y, p.x, Mathf.Sin(theta) * p.y));
                uvs.Add(new Vector2((float)j / radialSegments, p.x / pinHeight));
            }
        }

        for (int i = 0; i < heightSegments; i++)
        {
            int row = i * rings;
            int next = (i + 1) * rings;
            for (int j = 0; j < radialSegments; j++)
            {
                int a = row + j;
                int b = next + j;
                int c = a + 1;
                int d = b + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(d);
            }
        }

        int bottomCenter = verts.Count;
        verts.Add(new Vector3(0, 0, 0));
        uvs.Add(new Vector2(0.5f, 0f));
        for (int j = 0; j < radialSegments; j++)
        {
            tris.Add(bottomCenter);
            tris.Add(j + 1);
            tris.Add(j);
        }

        int topCenter = verts.Count;
        verts.Add(new Vector3(0, pinHeight, 0));
        uvs.Add(new Vector2(0.5f, 1f));
        int baseRing = heightSegments * rings;
        for (int j = 0; j < radialSegments; j++)
        {
            tris.Add(topCenter);
            tris.Add(baseRing + j);
            tris.Add(baseRing + j + 1);
        }

        pinMesh.SetVertices(verts);
        pinMesh.SetTriangles(tris, 0);
        pinMesh.SetUVs(0, uvs);
        pinMesh.RecalculateNormals();
        pinMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = pinMesh;
    }

    void SetupVisual()
    {
        var rend = GetComponent<MeshRenderer>();
        switch (physicsType)
        {
            case PhysicsType.Rubber: rend.sharedMaterial = rubberVisual; break;
            case PhysicsType.Glass: rend.sharedMaterial = glassVisual; break;
            default: rend.sharedMaterial = solidVisual; break;
        }
    }
}

