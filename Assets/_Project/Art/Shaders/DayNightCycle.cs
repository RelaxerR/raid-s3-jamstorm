using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Настройки времени")]
    [Tooltip("Длительность полного цикла в секундах")]
    public float dayLengthInSeconds = 120f;
    [Range(0, 1)] public float timeOfDay = 0.25f; // 0-1, где 0.25 = полдень

    [Header("Ссылки")]
    public Light directionalLight;
    public Material skyboxMaterial;

    [Header("Кривые для скайбокса")]
    [Tooltip("Кривая прозрачности для перехода между скайбоксами")]
    public AnimationCurve skyboxBlendCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Кривые для Directional Light")]
    public Gradient lightColor;
    public AnimationCurve lightIntensity = AnimationCurve.Linear(0, 0, 1, 1);

    void Update()
    {
        // Обновляем время
        timeOfDay += Time.deltaTime / dayLengthInSeconds;
        if (timeOfDay > 1) timeOfDay -= 1;

        // Вращаем светило (солнце/луну) по кругу
        directionalLight.transform.rotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 170f, 0);

        // Управляем цветом и интенсивностью света
        directionalLight.color = lightColor.Evaluate(timeOfDay);
        directionalLight.intensity = lightIntensity.Evaluate(timeOfDay);

        // Управляем переходом скайбокса (если в шейдере есть параметр _Blend)
        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_Blend"))
        {
            skyboxMaterial.SetFloat("_Blend", skyboxBlendCurve.Evaluate(timeOfDay));
        }
    }
}