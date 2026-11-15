using UnityEngine;

/// <summary>
/// Создаёт много левиафанов из префаба и расселяет их по океану.
/// Поставь на пустой объект в сцене, перетащи префаб левиафана (crimson) в поле Prefab.
/// </summary>
public class LeviathanSpawner : MonoBehaviour
{
    [Header("Что спаунить")]
    [Tooltip("Префаб левиафана (например, префаб crimson).")]
    public GameObject leviathanPrefab;
    [Tooltip("Сколько левиафанов создать.")]
    [Range(1, 100)] public int count = 15;
    [Tooltip("Сид для повторяемого размещения (0 — случайный).")]
    public int randomSeed = 0;

    [Header("Зона")]
    [Tooltip("Радиус зоны плавания вокруг этого объекта.")]
    public float roamRadius = 400f;
    [Tooltip("Океан в сцене (если пусто — ищется автоматически).")]
    public OceanSystem ocean;

    [Header("Размер и скорость")]
    [Tooltip("Множитель размера (поверх масштаба префаба). 1 — как в префабе.")]
    public Vector2 scaleRange = new Vector2(0.9f, 1.2f);
    public float minSpeed = 1.5f;
    public float maxSpeed = 4f;
    [Tooltip("Дополнительная глубина под верхом тела, м. Тело уходит под воду автоматически по размеру модели.")]
    public Vector2 depthRange = new Vector2(1f, 8f);

    [Header("Поворот модели")]
    [Tooltip("Попробуй 0, 90, -90 или 180, если левиафаны плывут боком.")]
    public float modelYawOffset = 0f;

    private void Start()
    {
        if (leviathanPrefab == null)
        {
            Debug.LogWarning("LeviathanSpawner: не назначен префаб левиафана.", this);
            return;
        }
        if (ocean == null) ocean = FindFirstObjectByType<OceanSystem>();
        if (randomSeed != 0) Random.InitState(randomSeed);

        Vector3 center = transform.position;
        for (int i = 0; i < count; i++)
        {
            Vector2 p = Random.insideUnitCircle * roamRadius;
            Vector3 pos = center + new Vector3(p.x, 0f, p.y);
            float yaw = Random.Range(0f, 360f);

            GameObject fish = Instantiate(leviathanPrefab, pos, Quaternion.Euler(0f, yaw, 0f), transform);
            fish.name = $"Leviathan_{i:00}";
            fish.transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);

            LeviathanSwimmer swimmer = fish.GetComponent<LeviathanSwimmer>();
            if (swimmer == null) swimmer = fish.AddComponent<LeviathanSwimmer>();
            swimmer.depthRange = depthRange;
            swimmer.modelYawOffset = modelYawOffset;
            swimmer.Init(ocean, center, roamRadius, minSpeed, maxSpeed, Random.Range(1f, 1000f));
        }
    }
}
