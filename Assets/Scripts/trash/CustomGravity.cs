using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CustomGravity : MonoBehaviour
{
    [Header("إعدادات الجاذبية والفيزياء")]
    public float gravity = 9.81f;
    public float mass = 1f;
    [Range(0f, 1f)]
    public float restitution = 0.2f;

    // سرعة انتقالية وزاويّة
    private Vector3 velocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;

    // بيانات الـMesh
    private Vector3[] localVerts;
    private int[] triangles;
    private Vector3 localCenterOfMass;

    // مقلوب Tensor عطالي (تقريب أسطواني)
    private float inertiaInv;

    // عتبة بسيطة لتصحيح التصادم بالأرض
    private const float groundEpsilon = 0.01f;

    private void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;
        localVerts = mesh.vertices;       // إحداثيات النقاط في النظام المحلي
        triangles = mesh.triangles;

        // 1) نحصي المتوسط الهندسي لجميع الـvertices
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < localVerts.Length; i++)
            sum += localVerts[i];
        localCenterOfMass = sum / localVerts.Length;

        // 2) إذا أردت تقريب أفضل (مثلاً أسطوانة)، يمكنك استبدال الآتي:
        //    float halfHeight = mesh.bounds.extents.y;
        //    localCenterOfMass = new Vector3(0f, halfHeight, 0f);

        // 3) حساب العطالة التقريبي بناءً على أبعاد الجسم
        //    (مثال للأسطوانة الصلبة البسيط)
        float r = mesh.bounds.extents.x;   // تقريب نصف القطر
        float h = mesh.bounds.size.y;      // ارتفاع الـPin
                                           // نصيغ I عن طريق معادلات أسطوانة صلبة:
                                           // I_x = I_z = (1/12) * m * (3r^2 + h^2)
                                           // I_y = (1/2) * m * r^2
        float Ixx = (1f / 12f) * mass * (3f * r * r + h * h);
        // لتبسيط العملية، نأخذ Ixx = Izz (لأنها على المحورين الأفقيين)
        if (Ixx <= Mathf.Epsilon) Ixx = 0.0001f;
        inertiaInv = 1f / Ixx;

        // 4) تأكّد من بقاء Pin فوق الأرض في البداية:
        Vector3 p = transform.position;
        float worldCOMy = transform.TransformPoint(localCenterOfMass).y;
        // نريد أن تكون أدنى نقطة vertex عند y >= 0
        // لكن أسهل: نرفع الجسم بحيث مركز ثقله يكون فوق نصف ارتفاعه
        float halfHeight = mesh.bounds.extents.y;
        if (p.y < halfHeight)
            p.y = halfHeight;
        transform.position = p;

        // 5) نوّف السرعات قبل البدء
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        // 6) ضبط الدوران المبدئي ليتماشى محور Y الإممودي
        transform.rotation = Quaternion.identity;
    }


    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) تطبيق الجاذبية
        velocity += Vector3.down * gravity * dt;
        transform.position += velocity * dt;

        // 2) تحديث الدوران (حاصل ضرب زاويّة × dt)
        if (angularVelocity.sqrMagnitude > Mathf.Epsilon)
        {
            float angle = angularVelocity.magnitude * dt * Mathf.Rad2Deg;
            Vector3 axis = angularVelocity.normalized;
            Quaternion deltaRot = Quaternion.AngleAxis(angle, axis);
            transform.rotation = deltaRot * transform.rotation;
        }

        // 3) التحقق وتصحيح التصادم مع الأرض
        HandleGroundCollisions();
    }

    private void HandleGroundCollisions()
    {
        // اكتشاف أقل نقطة vertex تحت مستوى y=0
        float minY = float.MaxValue;
        int minVertIndex = -1;
        for (int i = 0; i < localVerts.Length; i++)
        {
            Vector3 worldV = transform.TransformPoint(localVerts[i]);
            if (worldV.y < minY)
            {
                minY = worldV.y;
                minVertIndex = i;
            }
        }

        // إن كان الاختراق أعمق من groundEpsilon
        if (minY < -groundEpsilon)
        {
            // 1) نرفع الجسم لإزالة الاختراق
            transform.position += Vector3.up * (-minY);

            // 2) نحسب نقطة التصادم ومركز الكتلة في العالم
            Vector3 worldP = transform.TransformPoint(localVerts[minVertIndex]);
            Vector3 worldCOM = transform.TransformPoint(localCenterOfMass);

            Vector3 r = worldP - worldCOM;
            Vector3 velAtP = velocity + Vector3.Cross(angularVelocity, r);
            float vRelY = velAtP.y;

            // 3) إذا ما زالت السرعة العمودية سالبة كفاية لتثبيت التصادم
            if (vRelY < -groundEpsilon)
            {
                Vector3 n = Vector3.up;
                float invM = 1f / mass;
                Vector3 rCrossN = Vector3.Cross(r, n);
                Vector3 Iinv_rCrossN = inertiaInv * rCrossN;
                Vector3 crossTerm = Vector3.Cross(Iinv_rCrossN, r);
                float inertiaTerm = Vector3.Dot(n, crossTerm);

                float j = -(1f + restitution) * vRelY / (invM + inertiaTerm);
                Vector3 impulse = j * n;

                velocity += impulse * invM;
                angularVelocity += inertiaInv * Vector3.Cross(r, impulse);

                // قصر الدوران على المحور الرأسي فقط
                angularVelocity.x = 0f;
                angularVelocity.z = 0f;
            }
        }
        // إن كان الاختراق طفيفًا بين [-ε, 0]
        else if (minY < 0f)
        {
            // نرفع الجسم قليلًا دون Impulse
            transform.position += Vector3.up * (-minY);

            // نصفر السرعة العمودية ونقفّل الدوران الجانبي
            if (velocity.y < 0f) velocity.y = 0f;
            angularVelocity.x = 0f;
            angularVelocity.z = 0f;
        }
    }
}
