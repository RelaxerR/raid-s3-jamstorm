using UnityEngine;

/// <summary>
/// РЫБА v6 - ИСПРАВЛЕНО ДЁРГАНИЕ ПРИ ПАДЕНИИ.
/// Причины дёргания в v4/v5:
/// 1) FixedUpdate каждый шаг переписывал angularVelocity синусоидой по Time.time - физика дёргалась.
/// 2) Два коллайдера на одной рыбе (Box + Mesh) - коллизии сами с собой.
/// 3) Настройки Rigidbody ставились поздно (в Start) - первые кадры без интерполяции.
/// Фикс: настройки в Awake, вращение задаётся ОДИН раз, вихляние - мягким AddTorque по fixedTime,
/// остаётся только ОДИН коллайдер (Box в приоритете).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Fish : MonoBehaviour
{
    [Header("Тип рыбы (для лавки)")]
    [SerializeField] private string fishTypeName = "";

    [Header("Падение (физика)")]
    [Tooltip("Начальная скорость вниз (м/с)")]
    [SerializeField] private float fallSpeed = 3f;

    [Tooltip("Сила вращения при падении. Меньше = плавнее. 2-3 - плавно, 5+ - кувырок")]
    [SerializeField] private float spinSpeed = 2.5f;

    [Tooltip("Вихляние в воздухе. Выкл = самое плавное падение")]
    [SerializeField] private bool enableWiggle = false;
    [SerializeField] private float wiggleAmount = 0.3f;

    [SerializeField] private float spread = 0.6f;
    [SerializeField] private float killY = -10f;

    public float FallSpeed { get => fallSpeed; set => fallSpeed = value; }

    public string FishTypeName
    {
        get
        {
            if (!string.IsNullOrEmpty(fishTypeName)) return fishTypeName;
            return name.Replace("(Clone)", "").Trim();
        }
    }

    private Rigidbody rb;
    private bool landed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // --- Настройки Rigidbody СРАЗУ в Awake (не в Start!) - иначе первые кадры дёргаются ---
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // плавное движение между физ.шагами
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // не пролетает сквозь
        rb.sleepThreshold = 0f; // не засыпает в воздухе (засыпание = рывок при пробуждении)
        rb.maxAngularVelocity = 7f; // не даёт крутиться как бешеная

        // --- Только ОДИН коллайдер! Box в приоритете (стабильнее Mesh) ---
        BoxCollider box = GetComponent<BoxCollider>();
        MeshCollider[] meshes = GetComponentsInChildren<MeshCollider>();

        if (box != null && meshes.Length > 0)
        {
            // Есть и Box, и Mesh - отключаем Mesh, иначе двойная коллизия дёргает
            foreach (MeshCollider mc in meshes)
            {
                mc.enabled = false;
                Debug.LogWarning($"[Fish] На {gameObject.name} было 2 коллайдера (Box + Mesh) - отключил Mesh. Оставь только Box для плавности.");
            }
        }
        else
        {
            // Только Mesh - делаем Convex, иначе с Rigidbody не сталкивается
            foreach (MeshCollider mc in meshes)
            {
                if (!mc.convex)
                {
                    mc.convex = true;
                    Debug.LogWarning($"[Fish] MeshCollider на {mc.gameObject.name} был Concave - сделал Convex.");
                }
            }
        }
    }

    private void Start()
    {
        if (rb == null || landed) return;

        // Случайный размер +-10%
        float s = Random.Range(0.9f, 1.15f);
        transform.localScale *= s;

        // Начальный толчок ОДИН раз - дальше гравитация плавно разгоняет
        rb.linearVelocity = new Vector3(
            Random.Range(-spread, spread),
            -fallSpeed,
            Random.Range(-spread, spread));

        // Вращение ОДИН раз - НЕ переписываем каждый FixedUpdate!
        rb.angularVelocity = new Vector3(
            Random.Range(-spinSpeed, spinSpeed),
            Random.Range(-spinSpeed, spinSpeed),
            Random.Range(-spinSpeed, spinSpeed));
    }

    private void FixedUpdate()
    {
        if (landed || rb == null || !enableWiggle) return;
        // Мягкое вихляние через AddTorque по fixedTime (плавно, без рывков)
        Vector3 torque = new Vector3(
            Mathf.Sin(Time.fixedTime * 6f) * wiggleAmount,
            0f,
            Mathf.Cos(Time.fixedTime * 5f) * wiggleAmount);
        rb.AddTorque(torque, ForceMode.Force);
    }

    private void Update()
    {
        if (landed) return;
        if (transform.position.y < killY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (landed) return;
        CargoHold hold = other.GetComponent<CargoHold>();
        if (hold != null)
        {
            bool ok = hold.AddFish(gameObject);
            if (ok)
            {
                landed = true;
                enabled = false;
            }
        }
    }
}
