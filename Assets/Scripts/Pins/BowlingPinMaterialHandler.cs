// using UnityEngine;

// public class BowlingPinMaterialHandler
// {
//     private readonly ProceduralBowlingPin pin;

//     public BowlingPinMaterialHandler(ProceduralBowlingPin pin)
//     {
//         this.pin = pin;
//     }

//     public void SetupVisual()
//     {
//         var rend = pin.GetComponent<MeshRenderer>();
//         switch (pin.physicsType)
//         {
//             case ProceduralBowlingPin.PhysicsType.Rubber:
//                 rend.sharedMaterial = pin.rubberVisual;
//                 break;
//             case ProceduralBowlingPin.PhysicsType.Glass:
//                 rend.sharedMaterial = pin.glassVisual;
//                 break;
//             default:
//                 rend.sharedMaterial = pin.solidVisual;
//                 break;
//         }
//     }
// }
