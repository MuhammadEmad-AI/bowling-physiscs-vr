// using UnityEngine;
// using System.Collections.Generic;

// [ExecuteInEditMode]
// // لم نعد نستخدم SphereCollider أو Rigidbody
// public class ProceduralSphere : MonoBehaviour
// {
//     public enum MaterialType { Solid, Rubber, Glass }
//     [Tooltip("اختر نوع المادة الفيزيائية للكرة")]
//     public MaterialType materialType = MaterialType.Solid;

//     public Material solidVisualMaterial;
//     public Material rubberVisualMaterial;
//     public Material glassVisualMaterial;

//     [Header("Sphere Geometry")]
//     public int longitudeSegments = 24;
//     public int latitudeSegments = 16;
//     [Tooltip("عدد مرات تقسيم كل مثلث لزيادة الدقة")]
//     public int subdivisionLevels = 0;
//     public float radius = 1f; // نصف قطر الكرة

//     [Header("Glass Break Settings")]
//     [Tooltip("قوة الاصطدام اللازمة لتحطيم الكرة الزجاجية")]
//     public float breakForceThreshold = 10f;
//     [Tooltip("الصوت الذي يُشغل عند تحطيم الكرة")]
//     public AudioClip breakSound;

//     [Header("Movement Settings")]
//     [Tooltip("السرعة الثابتة لحركة الكرة عند الضغط على M")]
//     public float moveSpeed = 5f;

//     private Mesh mesh;
//     private Vector3[] vertices;
//     private bool isBroken = false;
//     private bool isMoving = false;

//     void Start()
//     {
//         ApplyMaterials();
//         GenerateSphere();
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

//     void FixedUpdate()
//     {
//         if (isMoving && !isBroken)
//         {
//             // حركة الكرة يدويًا بدون Rigidbody
//             transform.position += Vector3.left * moveSpeed * Time.fixedDeltaTime;

//             // بعد الحركة، نقوم بفحص التصادم يدوياً مع جميع الـPins
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
//         // نجمع كل الـPins في المشهد
//         ProceduralBowlingPin[] pins = FindObjectsOfType<ProceduralBowlingPin>();

//         Vector3 sphereCenter = transform.position;
//         float sphereRadius = radius;
//         float radiusSqr = sphereRadius * sphereRadius;

//         foreach (var pin in pins)
//         {
//             // --- Broad Phase: Bounding Sphere Check ---
//             // نحسب نصف ارتفاع الـPin من خاصية pinHeight في ProceduralBowlingPin
//             float pinHalfHeight = pin.pinHeight * 0.5f;
//             float pinMaxRadius = pin.maxRadius;

//             // مركز الـPin التقريبي: 
//             Vector3 pinWorldCenter = pin.transform.position + Vector3.up * pinHalfHeight;
//             float pinBoundingRadius = Mathf.Sqrt(pinHalfHeight * pinHalfHeight + pinMaxRadius * pinMaxRadius);

//             float distCenters = Vector3.Distance(sphereCenter, pinWorldCenter);
//             if (distCenters > (sphereRadius + pinBoundingRadius))
//             {
//                 // غير مرشح للتصادم؛ نتخطّاه
//                 continue;
//             }

//             // --- Narrow Phase: Sphere vs Mesh Triangles ---
//             if (IsSphereIntersectingMesh(pin, sphereCenter, sphereRadius, radiusSqr))
//             {
//                 // تصادم دقيق
//                 HandleCollision(pin, sphereCenter);
//             }
//         }
//     }

//     bool IsSphereIntersectingMesh(ProceduralBowlingPin pin, Vector3 sphereCenter, float sphereRadius, float radiusSqr)
//     {
//         Mesh meshPin = pin.pinMesh; // Mesh الخاص بالـPin
//         Vector3[] verts = meshPin.vertices;
//         int[] tris = meshPin.triangles;

//         // مصفوفة تحويل من المحلي إلى العالَم للـPin
//         Matrix4x4 localToWorld = pin.transform.localToWorldMatrix;

//         // لكل مثلث في شبكة الـPin
//         for (int i = 0; i < tris.Length; i += 3)
//         {
//             Vector3 p0 = localToWorld.MultiplyPoint3x4(verts[tris[i]]);
//             Vector3 p1 = localToWorld.MultiplyPoint3x4(verts[tris[i + 1]]);
//             Vector3 p2 = localToWorld.MultiplyPoint3x4(verts[tris[i + 2]]);

//             // ** Collision Detection (Narrow Phase): Sphere vs Mesh Triangle **
//             Vector3 closestPt = ClosestPointOnTriangle(sphereCenter, p0, p1, p2);
//             float distSqr = (sphereCenter - closestPt).sqrMagnitude;
//             if (distSqr <= radiusSqr)
//             {
//                 return true;
//             }
//         }
//         return false;
//     }

//     // خوارزمية Ericson لحساب أقرب نقطة من نقطة (point) إلى مثلث (a,b,c)
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
//         if (vc <= 0f && d1 >= 0f && d3 <= 0f)
//         {
//             float v = d1 / (d1 - d3);
//             return a + v * ab;
//         }

//         Vector3 cp = point - c;
//         float d5 = Vector3.Dot(ab, cp);
//         float d6 = Vector3.Dot(ac, cp);
//         if (d6 >= 0f && d5 <= d6) return c;

//         float vb = d5 * d2 - d1 * d6;
//         if (vb <= 0f && d2 >= 0f && d6 <= 0f)
//         {
//             float w = d2 / (d2 - d6);
//             return a + w * ac;
//         }

//         float va = d3 * d6 - d5 * d4;
//         if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
//         {
//             Vector3 bc = c - b;
//             float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
//             return b + w * bc;
//         }

//         Vector3 normal = Vector3.Cross(ab, ac);
//         float distance = Vector3.Dot(point - a, normal) / normal.magnitude;
//         return point - normal.normalized * distance;
//     }

//     void HandleCollision(ProceduralBowlingPin pin, Vector3 sphereCenter)
//     {
//         if (isBroken) return;

//         if (materialType == MaterialType.Glass)
//         {
//             float impactForce = moveSpeed;
//             if (impactForce > breakForceThreshold)
//             {
//                 BreakSphere();
//                 return;
//             }
//         }

//         Vector3 pushDir = (pin.transform.position - sphereCenter).normalized;
//         float pushStrength = moveSpeed * 0.5f;

//         Vector3 newPos = pin.transform.position + pushDir * pushStrength * Time.fixedDeltaTime;

//         // ثبّت محور Y على القيمة الأصلية (مركز الـ Pin)
//         newPos.y = pin.transform.position.y;

//         pin.transform.position = newPos;
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
