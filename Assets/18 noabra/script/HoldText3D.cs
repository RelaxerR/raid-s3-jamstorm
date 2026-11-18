using TMPro;
using UnityEngine;

/// <summary>
/// 3) ТЕКСТ В 3D-МИРЕ v4 - красный при полном + "поп" при изменении.
/// Вешается на TextMeshPro (3D Object -> TextMeshPro).
/// </summary>
public class HoldText3D : MonoBehaviour
{
    [SerializeField] private TextMeshPro tmpText;
    [SerializeField] private string prefix = "Трюм: ";
    [SerializeField] private string fullText = "Трюм: ПОЛОН!";

    private float popTime;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshPro>();
        if (tmpText == null)
            Debug.LogWarning("[HoldText3D] Не найден TextMeshPro!");
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (popTime > 0f)
        {
            popTime -= Time.deltaTime;
            float k = Mathf.Clamp01(popTime / 0.2f);
            transform.localScale = baseScale * (1f + k * 0.3f);
            if (popTime <= 0f) transform.localScale = baseScale;
        }
    }

    public void SetText(int current, int max)
    {
        if (tmpText == null) return;

        bool full = current >= max;
        tmpText.text = full ? fullText : $"{prefix}{current}/{max}";
        tmpText.color = full ? Color.red : Color.white;
        popTime = 0.2f; // поп-анимация
    }
}
