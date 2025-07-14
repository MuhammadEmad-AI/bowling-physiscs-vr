using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ProceduralSphere : MonoBehaviour
{
    public MassSpring massSpring;
    public float springStiffness = 200f;
    public float springDamping = 10f;
    public float pointMass = 1f;

    public enum MaterialType { Solid, Rubber, Glass, Tin }
    public MaterialType materialType = MaterialType.Solid;

    public Material solidVisualMaterial;
    public Material rubberVisualMaterial;
    public Material glassVisualMaterial;
    public Material tinVisualMaterial;

    [Header("Sphere Geometry")]
    public int longitudeSegments = 24;
    public int latitudeSegments = 16;
    public int subdivisionLevels = 0;
    public float radius = 1f;

    [Header("Glass Break Settings")]
    public float breakForceThreshold = 10f;

    [Header("Sounds")]
    public AudioClip breakSound;
    public AudioClip hitPinSound;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dentDepth = 0.1f;
    public float dentRadius = 0.5f;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] originalVertices;
    private List<int> vertexToMassIndex;
    private bool isBroken = false;
    private bool isMoving = false;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;

    void Awake()
    {
        if (!Application.isPlaying) return;
        GenerateSphere();
        InitializeMassSpring();
    }

    void Start()
    {
        ApplyMaterials();
        previousPosition = transform.position;
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (!isMoving && Input.GetKeyDown(KeyCode.M)) isMoving = true;
    }

    void FixedUpdate()
    {
        if (!isMoving || isBroken) return;

        Vector3 currentPosition = transform.position;
        transform.position += Vector3.left * moveSpeed * Time.fixedDeltaTime;
        currentVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;

        Vector3 delta = transform.position - previousPosition;
        previousPosition = currentPosition;

        massSpring?.OffsetAllPoints(delta);
        massSpring?.Simulate(Time.fixedDeltaTime);

        var newPositions = massSpring.GetPositions();
        if (vertexToMassIndex == null || vertexToMassIndex.Count != vertices.Length) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            int massIndex = vertexToMassIndex[i];
            if (massIndex >= 0 && massIndex < newPositions.Count)
                vertices[i] = transform.InverseTransformPoint(newPositions[massIndex]);
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        CheckCollisionsWithAllPins();
    }

    void InitializeMassSpring()
    {
        massSpring = new MassSpring();
        vertexToMassIndex = new List<int>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldV = transform.TransformPoint(vertices[i]);
            int index = massSpring.AddOrGetPoint(worldV, pointMass);
            vertexToMassIndex.Add(index);
        }

        var points = massSpring.GetPositions();
        int count = points.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                float dist = Vector3.Distance(points[i], points[j]);
                if (dist <= 0.25f)
                    massSpring.AddSpring(i, j, springStiffness, springDamping);
            }
        }
    }

    void ApplyMaterials()
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = materialType switch
        {
            MaterialType.Rubber => rubberVisualMaterial,
            MaterialType.Glass => glassVisualMaterial,
            MaterialType.Tin => tinVisualMaterial,
            _ => solidVisualMaterial,
        };
    }

    void GenerateSphere()
    {
        mesh = new Mesh { name = "Procedural Sphere" };
        GetComponent<MeshFilter>().sharedMesh = mesh;

        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = Mathf.PI * lat / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSegments;
                Vector3 pt = new Vector3(
                    sinTheta * Mathf.Cos(phi),
                    cosTheta,
                    sinTheta * Mathf.Sin(phi)
                ) * radius;
                verts.Add(pt);
            }
        }

        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int first = lat * (longitudeSegments + 1) + lon;
                int second = first + longitudeSegments + 1;
                tris.Add(first); tris.Add(second); tris.Add(first + 1);
                tris.Add(second); tris.Add(second + 1); tris.Add(first + 1);
            }
        }

        if (subdivisionLevels > 0)
            Subdivide(verts, tris, subdivisionLevels);

        vertices = verts.ToArray();
        originalVertices = (Vector3[])vertices.Clone();
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
    }

    void Subdivide(List<Vector3> verts, List<int> tris, int levels)
    {
        var cache = new Dictionary<(int, int), int>();
        for (int level = 0; level < levels; level++)
        {
            var newTris = new List<int>();
            cache.Clear();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = GetMidpoint(a, b, verts, cache);
                int bc = GetMidpoint(b, c, verts, cache);
                int ca = GetMidpoint(c, a, verts, cache);
                newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris.Clear();
            tris.AddRange(newTris);
        }
    }

    int GetMidpoint(int i1, int i2, List<Vector3> verts, Dictionary<(int, int), int> cache)
    {
        var key = i1 < i2 ? (i1, i2) : (i2, i1);
        if (cache.TryGetValue(key, out int mid)) return mid;
        Vector3 midpoint = ((verts[i1] + verts[i2]) * 0.5f).normalized * radius;
        verts.Add(midpoint);
        mid = verts.Count - 1;
        cache[key] = mid;
        return mid;
    }

    void CheckCollisionsWithAllPins()
    {
        ProceduralBowlingPin[] pins = FindObjectsOfType<ProceduralBowlingPin>();
        Vector3 sphereCenter = transform.position;
        float radiusSqr = radius * radius;

        foreach (var pin in pins)
        {
            float h = pin.pinHeight * 0.5f;
            float r = pin.maxRadius;
            Vector3 pinCenter = pin.transform.position + Vector3.up * h;
            float boundRadius = Mathf.Sqrt(h * h + r * r);

            if (Vector3.Distance(sphereCenter, pinCenter) > radius + boundRadius) continue;

            if (IsSphereIntersectingMesh(pin, sphereCenter, radius, radiusSqr))
                HandleCollision(pin, sphereCenter, radius);
        }
    }

    bool IsSphereIntersectingMesh(ProceduralBowlingPin pin, Vector3 center, float r, float rSqr)
    {
        Mesh meshPin = pin.pinMesh;
        Vector3[] verts = meshPin.vertices;
        int[] tris = meshPin.triangles;
        Matrix4x4 l2w = pin.transform.localToWorldMatrix;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 p0 = l2w.MultiplyPoint3x4(verts[tris[i]]);
            Vector3 p1 = l2w.MultiplyPoint3x4(verts[tris[i + 1]]);
            Vector3 p2 = l2w.MultiplyPoint3x4(verts[tris[i + 2]]);
            Vector3 closest = ClosestPointOnTriangle(center, p0, p1, p2);
            if ((center - closest).sqrMagnitude <= rSqr) return true;
        }
        return false;
    }

    Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            return a + (d1 / (d1 - d3)) * ab;

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            return a + (d2 / (d2 - d6)) * ac;

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            Vector3 bc = c - b;
            return b + ((d4 - d3) / ((d4 - d3) + (d5 - d6))) * bc;
        }

        Vector3 n = Vector3.Cross(ab, ac);
        float d = Vector3.Dot(p - a, n) / n.magnitude;
        return p - n.normalized * d;
    }

    void HandleCollision(ProceduralBowlingPin pin, Vector3 center, float r)
    {
        if (isBroken) return;

        Vector3 basePt = pin.transform.position - Vector3.up * (pin.pinHeight * 0.5f - 0.11f);
        Vector3 topPt = pin.transform.position + Vector3.up * (pin.pinHeight * 0.5f - 0.11f);

        if (!SphereCapsuleCollision(center, r, basePt, topPt, 0.6f)) return;

        if (materialType == MaterialType.Glass && currentVelocity.magnitude > breakForceThreshold)
        {
            BreakSphere();
            return;
        }

        // 1. Move pin physically (basic motion)
        Vector3 pushDir = (pin.transform.position - center).normalized;
        pin.transform.position += pushDir * moveSpeed * 0.5f * Time.fixedDeltaTime;

        // 2. Apply force to pin's mass spring structure (physical deformation)
        Vector3 impactPoint = (center + pin.transform.position) * 0.5f;
        float strength = currentVelocity.magnitude * pointMass;
        pin.ApplyImpact(
            position: impactPoint,
            direction: -currentVelocity.normalized,
            magnitude: strength
        );

        // 3. Apply impulse to sphere's own mass spring (deform sphere)
        Vector3 localImpact = transform.InverseTransformPoint(impactPoint);
        massSpring?.ApplyImpulse(localImpact, -currentVelocity.normalized, strength, dentRadius);

        // 4. Apply dent effect if material is Tin
        if (materialType == MaterialType.Tin)
            ApplyTinDent(center, pin.transform.position, dentRadius, dentDepth);

        // 5. Play collision sound
        if (hitPinSound)
            AudioSource.PlayClipAtPoint(hitPinSound, center);
    }


    bool SphereCapsuleCollision(Vector3 sphereCenter, float sphereRadius, Vector3 capsuleA, Vector3 capsuleB, float capsuleRadius)
    {
        Vector3 axis = capsuleB - capsuleA;
        Vector3 toSphere = sphereCenter - capsuleA;
        float t = Mathf.Clamp(Vector3.Dot(toSphere, axis.normalized), 0f, axis.magnitude);
        Vector3 closest = capsuleA + axis.normalized * t;
        return Vector3.Distance(sphereCenter, closest) <= (sphereRadius + capsuleRadius);
    }

    void ApplyTinDent(Vector3 collisionPoint, Vector3 pinPos, float dentR, float dentD)
    {
        if (mesh == null || vertices == null || originalVertices == null) return;

        Vector3 localPt = transform.InverseTransformPoint((collisionPoint + pinPos) * 0.5f);
        for (int i = 0; i < vertices.Length; i++)
        {
            float dist = Vector3.Distance(vertices[i], localPt);
            if (dist < dentR)
            {
                Vector3 dir = (vertices[i] - localPt).normalized;
                float strength = Mathf.Lerp(dentD * 2f, 0, dist / dentR);
                vertices[i] -= dir * strength;
            }
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void BreakSphere()
    {
        isBroken = true;
        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (massSpring != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var s in massSpring.Springs)
            {
                Vector3 pA = transform.TransformPoint(massSpring.Points[s.PointA].Position);
                Vector3 pB = transform.TransformPoint(massSpring.Points[s.PointB].Position);
                Gizmos.DrawLine(pA, pB);
            }
        }
    }
}
