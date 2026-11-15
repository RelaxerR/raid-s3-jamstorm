using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>Процедурная молния между двумя Transform с Unity Input System.</summary>
[RequireComponent(typeof(LineRenderer))]
public class LightningBolt : MonoBehaviour
{
    [Header("Точки молнии")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Управление (Unity Input System)")]
    [SerializeField] private bool listenForAction = true;
    [SerializeField] private InputAction strikeAction = new InputAction(
        "Strike", InputActionType.Button, "<Keyboard>/space");

    [Header("Форма")]
    [Min(2)] [SerializeField] private int segmentCount = 18;
    [Min(0f)] [SerializeField] private float jitter = 0.5f;
    [Min(0.001f)] [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private Color lightningColor = new Color(0.12f, 0.58f, 1f, 1f);

    [Header("Окантовка")]
    [SerializeField] private bool outlineEnabled = true;
    [Min(0f)] [SerializeField] private float outlineWidth = 0.012f;
    [SerializeField] private Color outlineColor = new Color(0.025f, 0.06f, 0.18f, 1f);

    [Header("Вспышка")]
    [Min(0.01f)] [SerializeField] private float duration = 0.18f;
    [Min(0.01f)] [SerializeField] private float refreshInterval = 0.035f;

    private LineRenderer line;
    private LineRenderer outlineLine;
    private Coroutine strikeRoutine;
    private Vector3[] points;
    private Material generatedMaterial;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        ConfigureLine(line);
        CreateOutline();
        EnsureCompatibleMaterial();
        ApplyStyle();

        line.positionCount = 0;
        line.enabled = false;
    }

    private void OnEnable()
    {
        if (strikeAction == null)
            return;

        strikeAction.performed += OnStrikePerformed;
        if (listenForAction)
            strikeAction.Enable();
    }

    private void OnDisable()
    {
        if (strikeAction != null)
        {
            strikeAction.performed -= OnStrikePerformed;
            strikeAction.Disable();
        }

        if (strikeRoutine != null)
        {
            StopCoroutine(strikeRoutine);
            strikeRoutine = null;
        }

        if (line != null)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
        if (outlineLine != null)
        {
            outlineLine.positionCount = 0;
            outlineLine.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (generatedMaterial != null)
        {
            if (Application.isPlaying) Destroy(generatedMaterial);
            else DestroyImmediate(generatedMaterial);
        }
    }

    private void OnStrikePerformed(InputAction.CallbackContext context)
    {
        if (listenForAction)
            Strike();
    }

    /// <summary>Показать разряд. Можно вызывать из другого скрипта или UnityEvent.</summary>
    public void Strike()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("LightningBolt: назначьте Start Point и End Point в Inspector.", this);
            return;
        }

        if (strikeRoutine != null)
            StopCoroutine(strikeRoutine);
        strikeRoutine = StartCoroutine(ShowBolt());
    }

    private IEnumerator ShowBolt()
    {
        line.enabled = true;
        if (outlineLine != null)
            outlineLine.enabled = outlineEnabled;

        float elapsed = 0f;
        float interval = Mathf.Max(0.01f, refreshInterval);
        while (elapsed < duration)
        {
            BuildBolt();
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        line.positionCount = 0;
        line.enabled = false;
        if (outlineLine != null)
        {
            outlineLine.positionCount = 0;
            outlineLine.enabled = false;
        }
        strikeRoutine = null;
    }

    private void BuildBolt()
    {
        int count = Mathf.Max(2, segmentCount) + 1;
        if (points == null || points.Length != count)
            points = new Vector3[count];

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        Vector3 direction = end - start;
        Vector3 side = Vector3.Cross(direction.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.001f)
            side = Vector3.Cross(direction.normalized, Vector3.right);
        side.Normalize();
        Vector3 otherSide = Vector3.Cross(direction.normalized, side).normalized;

        points[0] = start;
        points[count - 1] = end;
        for (int i = 1; i < count - 1; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 center = Vector3.Lerp(start, end, t);
            Vector3 offset = side * Random.Range(-jitter, jitter)
                           + otherSide * Random.Range(-jitter, jitter) * 0.35f;
            points[i] = center + offset;
        }

        line.positionCount = count;
        line.SetPositions(points);
        ApplyStyle();
        if (outlineLine != null)
        {
            outlineLine.positionCount = count;
            outlineLine.SetPositions(points);
        }
    }

    private void ConfigureLine(LineRenderer renderer)
    {
        renderer.useWorldSpace = true;
        renderer.startColor = Color.white;
        renderer.endColor = Color.white;
        renderer.alignment = LineAlignment.View;
        renderer.numCapVertices = 0;
        renderer.numCornerVertices = 0;
    }

    private void CreateOutline()
    {
        Transform child = transform.Find("Lightning Outline Renderer");
        if (child == null)
        {
            var outlineObject = new GameObject("Lightning Outline Renderer");
            outlineObject.transform.SetParent(transform, false);
            child = outlineObject.transform;
        }

        outlineLine = child.GetComponent<LineRenderer>();
        if (outlineLine == null)
            outlineLine = child.gameObject.AddComponent<LineRenderer>();

        ConfigureLine(outlineLine);
        outlineLine.positionCount = 0;
        outlineLine.enabled = false;
    }

    private void ApplyStyle()
    {
        if (line == null)
            return;

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = lightningColor;
        line.endColor = lightningColor;

        if (outlineLine == null)
            return;

        outlineLine.startWidth = lineWidth + outlineWidth * 2f;
        outlineLine.endWidth = lineWidth + outlineWidth * 2f;
        outlineLine.startColor = outlineColor;
        outlineLine.endColor = outlineColor;
        outlineLine.sortingLayerID = line.sortingLayerID;
        outlineLine.sortingOrder = line.sortingOrder - 1;
        outlineLine.sharedMaterial = line.sharedMaterial;
    }

    private void EnsureCompatibleMaterial()
    {
        Material current = line.sharedMaterial;
        string pipelineName = GraphicsSettings.currentRenderPipeline != null
            ? GraphicsSettings.currentRenderPipeline.GetType().Name
            : string.Empty;
        string shaderName = current != null && current.shader != null ? current.shader.name : string.Empty;

        bool isUrp = pipelineName.Contains("Universal");
        bool isHdrp = pipelineName.Contains("HDRender");
        bool incompatible = current == null || current.shader == null || !current.shader.isSupported
            || shaderName.Contains("InternalError")
            || (isUrp && !shaderName.StartsWith("Universal Render Pipeline/"))
            || (isHdrp && !shaderName.StartsWith("HDRP/"));

        if (!incompatible)
            return;

        Shader shader = null;
        if (isUrp) shader = Shader.Find("Universal Render Pipeline/Unlit");
        else if (isHdrp) shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("LightningBolt: не найден совместимый Unlit shader. Назначьте материал вручную в Line Renderer.", this);
            return;
        }

        generatedMaterial = new Material(shader) { name = "LightningBolt Runtime Unlit" };
        line.sharedMaterial = generatedMaterial;
    }
}
