using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Управление спасательным кругом на воде: W/S — вперёд/назад, A/D — поворот.
/// Держит круг на воде через несколько точек плавучести, опускает центр масс
/// и возвращает круг в вертикальное положение, чтобы он не переворачивался.
/// Работает с OceanSystem из OceanPro (если он есть в сцене).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LifebuoyController : MonoBehaviour
{
    [Header("Движение (WASD)")]
    [Tooltip("Сила толкания вперёд/назад.")]
    [SerializeField] private float moveForce = 25f;
    [Tooltip("Скорость поворота A/D.")]
    [SerializeField] private float turnTorque = 6f;
    [Tooltip("Максимальная скорость, которую поддерживает ввод.")]
    [SerializeField] private float maxSpeed = 6f;

    [Header("Остойчивость")]
    [Tooltip("Насколько сильно круг стремится вернуться в вертикальное положение.")]
    [SerializeField] private float uprightStrength = 40f;
    [Tooltip("Гашение качания (вращения) на воде.")]
    [SerializeField] private float angularDamping = 6f;
    [Tooltip("Опускание центра масс относительно центра круга (в метрах). Чем больше, тем устойчивее.")]
    [SerializeField] private float centerOfMassDrop = 0.35f;

    [Header("Плавучесть")]
    [Tooltip("Радиус круга, по которому расставляются точки плавучести.")]
    [SerializeField] private float ringRadius = 0.5f;
    [Tooltip("Количество точек плавучести по кругу.")]
    [Range(4, 16)] [SerializeField] private int floatPoints = 8;
    [Tooltip("Глубина погружения точки, при которой выталкивание максимальное.")]
    [SerializeField] private float submersionDepth = 0.3f;
    [Tooltip("Сопротивление воды при погружении.")]
    [SerializeField] private float waterDrag = 2.5f;

    [Header("След")]
    [SerializeField] private bool leaveWake = true;
    [SerializeField] private float wakeRadius = 1.2f;
    [SerializeField] private float wakeStrength = 1f;

    private Rigidbody rb;
    private OceanSystem ocean;
    private Vector3[] pointsLocal;
    private float[] pointSubmersion;

    [Header("Модель")]
    [Tooltip("Автоматически центрирует видимую модель круга относительно pivot (физического центра).")]
    [SerializeField] private bool centerModelOnPivot = true;

    /// <summary>
    /// Сдвигает все дочерние модели так, чтобы центр их общих границ совпал с pivot объекта.
    /// Безопасно вызывать несколько раз: после первого сдвига смещение становится нулевым.
    /// Вызывается автоматически при запуске и из меню Inspector (⋮ → Center Model Now).
    /// </summary>
    [ContextMenu("Center Model Now")]
    public void CenterModel()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 delta = transform.position - bounds.center;
        if (delta.sqrMagnitude < 1e-8f) return;

        foreach (Transform child in transform)
            child.position += delta;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Awake()
    {
        if (centerModelOnPivot) CenterModel();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = Mathf.Max(rb.mass, 1f);
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.automaticCenterOfMass = false;
        rb.centerOfMass = new Vector3(0f, -centerOfMassDrop, 0f);

        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = ringRadius;
        }

        ocean = FindFirstObjectByType<OceanSystem>();

        pointsLocal = new Vector3[floatPoints];
        pointSubmersion = new float[floatPoints];
        for (int i = 0; i < floatPoints; i++)
        {
            float a = i * Mathf.PI * 2f / floatPoints;
            pointsLocal[i] = new Vector3(Mathf.Cos(a) * ringRadius, 0f, Mathf.Sin(a) * ringRadius);
        }
    }

    private void OnDisable()
    {
        if (ocean != null && leaveWake) ocean.RemoveWakeSource(transform);
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyUpright();
        ApplyControls();
    }

    private void Update()
    {
        if (leaveWake && ocean != null)
            ocean.SetWakeSource(transform, rb.linearVelocity, wakeRadius, wakeStrength);
    }

    private void ApplyBuoyancy()
    {
        float gravity = Physics.gravity.magnitude;
        float perPoint = rb.mass * gravity / floatPoints * 1.6f;
        float totalSubmersion = 0f;

        for (int i = 0; i < floatPoints; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(pointsLocal[i]);
            float waterY = WaterHeightAt(worldPoint, out Vector3 normal);
            float depth = waterY - worldPoint.y;
            float submersion = Mathf.Clamp01(depth / Mathf.Max(0.01f, submersionDepth));
            pointSubmersion[i] = submersion;
            totalSubmersion += submersion;

            if (submersion <= 0f) continue;

            Vector3 lift = Vector3.up * perPoint * submersion;
            rb.AddForceAtPosition(lift, worldPoint, ForceMode.Force);

            // Сопротивление воды: гасим скорость точки, пропорционально погружению.
            Vector3 pointVelocity = rb.GetPointVelocity(worldPoint);
            rb.AddForceAtPosition(-pointVelocity * waterDrag * submersion * rb.mass / floatPoints, worldPoint, ForceMode.Force);
        }

        float submergedFraction = totalSubmersion / floatPoints;
        rb.linearDamping = Mathf.Lerp(0.3f, 2.0f, submergedFraction);
        rb.angularDamping = Mathf.Lerp(0.5f, angularDamping, submergedFraction);
    }

    private void ApplyUpright()
    {
        // Возвращающий момент: поворачиваем "верх" круга к мировому вверху.
        Vector3 axis = Vector3.Cross(transform.up, Vector3.up);
        float angle = Vector3.Angle(transform.up, Vector3.up) * Mathf.Deg2Rad;
        Vector3 correction = axis.normalized * (angle * uprightStrength);
        if (float.IsNaN(correction.x)) correction = Vector3.zero;

        // Упругость: тянем к вертикали, гасим вращение.
        rb.AddTorque(correction - rb.angularVelocity * angularDamping * 0.5f, ForceMode.Acceleration);
    }

    private void ApplyControls()
    {
        float throttle = 0f;
        float steer = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed) throttle -= 1f;
            if (kb.dKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed) steer -= 1f;
        }
#else
        if (Input.GetKey(KeyCode.W)) throttle += 1f;
        if (Input.GetKey(KeyCode.S)) throttle -= 1f;
        if (Input.GetKey(KeyCode.D)) steer += 1f;
        if (Input.GetKey(KeyCode.A)) steer -= 1f;
#endif

        float submerged = 0f;
        for (int i = 0; i < pointSubmersion.Length; i++) submerged += pointSubmersion[i];
        submerged /= pointSubmersion.Length;

        // Двигаем только когда круг хотя бы частично в воде.
        if (submerged > 0.05f && throttle != 0f)
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 planarVel = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            if (planarVel.magnitude < maxSpeed || Vector3.Dot(planarVel, forward * throttle) < 0f)
                rb.AddForce(forward * throttle * moveForce * submerged, ForceMode.Force);
        }

        if (submerged > 0.05f && steer != 0f)
            rb.AddTorque(Vector3.up * steer * turnTorque * submerged, ForceMode.Acceleration);
    }

    private float WaterHeightAt(Vector3 worldPoint, out Vector3 normal)
    {
        normal = Vector3.up;
        if (ocean == null) return 0f;
        return ocean.GetWaterHeight(worldPoint, out normal, out _);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 com = transform.TransformPoint(new Vector3(0f, -centerOfMassDrop, 0f));
        Gizmos.DrawWireSphere(com, 0.05f);
        Gizmos.color = Color.cyan;
        // Круг плавучести учитывает масштаб объекта, поэтому рисуем с ним же.
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireSphere(Vector3.zero, ringRadius * transform.lossyScale.x);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
