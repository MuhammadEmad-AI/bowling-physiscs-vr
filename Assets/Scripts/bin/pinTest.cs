// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;

// [ExecuteInEditMode]
// [RequireComponent(typeof(Rigidbody), typeof(MeshCollider))]
// [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
// public class RealisticPinWithTopBall : MonoBehaviour
// {
//     public int segments = 32;
//     public int verticalSegments = 8;
//     public float height = 1.2f;
//     public float topRadius = 0.05f;
//     public float middleRadius = 0.3f;
//     public float bottomRadius = 0.15f;
//     public float widthFactor = 1.5f;
//     [Tooltip("عامل توسيع خاص للقاعدة")]
//     public float baseWidthFactor = 2f;
//     public float topBallRadius = 0.09f;
//     public float ballYOffset = 0.07f;

//     public enum MaterialType { Glass, Rubber, Solid }
//     public MaterialType materialType = MaterialType.Solid;
//     public PhysicsMaterial glassMaterial;
//     public PhysicsMaterial rubberMaterial;
//     public PhysicsMaterial solidMaterial;

//     private Mesh mesh;

//     void Start()
//     {
//         GeneratePin();
//         StartCoroutine(SetupPhysics());
//     }

//     IEnumerator SetupPhysics()
//     {
//         yield return null;
//         var rb = GetComponent<Rigidbody>();
//         rb.useGravity = true;
//         rb.mass = 5f;
//         rb.linearDamping = 0.5f;
//         rb.angularDamping = 0.5f;

//         var col = GetComponent<MeshCollider>();
//         col.sharedMesh = mesh;
//         col.convex = true;
//         switch (materialType)
//         {
//             case MaterialType.Glass: col.material = glassMaterial; break;
//             case MaterialType.Rubber: col.material = rubberMaterial; break;
//             case MaterialType.Solid: col.material = solidMaterial; break;
//         }
//     }

// #if UNITY_EDITOR
//     void Update() { if (!Application.isPlaying) GeneratePin(); }
// #endif

//     void GeneratePin()
//     {
//         mesh = new Mesh { name = "Procedural Bowling Pin" };
//         var verts = new List<Vector3>();
//         var tris = new List<int>();

//         // Body rings (including duplicate first vertex for closure)
//         for (int y = 0; y <= verticalSegments; y++)
//         {
//             float t = (float)y / verticalSegments;
//             float r1 = Mathf.Lerp(bottomRadius, middleRadius, Mathf.Sin(t * Mathf.PI));
//             float r2 = Mathf.Lerp(r1, topRadius, t);
//             float yPos = t * height;
//             for (int i = 0; i <= segments; i++)
//             {
//                 float angle = 2f * Mathf.PI * i / segments;
//                 float x = Mathf.Cos(angle) * r2 * widthFactor;
//                 float z = Mathf.Sin(angle) * r2;
//                 verts.Add(new Vector3(x, yPos, z));
//             }
//         }
//         int ringVerts = segments + 1;
//         // Side triangles
//         for (int y = 0; y < verticalSegments; y++)
//             for (int i = 0; i < segments; i++)
//             {
//                 int i0 = y * ringVerts + i;
//                 int i1 = i0 + 1;
//                 int i2 = i0 + ringVerts;
//                 int i3 = i2 + 1;
//                 tris.AddRange(new[] { i0, i2, i1 });
//                 tris.AddRange(new[] { i1, i2, i3 });
//             }

//         // Bottom cap
//         int bottomCenter = verts.Count;
//         verts.Add(new Vector3(0, 0, 0));
//         for (int i = 0; i < segments; i++)
//         {
//             tris.Add(bottomCenter);
//             tris.Add(i + 1);
//             tris.Add(i);
//         }

//         // Top ball
//         int baseIdx = verts.Count;
//         for (int lat = 0; lat <= verticalSegments; lat++)
//         {
//             float theta = Mathf.PI * lat / verticalSegments;
//             float sinT = Mathf.Sin(theta);
//             float cosT = Mathf.Cos(theta);
//             for (int lon = 0; lon <= segments; lon++)
//             {
//                 float phi = 2f * Mathf.PI * lon / segments;
//                 float x = sinT * Mathf.Cos(phi) * widthFactor;
//                 float z = sinT * Mathf.Sin(phi);
//                 float y = cosT;
//                 verts.Add(new Vector3(x, y, z) * topBallRadius + new Vector3(0, height + ballYOffset, 0));
//             }
//         }
//         int ballRing = segments + 1;
//         for (int lat = 0; lat < verticalSegments; lat++)
//             for (int lon = 0; lon < segments; lon++)
//             {
//                 int i0 = baseIdx + lat * ballRing + lon;
//                 int i1 = i0 + 1;
//                 int i2 = i0 + ballRing;
//                 int i3 = i2 + 1;
//                 tris.AddRange(new[] { i0, i2, i1 });
//                 tris.AddRange(new[] { i1, i2, i3 });
//             }

//         // Top cap (sphere pole)
//         int topCenter = verts.Count;
//         verts.Add(new Vector3(0, height + ballYOffset + topBallRadius, 0));
//         int offset = baseIdx + verticalSegments * ballRing;
//         for (int i = 0; i < segments; i++)
//         {
//             tris.Add(topCenter);
//             tris.Add(offset + i);
//             tris.Add(offset + i + 1);
//         }

//         mesh.SetVertices(verts);
//         mesh.SetTriangles(tris, 0);
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//         GetComponent<MeshFilter>().sharedMesh = mesh;
//     }
// }