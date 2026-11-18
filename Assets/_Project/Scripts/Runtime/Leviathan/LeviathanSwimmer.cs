using UnityEngine;

/// <summary>
/// Плавание одного левиафана: медленно кружит по воде, плавно меняет глубину,
/// покачивается на волнах и изгибается при повороте. Двигается без физики (kinematic).
/// Обычно добавляется спаунером LeviathanSpawner, но можно повесить и вручную.
/// </summary>
public class LeviathanSwimmer : MonoBehaviour
{
    [Header("Движение")]
    [Tooltip("Скорость плавания, метров в секунду.")]
    public float speed = 3f;
    [Tooltip("Насколько быстро меняется направление (больше = резче повороты).")]
    public float turnResponsiveness = 0.35f;
    [Tooltip("Центр зоны, в которой левиафан плавает.")]
    public Vector3 roamCenter = Vector3.zero;
    [Tooltip("Радиус зоны, при выходе за который левиафан поворачивает обратно.")]
    public float roamRadius = 400f;

    [Header("Глубина")]
    [Tooltip("Дополнительная глубина ПОД верхом тела, м: минимум и максимум. Сама модель уходит под воду автоматически.")]
    public Vector2 depthRange = new Vector2(1f, 8f);
    [Tooltip("Запас между верхом тела и поверхностью воды, м.")]
    public float clearanceBelowSurface = 2f;
    [Tooltip("Насколько быстро меняется глубина.")]
    public float depthSpeed = 0.15f;

    [Header("Поза тела")]
    [Tooltip("Поворот модели по Y, если модель смотрит не вдоль +Z. Попробуй 0, 90, -90 или 180.")]
    public float modelYawOffset = 0f;
    [Tooltip("Амплитуда покачивания из стороны в сторону (градусы).")]
    public float swayAmount = 6f;
    [Tooltip("Наклон при повороте (градусы).")]
    public float bankAmount = 18f;
    [Tooltip("Частота виляния, циклов в секунду.")]
    public float swayFrequency = 0.6f;

    private OceanSystem ocean;
    private float heading;
    private float turnRate;
    private float currentDepth;
    private float minDepth;
    private float maxDepth;
    private float targetDepth;
    private float seed;
    private float yawSmooth;
    private float bank;
    private Vector3 lastPosition;

    /// <summary>Вызывается спаунером после создания.</summary>
    public void Init(OceanSystem oceanSystem, Vector3 center, float radius, float minSpeed, float maxSpeed, float randomSeed)
    {
        ocean = oceanSystem;
        roamCenter = center;
        roamRadius = radius;
        speed = Random.Range(minSpeed, maxSpeed);
        seed = randomSeed;
        heading = Random.Range(0f, 360f);
        // Глубина считается от верха модели, чтобы крупный левиафан полностью ушёл под воду.
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float topAbovePivot = 0f;
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            topAbovePivot = Mathf.Max(0f, b.max.y - transform.position.y);
        }
        minDepth = topAbovePivot + clearanceBelowSurface;
        maxDepth = minDepth + Mathf.Max(0f, depthRange.y - depthRange.x);
        currentDepth = Random.Range(minDepth, maxDepth);
        targetDepth = currentDepth;
        lastPosition = transform.position;

        // Модель с физикой не нужна — отключаем, если она есть в префабе.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void Start()
    {
        if (ocean == null) ocean = FindFirstObjectByType<OceanSystem>();
        if (seed == 0f) seed = Random.Range(0f, 1000f);
        lastPosition = transform.position;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        float t = Time.time + seed;

        // 1. Направление: плавный шум + возврат в зону.
        float wander = (Mathf.PerlinNoise(t * 0.08f, seed) - 0.5f) * 2f;
        turnRate = Mathf.Lerp(turnRate, wander * 40f, turnResponsiveness * dt);
        heading += turnRate * dt;

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatCenter = new Vector3(roamCenter.x, 0f, roamCenter.z);
        Vector3 toCenter = flatCenter - flatPos;
        if (toCenter.magnitude > roamRadius * 0.8f)
        {
            float wantHeading = Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg;
            heading = Mathf.LerpAngle(heading, wantHeading, 0.8f * dt);
        }

        Vector3 dir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        Vector3 newFlat = flatPos + dir * speed * dt;

        // 2. Глубина: меняем цель медленно, текущая глубина плавно догоняет.
        if (Mathf.PerlinNoise(t * 0.05f, seed + 17f) > 0.5f || Mathf.Abs(currentDepth - targetDepth) < 0.1f)
            targetDepth = Mathf.Lerp(minDepth, maxDepth, Mathf.PerlinNoise(t * 0.03f, seed + 3f));
        currentDepth = Mathf.Lerp(currentDepth, targetDepth, depthSpeed * dt);

        // 3. Высота: под волной + глубина.
        float surfaceY = 0f;
        if (ocean != null)
        {
            surfaceY = ocean.GetWaterHeight(newFlat, out _, out _);
        }
        float wantY = surfaceY - currentDepth;
        Vector3 newPos = new Vector3(newFlat.x, Mathf.Lerp(transform.position.y, wantY, 4f * dt), newFlat.z);

        // 4. Ориентация: по направлению, с покачиванием и креном.
        Vector3 move = newPos - transform.position;
        float yawTarget = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        yawSmooth = Mathf.LerpAngle(yawSmooth, yawTarget, 6f * dt);

        float sway = Mathf.Sin(Time.time * Mathf.PI * 2f * swayFrequency + seed) * swayAmount;
        bank = Mathf.Lerp(bank, -turnRate / 40f * bankAmount, 3f * dt);
        float pitch = Mathf.Clamp(-move.y / Mathf.Max(dt, 0.001f) * 4f, -12f, 12f);

        transform.position = newPos;
        transform.rotation = Quaternion.Euler(pitch, yawSmooth + modelYawOffset + sway, bank);
        lastPosition = newPos;
    }
}
