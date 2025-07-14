// using UnityEngine;

// /// <summary>
// /// نظام جاذبية مخصص بدون Rigidbody.
// /// يأخذ نصف قطر الجسم بعين الاعتبار لمنع غوصه تحت الأرض.
// /// </summary>
// [ExecuteInEditMode]
// public class CustomGravity : MonoBehaviour
// {
//     [Tooltip("تفعيل الجاذبية في وقت التشغيل")]
//     public bool enableGravity = true;

//     [Tooltip("قيمة تسارع الجاذبية (افتراضي 9.81)")]
//     public float gravityStrength = 9.81f;

//     [Tooltip("اتجاه الجاذبية (افتراضي للأسفل)")]
//     public Vector3 gravityDirection = Vector3.down;

//     [Tooltip("هل يتم التوقف عند الأرض؟")]
//     public bool stopAtGround = true;

//     [Tooltip("أدنى ارتفاع يمكن الوصول إليه (يمثل الأرض)")]
//     public float groundY = 0f;

//     [Tooltip("نصف قطر الجسم (لمنع غوصه في الأرض)")]
//     public float objectRadius = 0.5f;

//     private Vector3 velocity = Vector3.zero;

//     void Update()
//     {
// #if UNITY_EDITOR
//         if (!Application.isPlaying) return;
// #endif

//         if (!enableGravity) return;

//         Vector3 acceleration = gravityDirection.normalized * gravityStrength;
//         velocity += acceleration * Time.deltaTime;

//         Vector3 newPosition = transform.position + velocity * Time.deltaTime;

//         if (stopAtGround && newPosition.y <= groundY + objectRadius)
//         {
//             newPosition.y = groundY + objectRadius;
//             velocity = Vector3.zero;
//         }

//         transform.position = newPosition;
//     }

//     public void ResetVelocity()
//     {
//         velocity = Vector3.zero;
//     }
// }
