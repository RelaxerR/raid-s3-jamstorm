using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2) ТРЮМ v4 - рыба лежит КУЧЕЙ НА ДНЕ (не в воздухе), трюм трясётся при падении.
/// BoxCollider = Is Trigger = true. Материал куба - полупрозрачный!
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class CargoHold : MonoBehaviour
{
    [Header("Лимит трюма")]
    [SerializeField] private int maxFish = 10;

    [Header("3D-текст")]
    [SerializeField] private HoldText3D text3D;

    [Header("Куча на дне")]
    [Tooltip("Сколько рыб в одном слое, потом следующий слой выше")]
    [SerializeField] private int fishPerLayer = 5;
    [Tooltip("Высота одного слоя (метры)")]
    [SerializeField] private float layerHeight = 0.35f;

    private int currentFish;
    private readonly List<GameObject> storedFish = new List<GameObject>();
    private Vector3 homePos;
    private Coroutine shakeCoroutine;

    public int CurrentFish => currentFish;
    public int MaxFish => maxFish;
    public bool IsFull => currentFish >= maxFish;
    public IReadOnlyList<GameObject> StoredFish => storedFish;

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
        homePos = transform.position;
        UpdateText();
    }

    public bool AddFish(GameObject fish)
    {
        if (fish == null) return false;
        if (IsFull)
        {
            Debug.LogWarning("[CargoHold] Трюм полон!");
            return false;
        }

        Rigidbody rb = fish.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }

        // Кладём на дно кучей: слой по количеству + джиттер
        fish.transform.position = GetLandingPosition();
        fish.transform.rotation = Quaternion.Euler(
            Random.Range(-20f, 20f),
            Random.Range(0f, 360f),
            Random.Range(-20f, 20f));

        storedFish.Add(fish);
        currentFish++;
        Debug.Log($"[CargoHold] Рыба в трюме: {currentFish}/{maxFish} ({fish.name})");
        UpdateText();
        Shake();
        return true;
    }

    private Vector3 GetLandingPosition()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        Vector3 size = col != null ? col.size : Vector3.one;
        Vector3 scale = transform.lossyScale;

        float halfX = Mathf.Max(0.2f, (size.x * scale.x) * 0.5f - 0.4f);
        float halfZ = Mathf.Max(0.2f, (size.z * scale.z) * 0.5f - 0.4f);
        float bottomY = transform.position.y - (size.y * scale.y) * 0.5f + 0.3f;

        int layer = currentFish / Mathf.Max(1, fishPerLayer);
        float y = bottomY + layer * layerHeight + Random.Range(-0.05f, 0.05f);

        return new Vector3(
            transform.position.x + Random.Range(-halfX, halfX),
            y,
            transform.position.z + Random.Range(-halfZ, halfZ));
    }

    private void Shake()
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0.15f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            float k = t / 0.15f;
            transform.position = homePos + new Vector3(
                Random.Range(-0.05f, 0.05f) * k,
                Random.Range(-0.03f, 0.03f) * k,
                Random.Range(-0.05f, 0.05f) * k);
            yield return null;
        }
        transform.position = homePos;
    }

    public Dictionary<string, int> GetCountsByType()
    {
        var dict = new Dictionary<string, int>();
        foreach (GameObject f in storedFish)
        {
            if (f == null) continue;
            Fish fishScript = f.GetComponent<Fish>();
            string type = fishScript != null ? fishScript.FishTypeName : f.name.Replace("(Clone)", "").Trim();
            if (!dict.ContainsKey(type)) dict[type] = 0;
            dict[type]++;
        }
        return dict;
    }

    public int SellType(string typeName)
    {
        int sold = 0;
        for (int i = storedFish.Count - 1; i >= 0; i--)
        {
            GameObject f = storedFish[i];
            if (f == null) continue;
            Fish fishScript = f.GetComponent<Fish>();
            string type = fishScript != null ? fishScript.FishTypeName : f.name.Replace("(Clone)", "").Trim();
            if (type == typeName)
            {
                Destroy(f);
                storedFish.RemoveAt(i);
                sold++;
            }
        }
        currentFish = storedFish.Count;
        UpdateText();
        return sold;
    }

    public void SellAll()
    {
        foreach (GameObject f in storedFish)
            if (f != null) Destroy(f);
        storedFish.Clear();
        currentFish = 0;
        UpdateText();
    }

    private void UpdateText()
    {
        if (text3D != null)
            text3D.SetText(currentFish, maxFish);
    }
}
