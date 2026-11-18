using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1) ГЕНЕРАТОР СПАВНА РЫБ v4 - разброс + залп (burst).
/// Вешается на "спавнер рыб". F = 1 рыба, G = залп 3 рыбы.
/// </summary>
public class FishSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<GameObject> fishPrefabs = new List<GameObject>();

    [Header("Настройки")]
    [SerializeField] private float fallSpeed = 3f;
    [SerializeField] private float spawnSpread = 0.6f;
    [SerializeField] private float spawnInterval = 0f;
    [SerializeField] private KeyCode spawnKey = KeyCode.F;
    [SerializeField] private KeyCode burstKey = KeyCode.G;
    [SerializeField] private int burstCount = 3;

    private float timer;

    private void Awake()
    {
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(spawnKey)) SpawnFish();
        if (Input.GetKeyDown(burstKey)) StartCoroutine(BurstRoutine());

        if (spawnInterval > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnFish();
            }
        }
    }

    private IEnumerator BurstRoutine()
    {
        for (int i = 0; i < burstCount; i++)
        {
            SpawnFish();
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void SpawnFish()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0)
        {
            Debug.LogWarning("[FishSpawner] Список префабов пуст!");
            return;
        }

        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        if (prefab == null) return;

        Vector3 pos = spawnPoint.position + new Vector3(
            Random.Range(-spawnSpread, spawnSpread),
            Random.Range(-0.2f, 0.2f),
            Random.Range(-spawnSpread, spawnSpread));

        Quaternion rot = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f));

        GameObject fish = Instantiate(prefab, pos, rot);

        Fish fishScript = fish.GetComponent<Fish>();
        if (fishScript != null)
            fishScript.FallSpeed = fallSpeed;
        else
            Debug.LogWarning("[FishSpawner] На префабе рыбы нет Fish.cs!");
    }
}
