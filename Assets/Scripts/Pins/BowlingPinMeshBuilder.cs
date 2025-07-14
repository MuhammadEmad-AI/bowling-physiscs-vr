// using UnityEngine;
// using System.Collections.Generic;

// public class BowlingPinMeshBuilder
// {
//     private readonly ProceduralBowlingPin pin;

//     public BowlingPinMeshBuilder(ProceduralBowlingPin pin)
//     {
//         this.pin = pin;
//     }

//     public void BuildMesh()
//     {
//         Mesh mesh = new Mesh { name = "ProceduralBowlingPin" };
//         List<Vector3> verts = new List<Vector3>();
//         List<int> tris = new List<int>();
//         List<Vector2> uvs = new List<Vector2>();

//         Vector2[] profile = new Vector2[pin.heightSegments + 1];
//         for (int i = 0; i <= pin.heightSegments; i++)
//         {
//             float t = (float)i / pin.heightSegments;
//             float y = t * pin.pinHeight;
//             float r = Mathf.Lerp(pin.bottomRadius, pin.maxRadius, Mathf.Sin(t * Mathf.PI));
//             r = Mathf.Lerp(r, pin.neckRadius, t * t);
//             profile[i] = new Vector2(y, r);
//         }

//         int rings = pin.radialSegments + 1;
//         for (int i = 0; i <= pin.heightSegments; i++)
//         {
//             Vector2 p = profile[i];
//             for (int j = 0; j < rings; j++)
//             {
//                 float theta = (float)j / pin.radialSegments * Mathf.PI * 2f;
//                 verts.Add(new Vector3(Mathf.Cos(theta) * p.y, p.x, Mathf.Sin(theta) * p.y));
//                 uvs.Add(new Vector2((float)j / pin.radialSegments, p.x / pin.pinHeight));
//             }
//         }

//         for (int i = 0; i < pin.heightSegments; i++)
//         {
//             int row = i * rings;
//             int next = (i + 1) * rings;
//             for (int j = 0; j < pin.radialSegments; j++)
//             {
//                 int a = row + j;
//                 int b = next + j;
//                 int c = a + 1;
//                 int d = b + 1;
//                 tris.Add(a); tris.Add(b); tris.Add(c);
//                 tris.Add(c); tris.Add(b); tris.Add(d);
//             }
//         }

//         int bottomCenter = verts.Count;
//         verts.Add(Vector3.zero);
//         uvs.Add(new Vector2(0.5f, 0f));
//         for (int j = 0; j < pin.radialSegments; j++)
//         {
//             tris.Add(bottomCenter);
//             tris.Add(j + 1);
//             tris.Add(j);
//         }

//         int topCenter = verts.Count;
//         verts.Add(new Vector3(0, pin.pinHeight, 0));
//         uvs.Add(new Vector2(0.5f, 1f));
//         int baseRing = pin.heightSegments * rings;
//         for (int j = 0; j < pin.radialSegments; j++)
//         {
//             tris.Add(topCenter);
//             tris.Add(baseRing + j);
//             tris.Add(baseRing + j + 1);
//         }

//         mesh.SetVertices(verts);
//         mesh.SetTriangles(tris, 0);
//         mesh.SetUVs(0, uvs);
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();

//         pin.pinMesh = mesh;
//         pin.GetComponent<MeshFilter>().sharedMesh = mesh;
//     }
// }
