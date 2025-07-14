// using UnityEngine;

// [ExecuteInEditMode]
// [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
// public class ProceduralBowlingPin : MonoBehaviour
// {
//     public enum PhysicsType { Solid, Rubber, Glass }

//     [Header("Pin Profile")]
//     public int radialSegments = 24;
//     public int heightSegments = 32;
//     public float pinHeight = 1.2f;
//     public float maxRadius = 0.3f;
//     public float bottomRadius = 0.15f;
//     public float neckRadius = 0.05f;

//     [Header("Rendering Materials")]
//     public PhysicsType physicsType = PhysicsType.Solid;
//     public Material solidVisual;
//     public Material rubberVisual;
//     public Material glassVisual;

//     [Header("Glass Break Effects")]
//     public AudioClip glassBreakClip;
//     public float breakSoundThreshold = 5f;
//     public GameObject glassShatterPrefab;

//     [HideInInspector]
//     public Mesh pinMesh;

//     private BowlingPinMeshBuilder meshBuilder;
//     private BowlingPinMaterialHandler materialHandler;

//     void Start()
//     {
//         meshBuilder = new BowlingPinMeshBuilder(this);
//         materialHandler = new BowlingPinMaterialHandler(this);

//         meshBuilder.BuildMesh();
//         materialHandler.SetupVisual();
//     }

//     void Update()
//     {
//         if (!Application.isPlaying)
//         {
//             meshBuilder.BuildMesh();
//             materialHandler.SetupVisual();
//         }
//     }

//     void OnValidate()
//     {
//         if (!Application.isPlaying)
//         {
//             meshBuilder.BuildMesh();
//             materialHandler.SetupVisual();
//         }
//     }
// }
