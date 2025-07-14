// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;

// [ExecuteInEditMode]
// public class ProceduralSphere : MonoBehaviour
// {
//     public MassSpring massSpring;
//     public float springStiffness = 500f;
//     public float springDamping = 5f;
//     public float pointMass = 1f;

//     public enum MaterialType { Solid, Rubber, Glass, Tin }

//     [Tooltip("اختر نوع المادة الفيزيائية للكرة")]
//     public MaterialType materialType = MaterialType.Solid;

//     public Material solidVisualMaterial;
//     public Material rubberVisualMaterial;
//     public Material glassVisualMaterial;
//     public Material tinVisualMaterial;

//     [Header("Sphere Geometry")]
//     public int longitudeSegments = 24;
//     public int latitudeSegments = 16;
//     public int subdivisionLevels = 0;
//     public float radius = 1f;

//     [Header("Glass Break Settings")]
//     public float breakForceThreshold = 10f;

//     [Header("Sounds")]
//     public AudioClip breakSound;
//     public AudioClip hitPinSound;

//     [Header("Movement Settings")]
//     public float moveSpeed = 5f;
//     public float dentDepth = 0.1f;
//     public float dentRadius = 0.3f;

//     private Mesh mesh;
//     private Vector3[] vertices;
//     private Vector3[] originalVertices;
//     private bool isBroken = false;
//     private bool isMoving = false;
//     private Vector3 previousPosition;
//     private Vector3 currentVelocity;


//     void Awake()
//     {
//         GenerateSphere();
//         InitializeMassSpring();
//     }



//     void Start()
//     {
//         ApplyMaterials();

//         previousPosition = transform.position;
//     }



//     void Update()
//     {
// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//         {
//             ApplyMaterials();
//             GenerateSphere();
//             return;
//         }
// #endif
//         if (!isMoving && Input.GetKeyDown(KeyCode.M))
//         {
//             isMoving = true;
//         }
//     }

//     void InitializeMassSpring()
//     {
//         massSpring = new MassSpring();

//         foreach (var v in vertices)
//             massSpring.AddMassPoint(v, pointMass, false);
//     }




//     void OnDrawGizmos()
//     {
//         if (massSpring != null)
//         {
//             Gizmos.color = Color.yellow;
//             foreach (var spring in massSpring.Springs)
//             {
//                 Vector3 pA = transform.TransformPoint(massSpring.Points[spring.PointA].Position);
//                 Vector3 pB = transform.TransformPoint(massSpring.Points[spring.PointB].Position);
//                 Gizmos.DrawLine(pA, pB);
//             }
//         }
//     }

//     void FixedUpdate()
//     {
//         if (isMoving && !isBroken)
//         {
//             Vector3 currentPosition = transform.position;
//             transform.position += Vector3.left * moveSpeed * Time.fixedDeltaTime;
//             currentVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
//             previousPosition = currentPosition;

//             massSpring?.Simulate(Time.fixedDeltaTime);

//             var newPositions = massSpring.GetPositions();

//             int count = Mathf.Min(vertices.Length, newPositions.Count);
//             for (int i = 0; i < count; i++)
//             {
//                 vertices[i] = newPositions[i];
//             }

//             mesh.vertices = vertices;
//             mesh.RecalculateNormals();

//             CheckCollisionsWithAllPins();
//         }
//     }


//     void ApplyMaterials()
//     {
//         var renderer = GetComponent<MeshRenderer>();
//         switch (materialType)
//         {
//             case MaterialType.Solid:
//                 renderer.sharedMaterial = solidVisualMaterial;
//                 break;
//             case MaterialType.Rubber:
//                 renderer.sharedMaterial = rubberVisualMaterial;
//                 break;
//             case MaterialType.Glass:
//                 renderer.sharedMaterial = glassVisualMaterial;
//                 break;
//             case MaterialType.Tin:
//                 renderer.sharedMaterial = tinVisualMaterial;
//                 break;
//         }
//     }

//     void GenerateSphere()
//     {
//         mesh = new Mesh { name = "Procedural Sphere" };
//         GetComponent<MeshFilter>().sharedMesh = mesh;

//         var verts = new List<Vector3>();
//         var tris = new List<int>();

//         for (int lat = 0; lat <= latitudeSegments; lat++)
//         {
//             float theta = Mathf.PI * lat / latitudeSegments;
//             float sinTheta = Mathf.Sin(theta);
//             float cosTheta = Mathf.Cos(theta);
//             for (int lon = 0; lon <= longitudeSegments; lon++)
//             {
//                 float phi = 2f * Mathf.PI * lon / longitudeSegments;
//                 Vector3 pt = new Vector3(
//                     sinTheta * Mathf.Cos(phi),
//                     cosTheta,
//                     sinTheta * Mathf.Sin(phi)
//                 ) * radius;
//                 verts.Add(pt);
//             }
//         }

//         for (int lat = 0; lat < latitudeSegments; lat++)
//         {
//             for (int lon = 0; lon < longitudeSegments; lon++)
//             {
//                 int first = lat * (longitudeSegments + 1) + lon;
//                 int second = first + longitudeSegments + 1;
//                 tris.Add(first); tris.Add(second); tris.Add(first + 1);
//                 tris.Add(second); tris.Add(second + 1); tris.Add(first + 1);
//             }
//         }

//         if (subdivisionLevels > 0)
//             Subdivide(verts, tris, subdivisionLevels);

//         vertices = verts.ToArray();
//         originalVertices = (Vector3[])vertices.Clone();

//         mesh.vertices = vertices;
//         mesh.triangles = tris.ToArray();
//         mesh.RecalculateNormals();
//     }

//     void Subdivide(List<Vector3> verts, List<int> tris, int levels)
//     {
//         var cache = new Dictionary<(int, int), int>();
//         for (int level = 0; level < levels; level++)
//         {
//             var newTris = new List<int>();
//             cache.Clear();
//             for (int i = 0; i < tris.Count; i += 3)
//             {
//                 int a = tris[i], b = tris[i + 1], c = tris[i + 2];
//                 int ab = GetMidpoint(a, b, verts, cache);
//                 int bc = GetMidpoint(b, c, verts, cache);
//                 int ca = GetMidpoint(c, a, verts, cache);
//                 newTris.AddRange(new[] { a, ab, ca });
//                 newTris.AddRange(new[] { b, bc, ab });
//                 newTris.AddRange(new[] { c, ca, bc });
//                 newTris.AddRange(new[] { ab, bc, ca });
//             }
//             tris.Clear();
//             tris.AddRange(newTris);
//         }
//     }

//     int GetMidpoint(int i1, int i2, List<Vector3> verts, Dictionary<(int, int), int> cache)
//     {
//         var key = i1 < i2 ? (i1, i2) : (i2, i1);
//         if (cache.TryGetValue(key, out int mid)) return mid;
//         Vector3 midpoint = ((verts[i1] + verts[i2]) * 0.5f).normalized * radius;
//         verts.Add(midpoint);
//         mid = verts.Count - 1;
//         cache[key] = mid;
//         return mid;
//     }

//     void CheckCollisionsWithAllPins()
//     {
//         ProceduralBowlingPin[] pins = FindObjectsOfType<ProceduralBowlingPin>();
//         Vector3 sphereCenter = transform.position;
//         float sphereRadius = radius;
//         float radiusSqr = sphereRadius * sphereRadius;

//         foreach (var pin in pins)
//         {
//             float pinHalfHeight = pin.pinHeight * 0.5f;
//             float pinMaxRadius = pin.maxRadius;
//             Vector3 pinWorldCenter = pin.transform.position + Vector3.up * pinHalfHeight;
//             float pinBoundingRadius = Mathf.Sqrt(pinHalfHeight * pinHalfHeight + pinMaxRadius * pinMaxRadius);

//             float distCenters = Vector3.Distance(sphereCenter, pinWorldCenter);
//             if (distCenters > (sphereRadius + pinBoundingRadius)) continue;

//             if (IsSphereIntersectingMesh(pin, sphereCenter, sphereRadius, radiusSqr))
//             {
//                 HandleCollision(pin, transform.position, sphereRadius);
//             }
//         }
//     }

//     bool IsSphereIntersectingMesh(ProceduralBowlingPin pin, Vector3 sphereCenter, float sphereRadius, float radiusSqr)
//     {
//         Mesh meshPin = pin.pinMesh;
//         Vector3[] verts = meshPin.vertices;
//         int[] tris = meshPin.triangles;
//         Matrix4x4 localToWorld = pin.transform.localToWorldMatrix;

//         for (int i = 0; i < tris.Length; i += 3)
//         {
//             Vector3 p0 = localToWorld.MultiplyPoint3x4(verts[tris[i]]);
//             Vector3 p1 = localToWorld.MultiplyPoint3x4(verts[tris[i + 1]]);
//             Vector3 p2 = localToWorld.MultiplyPoint3x4(verts[tris[i + 2]]);
//             Vector3 closestPt = ClosestPointOnTriangle(sphereCenter, p0, p1, p2);
//             float distSqr = (sphereCenter - closestPt).sqrMagnitude;
//             if (distSqr <= radiusSqr) return true;
//         }
//         return false;
//     }

//     Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
//     {
//         Vector3 ab = b - a;
//         Vector3 ac = c - a;
//         Vector3 ap = point - a;

//         float d1 = Vector3.Dot(ab, ap);
//         float d2 = Vector3.Dot(ac, ap);
//         if (d1 <= 0f && d2 <= 0f) return a;

//         Vector3 bp = point - b;
//         float d3 = Vector3.Dot(ab, bp);
//         float d4 = Vector3.Dot(ac, bp);
//         if (d3 >= 0f && d4 <= d3) return b;

//         float vc = d1 * d4 - d3 * d2;
//         if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + (d1 / (d1 - d3)) * ab;

//         Vector3 cp = point - c;
//         float d5 = Vector3.Dot(ab, cp);
//         float d6 = Vector3.Dot(ac, cp);
//         if (d6 >= 0f && d5 <= d6) return c;

//         float vb = d5 * d2 - d1 * d6;
//         if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + (d2 / (d2 - d6)) * ac;

//         float va = d3 * d6 - d5 * d4;
//         if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
//         {
//             Vector3 bc = c - b;
//             return b + ((d4 - d3) / ((d4 - d3) + (d5 - d6))) * bc;
//         }

//         Vector3 normal = Vector3.Cross(ab, ac);
//         float distance = Vector3.Dot(point - a, normal) / normal.magnitude;
//         return point - normal.normalized * distance;
//     }

//     bool SphereCapsuleCollision(Vector3 sphereCenter, float sphereRadius,
//                                 Vector3 capsulePointA, Vector3 capsulePointB, float capsuleRadius)
//     {
//         Vector3 capsuleAxis = capsulePointB - capsulePointA;
//         Vector3 toSphere = sphereCenter - capsulePointA;

//         float t = Vector3.Dot(toSphere, capsuleAxis.normalized);
//         t = Mathf.Clamp(t, 0f, capsuleAxis.magnitude);

//         Vector3 closestPoint = capsulePointA + capsuleAxis.normalized * t;
//         float dist = Vector3.Distance(sphereCenter, closestPoint);
//         return dist <= (sphereRadius + capsuleRadius);
//     }

//     void HandleCollision(ProceduralBowlingPin pin, Vector3 sphereCenter, float sphereRadius)
//     {
//         if (isBroken) return;

//         Vector3 pinBase = pin.transform.position - Vector3.up * (pin.pinHeight * 0.5f - 0.11f);
//         Vector3 pinTop = pin.transform.position + Vector3.up * (pin.pinHeight * 0.5f - 0.11f);
//         float pinRadius = 0.6f;

//         bool collided = SphereCapsuleCollision(sphereCenter, sphereRadius, pinBase, pinTop, pinRadius);
//         if (!collided) return;

//         if (materialType == MaterialType.Glass)
//         {
//             float deltaV = currentVelocity.magnitude;
//             float impactForce = deltaV;

//             if (impactForce > breakForceThreshold)
//             {
//                 BreakSphere();
//                 return;
//             }
//         }

//         Vector3 pushDir = (pin.transform.position - sphereCenter).normalized;
//         float pushStrength = moveSpeed * 0.5f;
//         Vector3 newPos = pin.transform.position + pushDir * pushStrength * Time.fixedDeltaTime;
//         newPos.y = pin.transform.position.y;
//         pin.transform.position = newPos;

//         //عند الاصطدام، ستنتقل دفعة من القوة إلى نقاط MassSpring
//         if (massSpring != null)
//         {
//             Vector3 impactPoint = (sphereCenter + pin.transform.position) * 0.5f;
//             Vector3 localImpact = transform.InverseTransformPoint(impactPoint);
//             float impactStrength = currentVelocity.magnitude * pointMass;

//             // طبق دفعة على MassSpring
//             // massSpring.ApplyImpulse(localImpact, -currentVelocity.normalized, impactStrength, dentRadius);
//         }

//         // if (materialType == MaterialType.Tin)
//         // {
//         //     ApplyTinDent(sphereCenter, pin.transform.position, dentRadius, dentDepth);
//         // }

//         // تشغيل صوت اصطدام الكرة بالـ pin
//         if (hitPinSound != null)
//         {
//             AudioSource.PlayClipAtPoint(hitPinSound, sphereCenter);
//         }
//     }


//     void ApplyTinDent(Vector3 collisionPoint, Vector3 pinPosition, float dentRadius, float dentDepth)
//     {
//         if (mesh == null || vertices == null || originalVertices == null) return;

//         Vector3 localCollisionPoint = transform.InverseTransformPoint((collisionPoint + pinPosition) * 0.5f);

//         for (int i = 0; i < vertices.Length; i++)
//         {
//             float dist = Vector3.Distance(vertices[i], localCollisionPoint);
//             if (dist < dentRadius)
//             {
//                 Vector3 dir = (vertices[i] - localCollisionPoint).normalized;
//                 float strength = Mathf.Lerp(dentDepth, 0, dist / dentRadius);
//                 vertices[i] -= dir * strength;
//             }
//         }

//         mesh.vertices = vertices;
//         mesh.RecalculateNormals();
//     }

//     void BreakSphere()
//     {
//         isBroken = true;
//         if (breakSound != null)
//         {
//             AudioSource.PlayClipAtPoint(breakSound, transform.position);
//         }
//         Destroy(gameObject);
//     }
// }
