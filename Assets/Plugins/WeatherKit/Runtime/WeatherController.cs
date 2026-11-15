using System.Collections;
using UnityEngine;

public enum WeatherPreset
{
    Clear,
    Cloudy,
    Rain,
    Storm,
    Snow,
    Fog
}

/// <summary>
/// Простая погода для Unity: автоматически создаёт дождь и снег,
/// меняет освещение и туман, поддерживает плавный переход между пресетами.
/// </summary>
public class WeatherController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private WeatherPreset startWeather = WeatherPreset.Clear;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Light sun;
    [Min(0f)] [SerializeField] private float transitionSeconds = 2f;
    [Min(1f)] [SerializeField] private float particleAreaSize = 24f;
    [Min(2f)] [SerializeField] private float particleHeight = 12f;

    private ParticleSystem rain;
    private ParticleSystem snow;
    private ParticleSystem.EmissionModule rainEmission;
    private ParticleSystem.EmissionModule snowEmission;
    private ParticleSystem.VelocityOverLifetimeModule rainVelocity;
    private ParticleSystem.VelocityOverLifetimeModule snowVelocity;
    private ParticleSystem.ShapeModule rainShape;
    private ParticleSystem.ShapeModule snowShape;
    private Material rainMaterial;
    private Material snowMaterial;
    private Coroutine transitionRoutine;
    private WeatherPreset currentWeather;

    private Color originalAmbient;
    private bool originalFog;
    private Color originalFogColor;
    private float originalFogDensity;
    private float originalSunIntensity;
    private Color originalSunColor;
    private bool capturedOriginals;

    public WeatherPreset CurrentWeather => currentWeather;

    private void Awake()
    {
        CaptureOriginalSettings();
        EnsureParticleSystems();
        SetWeather(startWeather, true);
    }

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        Vector3 targetPosition = followTarget.position;
        if (rain != null) rain.transform.position = targetPosition;
        if (snow != null) snow.transform.position = targetPosition;
    }

    private void OnDisable()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (rainMaterial != null) Destroy(rainMaterial);
        if (snowMaterial != null) Destroy(snowMaterial);
    }

    /// <summary>Применить погодный пресет. Можно вызвать из кнопки UI или другого скрипта.</summary>
    public void SetWeather(WeatherPreset preset)
    {
        SetWeather(preset, false);
    }

    private void SetWeather(WeatherPreset preset, bool instant)
    {
        currentWeather = preset;
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        WeatherValues target = GetValues(preset);
        if (instant || !Application.isPlaying || transitionSeconds <= 0f)
        {
            ApplyValues(target);
            return;
        }

        transitionRoutine = StartCoroutine(BlendTo(target, transitionSeconds));
    }

    private IEnumerator BlendTo(WeatherValues target, float seconds)
    {
        WeatherValues from = ReadCurrentValues();
        if (target.fogEnabled)
            RenderSettings.fog = true;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            ApplyValues(WeatherValues.Lerp(from, target, Mathf.Clamp01(elapsed / seconds)));
            yield return null;
        }

        ApplyValues(target);
        RenderSettings.fog = target.fogEnabled;
        transitionRoutine = null;
    }

    private void ApplyValues(WeatherValues values)
    {
        RenderSettings.ambientLight = values.ambient;
        RenderSettings.fogColor = values.fogColor;
        RenderSettings.fogDensity = values.fogDensity;
        if (values.fogEnabled)
            RenderSettings.fog = true;

        if (sun != null)
        {
            sun.intensity = values.sunIntensity;
            sun.color = values.sunColor;
        }

        SetParticleWeather(rain, rainEmission, rainVelocity, values.rainRate, values.wind, true);
        SetParticleWeather(snow, snowEmission, snowVelocity, values.snowRate, values.wind * 0.35f, false);

        if (!values.fogEnabled && values.fogDensity <= 0f)
            RenderSettings.fog = false;
    }

    private static void SetParticleWeather(
        ParticleSystem system,
        ParticleSystem.EmissionModule emission,
        ParticleSystem.VelocityOverLifetimeModule velocity,
        float rate,
        float wind,
        bool isRain)
    {
        if (system == null)
            return;

        emission.enabled = rate > 0.01f;
        emission.rateOverTime = rate;
        velocity.x = new ParticleSystem.MinMaxCurve(wind);
        if (isRain)
            velocity.y = new ParticleSystem.MinMaxCurve(-24f);
        else
            velocity.y = new ParticleSystem.MinMaxCurve(-2.2f);
    }

    private WeatherValues ReadCurrentValues()
    {
        return new WeatherValues
        {
            ambient = RenderSettings.ambientLight,
            sunIntensity = sun != null ? sun.intensity : 1f,
            sunColor = sun != null ? sun.color : Color.white,
            fogEnabled = RenderSettings.fog,
            fogColor = RenderSettings.fogColor,
            fogDensity = RenderSettings.fogDensity,
            rainRate = rainEmission.rateOverTime.constant,
            snowRate = snowEmission.rateOverTime.constant,
            wind = rainVelocity.x.constant
        };
    }

    private static WeatherValues GetValues(WeatherPreset preset)
    {
        switch (preset)
        {
            case WeatherPreset.Cloudy:
                return new WeatherValues(new Color(0.43f, 0.47f, 0.53f), 0.65f, new Color(0.78f, 0.82f, 0.9f), false, new Color(0.48f, 0.52f, 0.58f), 0f, 0f, 0f, 2f);
            case WeatherPreset.Rain:
                return new WeatherValues(new Color(0.34f, 0.39f, 0.48f), 0.45f, new Color(0.68f, 0.74f, 0.84f), true, new Color(0.46f, 0.52f, 0.61f), 0.006f, 1500f, 0f, 3f);
            case WeatherPreset.Storm:
                return new WeatherValues(new Color(0.20f, 0.23f, 0.31f), 0.24f, new Color(0.55f, 0.61f, 0.72f), true, new Color(0.30f, 0.34f, 0.43f), 0.012f, 2800f, 0f, 8f);
            case WeatherPreset.Snow:
                return new WeatherValues(new Color(0.62f, 0.67f, 0.75f), 0.78f, new Color(0.83f, 0.88f, 0.98f), true, new Color(0.70f, 0.76f, 0.85f), 0.004f, 0f, 650f, 1.5f);
            case WeatherPreset.Fog:
                return new WeatherValues(new Color(0.45f, 0.48f, 0.53f), 0.55f, new Color(0.76f, 0.79f, 0.83f), true, new Color(0.72f, 0.76f, 0.82f), 0.035f, 0f, 0f, 0f);
            default: // Clear
                return new WeatherValues(new Color(0.58f, 0.62f, 0.70f), 1.15f, new Color(1f, 0.95f, 0.84f), false, new Color(0.65f, 0.75f, 0.9f), 0f, 0f, 0f, 0f);
        }
    }

    private void CaptureOriginalSettings()
    {
        originalAmbient = RenderSettings.ambientLight;
        originalFog = RenderSettings.fog;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalSunIntensity = sun != null ? sun.intensity : 1f;
        originalSunColor = sun != null ? sun.color : Color.white;
        capturedOriginals = true;
    }

    private void EnsureParticleSystems()
    {
        if (rain == null)
            rain = CreateParticleSystem("Weather Rain", true, out rainMaterial);
        if (snow == null)
            snow = CreateParticleSystem("Weather Snow", false, out snowMaterial);

        rainEmission = rain.emission;
        snowEmission = snow.emission;
        rainVelocity = rain.velocityOverLifetime;
        snowVelocity = snow.velocityOverLifetime;
        rainShape = rain.shape;
        snowShape = snow.shape;
        ConfigureShape(rainShape);
        ConfigureShape(snowShape);

        Transform initialTarget = followTarget != null ? followTarget : transform;
        rain.transform.position = initialTarget.position;
        snow.transform.position = initialTarget.position;
    }

    private void ConfigureShape(ParticleSystem.ShapeModule shape)
    {
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0f, particleHeight, 0f);
        shape.scale = new Vector3(particleAreaSize, 0.1f, particleAreaSize);
    }

    private ParticleSystem CreateParticleSystem(string objectName, bool isRain, out Material generated)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, true);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = isRain ? 4000 : 1200;
        main.startLifetime = isRain ? 2.5f : 10f;
        main.startSpeed = 0f;
        main.startSize = isRain ? 0.035f : 0.09f;
        main.startColor = isRain ? new Color(0.72f, 0.86f, 1f, 0.7f) : new Color(0.95f, 0.98f, 1f, 0.9f);

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = true;
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(isRain ? -24f : -2.2f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = isRain ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (isRain)
        {
            renderer.lengthScale = 3f;
            renderer.velocityScale = 0.1f;
        }
        generated = CreateParticleMaterial();
        if (generated != null)
            renderer.sharedMaterial = generated;

        ps.Play();
        return ps;
    }

    private static Material CreateParticleMaterial()
    {
        string pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name
            : string.Empty;
        Shader shader = null;
        if (pipeline.Contains("Universal")) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        else if (pipeline.Contains("HDRender")) shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        return new Material(shader) { name = "Weather Particle Runtime Material" };
    }

    private struct WeatherValues
    {
        public Color ambient;
        public float sunIntensity;
        public Color sunColor;
        public bool fogEnabled;
        public Color fogColor;
        public float fogDensity;
        public float rainRate;
        public float snowRate;
        public float wind;

        public WeatherValues(Color ambient, float sunIntensity, Color sunColor, bool fogEnabled,
            Color fogColor, float fogDensity, float rainRate, float snowRate, float wind)
        {
            this.ambient = ambient;
            this.sunIntensity = sunIntensity;
            this.sunColor = sunColor;
            this.fogEnabled = fogEnabled;
            this.fogColor = fogColor;
            this.fogDensity = fogDensity;
            this.rainRate = rainRate;
            this.snowRate = snowRate;
            this.wind = wind;
        }

        public static WeatherValues Lerp(WeatherValues a, WeatherValues b, float t)
        {
            return new WeatherValues
            {
                ambient = Color.Lerp(a.ambient, b.ambient, t),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
                fogEnabled = b.fogEnabled || a.fogEnabled,
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                rainRate = Mathf.Lerp(a.rainRate, b.rainRate, t),
                snowRate = Mathf.Lerp(a.snowRate, b.snowRate, t),
                wind = Mathf.Lerp(a.wind, b.wind, t)
            };
        }
    }
}
