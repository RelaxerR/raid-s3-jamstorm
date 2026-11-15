using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single-object ocean: one generated mesh/material, depth tint, foam, Gerstner waves and CPU wave sampling.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanSystem : MonoBehaviour
{
    [Header("Поверхность")]
    [Min(10f)] [SerializeField] private float oceanSize = 1000f;
    [Range(32, 256)] [SerializeField] private int resolution = 256;
    [Min(0.05f)] [SerializeField] private float waveHeight = 1.8f;
    [Min(0f)] [SerializeField] private float waveSpeed = 1f;
    [Range(0f, 1.5f)] [SerializeField] private float waveSteepness = 0.62f;

    [Header("Глубина и цвет")]
    [SerializeField] private Color shallowColor = new Color(0.04f, 0.44f, 0.60f, 1f);
    [SerializeField] private Color middleDepthColor = new Color(0.015f, 0.16f, 0.34f, 1f);
    [SerializeField] private Color deepColor = new Color(0.005f, 0.035f, 0.12f, 1f);
    [Min(10f)] [SerializeField] private float waterDepth = 250f;
    [SerializeField] private bool createDepthVolume = true;
    [Min(0.001f)] [SerializeField] private float depthDensity = 0.045f;
    [Range(0.1f, 1f)] [SerializeField] private float opacity = 0.82f;
    [Range(0f, 1f)] [SerializeField] private float fresnelStrength = 0.3f;

    [Header("Пена")]
    [SerializeField] private Color foamColor = new Color(0.82f, 0.94f, 1f, 1f);
    [Min(0.01f)] [SerializeField] private float foamDepth = 0.7f;
    [Range(0f, 2f)] [SerializeField] private float crestFoamStrength = 0.8f;
    [Range(0f, 2f)] [SerializeField] private float intersectionFoamStrength = 1.1f;
    [Range(0f, 4f)] [SerializeField] private float foamScale = 0.12f;

    private Mesh generatedMesh;
    private Material generatedMaterial;
    private Mesh generatedVolumeMesh;
    private Mesh generatedBottomMesh;
    private Material volumeMaterial;
    private GameObject volumeObject;
    private GameObject bottomObject;
    private Vector3[] volumeVertices;
    private int[] volumeTopIndices;
    private const int MaxWakeSources = 4;
    private readonly List<WakeSource> wakeSources = new List<WakeSource>();
    private readonly Vector4[] wakeData = new Vector4[MaxWakeSources];
    private readonly Vector4[] wakeMotion = new Vector4[MaxWakeSources];
    private static readonly WaveData[] Waves =
    {
        new WaveData(new Vector2(1f, 0.23f), 220f, 0.28f, 0.4f),
        new WaveData(new Vector2(-0.48f, 1f), 142f, 0.23f, 2.0f),
        new WaveData(new Vector2(0.82f, 0.57f), 88f, 0.19f, 4.4f),
        new WaveData(new Vector2(-0.93f, -0.36f), 54f, 0.14f, 1.3f),
        new WaveData(new Vector2(0.16f, -1f), 34f, 0.10f, 5.2f),
        new WaveData(new Vector2(0.63f, -0.78f), 23f, 0.06f, 3.1f)
    };

    public float WaveHeight => waveHeight;
    public float WaveSpeed => waveSpeed;
    public float WaveSteepness => waveSteepness;

    private void Awake()
    {
        BuildOcean();
        EnableCameraDepth();
    }

    private void OnEnable() => EnableCameraDepth();

    private void LateUpdate()
    {
        if (generatedMaterial == null) return;
        UpdateDepthVolumeSurface();

        for (int i = wakeSources.Count - 1; i >= 0; i--)
        {
            if (wakeSources[i].transform == null)
                wakeSources.RemoveAt(i);
        }

        for (int i = 0; i < MaxWakeSources; i++)
        {
            wakeData[i] = Vector4.zero;
            wakeMotion[i] = Vector4.zero;
            if (i >= wakeSources.Count) continue;

            WakeSource source = wakeSources[i];
            Vector3 p = source.transform.position;
            wakeData[i] = new Vector4(p.x, p.z, source.radius, source.strength);
            wakeMotion[i] = new Vector4(source.velocity.x, source.velocity.z, source.velocity.magnitude, 0f);
        }

        generatedMaterial.SetVectorArray("_WakeData", wakeData);
        generatedMaterial.SetVectorArray("_WakeMotion", wakeMotion);
        generatedMaterial.SetFloat("_WakeCount", Mathf.Min(wakeSources.Count, MaxWakeSources));
    }

    /// <summary>Register/update one moving object for a soft trailing wake.</summary>
    public void SetWakeSource(Transform source, Vector3 velocity, float radius, float strength)
    {
        if (source == null) return;
        for (int i = 0; i < wakeSources.Count; i++)
        {
            if (wakeSources[i].transform != source) continue;
            wakeSources[i].velocity = velocity;
            wakeSources[i].radius = radius;
            wakeSources[i].strength = strength;
            return;
        }

        if (wakeSources.Count < MaxWakeSources)
            wakeSources.Add(new WakeSource(source, velocity, radius, strength));
    }

    public void RemoveWakeSource(Transform source)
    {
        for (int i = wakeSources.Count - 1; i >= 0; i--)
            if (wakeSources[i].transform == source)
                wakeSources.RemoveAt(i);
    }

    private void OnDestroy()
    {
        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        if (generatedMaterial != null)
        {
            if (Application.isPlaying) Destroy(generatedMaterial);
            else DestroyImmediate(generatedMaterial);
        }
        if (generatedVolumeMesh != null) DestroyOwned(generatedVolumeMesh);
        if (generatedBottomMesh != null) DestroyOwned(generatedBottomMesh);
        if (volumeMaterial != null) DestroyOwned(volumeMaterial);
        if (volumeObject != null) DestroyOwned(volumeObject);
        if (bottomObject != null) DestroyOwned(bottomObject);
    }

    private void EnableCameraDepth()
    {
        Camera camera = Camera.main;
        if (camera != null)
            camera.depthTextureMode |= DepthTextureMode.Depth;
    }

    /// <summary>Rebuilds the mesh and runtime material using the current Inspector settings.</summary>
    public void BuildOcean()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        ReleaseGeneratedAssets();

        generatedMesh = CreateGrid(resolution, oceanSize);
        generatedMesh.name = "Ocean Surface (generated)";
        filter.sharedMesh = generatedMesh;

        Shader shader = Shader.Find("OceanPro/Depth Foam Water");
        if (shader == null)
        {
            Debug.LogError("OceanPro: shader OceanPro/Depth Foam Water not found. Check that OceanDepthFoam.shader imported without errors.", this);
            return;
        }

        generatedMaterial = new Material(shader) { name = "OceanPro Water (runtime)" };
        generatedMaterial.SetColor("_ShallowColor", shallowColor);
        generatedMaterial.SetColor("_DeepColor", deepColor);
        generatedMaterial.SetColor("_FoamColor", foamColor);
        generatedMaterial.SetFloat("_WaveHeight", waveHeight);
        generatedMaterial.SetFloat("_WaveSpeed", waveSpeed);
        generatedMaterial.SetFloat("_Steepness", waveSteepness);
        generatedMaterial.SetFloat("_DepthDensity", depthDensity);
        generatedMaterial.SetFloat("_Opacity", opacity);
        generatedMaterial.SetFloat("_FresnelStrength", fresnelStrength);
        generatedMaterial.SetFloat("_FoamDepth", foamDepth);
        generatedMaterial.SetFloat("_CrestFoamStrength", crestFoamStrength);
        generatedMaterial.SetFloat("_IntersectionFoamStrength", intersectionFoamStrength);
        generatedMaterial.SetFloat("_FoamScale", foamScale);
        meshRenderer.sharedMaterial = generatedMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;

        if (createDepthVolume)
            BuildDepthVolume();
    }

    private void BuildDepthVolume()
    {
        Shader volumeShader = Shader.Find("OceanPro/Depth Volume");
        if (volumeShader == null)
        {
            Debug.LogWarning("OceanPro: depth volume shader missing; surface still works, but side/deep bands are disabled.", this);
            return;
        }

        volumeMaterial = new Material(volumeShader) { name = "OceanPro Depth Bands (runtime)" };
        int edgeResolution = Mathf.Clamp(resolution, 32, 128);
        generatedVolumeMesh = CreateDepthWalls(edgeResolution, oceanSize, waterDepth, out volumeVertices, out volumeTopIndices);
        volumeObject = new GameObject("Ocean depth bands (generated)");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.AddComponent<MeshFilter>().sharedMesh = generatedVolumeMesh;
        MeshRenderer wallRenderer = volumeObject.AddComponent<MeshRenderer>();
        wallRenderer.sharedMaterial = volumeMaterial;
        wallRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        wallRenderer.receiveShadows = false;

        generatedBottomMesh = CreateGrid(Mathf.Clamp(edgeResolution / 2, 16, 64), oceanSize);
        Vector3[] bottomVertices = generatedBottomMesh.vertices;
        Color[] bottomColors = new Color[bottomVertices.Length];
        for (int i = 0; i < bottomVertices.Length; i++)
        {
            bottomVertices[i].y = -waterDepth;
            bottomColors[i] = deepColor;
        }
        generatedBottomMesh.vertices = bottomVertices;
        generatedBottomMesh.colors = bottomColors;
        generatedBottomMesh.RecalculateNormals();
        generatedBottomMesh.RecalculateBounds();
        generatedBottomMesh.name = "Ocean dark seabed (generated)";
        bottomObject = new GameObject("Ocean deep bottom (generated)");
        bottomObject.transform.SetParent(transform, false);
        bottomObject.AddComponent<MeshFilter>().sharedMesh = generatedBottomMesh;
        MeshRenderer bottomRenderer = bottomObject.AddComponent<MeshRenderer>();
        bottomRenderer.sharedMaterial = volumeMaterial;
        bottomRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bottomRenderer.receiveShadows = false;
    }

    private void UpdateDepthVolumeSurface()
    {
        if (generatedVolumeMesh == null || volumeVertices == null || volumeTopIndices == null) return;
        for (int i = 0; i < volumeTopIndices.Length; i++)
        {
            int index = volumeTopIndices[i];
            Vector3 local = volumeVertices[index];
            Vector3 world = transform.TransformPoint(new Vector3(local.x, 0f, local.z));
            local.y = GetWaterHeight(world, out _, out _) - transform.position.y;
            volumeVertices[index] = local;
        }
        generatedVolumeMesh.vertices = volumeVertices;
        generatedVolumeMesh.RecalculateNormals();
        generatedVolumeMesh.RecalculateBounds();
    }

    private Mesh CreateDepthWalls(int edgeResolution, float size, float depth,
        out Vector3[] vertices, out int[] topIndices)
    {
        int rows = edgeResolution + 1;
        const int levels = 4;
        int verticesPerSide = rows * levels;
        vertices = new Vector3[verticesPerSide * 4];
        Color[] colors = new Color[vertices.Length];
        topIndices = new int[rows * 4];
        int[] triangles = new int[4 * edgeResolution * (levels - 1) * 6];
        float h = size * 0.5f;
        float[] yLevels = { 0f, -depth * 0.33f, -depth * 0.68f, -depth };
        Color[] bandColors = { shallowColor, middleDepthColor, deepColor, deepColor };
        int tri = 0;

        for (int side = 0; side < 4; side++)
        {
            int sideStart = side * verticesPerSide;
            for (int row = 0; row < rows; row++)
            {
                float t = (float)row / edgeResolution;
                float x, z;
                if (side == 0) { x = Mathf.Lerp(-h, h, t); z = -h; }
                else if (side == 1) { x = Mathf.Lerp(h, -h, t); z = h; }
                else if (side == 2) { x = -h; z = Mathf.Lerp(h, -h, t); }
                else { x = h; z = Mathf.Lerp(-h, h, t); }

                for (int level = 0; level < levels; level++)
                {
                    int idx = sideStart + row * levels + level;
                    vertices[idx] = new Vector3(x, yLevels[level], z);
                    colors[idx] = bandColors[level];
                    if (level == 0) topIndices[side * rows + row] = idx;
                }
            }

            for (int row = 0; row < edgeResolution; row++)
            {
                for (int level = 0; level < levels - 1; level++)
                {
                    int a = sideStart + row * levels + level;
                    int b = a + levels;
                    int c = a + 1;
                    int d = b + 1;
                    triangles[tri++] = a; triangles[tri++] = b; triangles[tri++] = c;
                    triangles[tri++] = c; triangles[tri++] = b; triangles[tri++] = d;
                }
            }
        }

        Mesh mesh = new Mesh { name = "Ocean depth color bands" };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.MarkDynamic();
        return mesh;
    }

    private void DestroyOwned(Object asset)
    {
        if (asset == null) return;
        if (Application.isPlaying) Destroy(asset);
        else DestroyImmediate(asset);
    }

    /// <summary>Sample the same wave field used by the shader. Used by floating objects.</summary>
    public float GetWaterHeight(Vector3 worldPosition, out Vector3 normal, out float crestFoam)
    {
        Vector3 p = worldPosition;
        p.y = transform.position.y;
        Vector3 tangentX = Vector3.right;
        Vector3 tangentZ = Vector3.forward;
        for (int i = 0; i < Waves.Length; i++)
        {
            WaveData w = Waves[i];
            float amplitude = waveHeight * w.amplitude;
            float k = 2f * Mathf.PI / w.length;
            float angularSpeed = Mathf.Sqrt(9.81f * k) * waveSpeed;
            float phase = k * Vector2.Dot(w.direction, new Vector2(worldPosition.x, worldPosition.z))
                        - angularSpeed * Time.time + w.phase;
            float sin = Mathf.Sin(phase);
            float cos = Mathf.Cos(phase);
            float horizontal = waveSteepness * amplitude;
            p.x += w.direction.x * horizontal * cos;
            p.z += w.direction.y * horizontal * cos;
            p.y += amplitude * sin;

            tangentX += new Vector3(
                -waveSteepness * amplitude * k * w.direction.x * w.direction.x * sin,
                amplitude * k * w.direction.x * cos,
                -waveSteepness * amplitude * k * w.direction.x * w.direction.y * sin);
            tangentZ += new Vector3(
                -waveSteepness * amplitude * k * w.direction.x * w.direction.y * sin,
                amplitude * k * w.direction.y * cos,
                -waveSteepness * amplitude * k * w.direction.y * w.direction.y * sin);
        }

        normal = Vector3.Cross(tangentZ, tangentX).normalized;
        float displacement = p.y - transform.position.y;
        crestFoam = waveHeight > 0.001f ? Mathf.Clamp01((displacement / waveHeight - 0.36f) * 2.8f) : 0f;
        return p.y;
    }

    private Mesh CreateGrid(int gridResolution, float size)
    {
        int side = Mathf.Max(2, gridResolution) + 1;
        Vector3[] vertices = new Vector3[side * side];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(side - 1) * (side - 1) * 6];
        int tri = 0;
        for (int z = 0; z < side; z++)
        {
            for (int x = 0; x < side; x++)
            {
                int index = z * side + x;
                float u = (float)x / (side - 1);
                float v = (float)z / (side - 1);
                vertices[index] = new Vector3((u - 0.5f) * size, 0f, (v - 0.5f) * size);
                uv[index] = new Vector2(u, v);
                if (x < side - 1 && z < side - 1)
                {
                    triangles[tri++] = index;
                    triangles[tri++] = index + side;
                    triangles[tri++] = index + 1;
                    triangles[tri++] = index + 1;
                    triangles[tri++] = index + side;
                    triangles[tri++] = index + side + 1;
                }
            }
        }

        Mesh mesh = new Mesh { name = "Ocean Surface" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.MarkDynamic();
        return mesh;
    }

    private void ReleaseGeneratedAssets()
    {
        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        if (generatedMaterial != null)
        {
            if (Application.isPlaying) Destroy(generatedMaterial);
            else DestroyImmediate(generatedMaterial);
        }
        generatedMesh = null;
        generatedMaterial = null;
        DestroyOwned(generatedVolumeMesh);
        DestroyOwned(generatedBottomMesh);
        DestroyOwned(volumeMaterial);
        DestroyOwned(volumeObject);
        DestroyOwned(bottomObject);
        generatedVolumeMesh = null;
        generatedBottomMesh = null;
        volumeMaterial = null;
        volumeObject = null;
        bottomObject = null;
        volumeVertices = null;
        volumeTopIndices = null;
    }

    private class WakeSource
    {
        public Transform transform;
        public Vector3 velocity;
        public float radius;
        public float strength;

        public WakeSource(Transform transform, Vector3 velocity, float radius, float strength)
        {
            this.transform = transform;
            this.velocity = velocity;
            this.radius = radius;
            this.strength = strength;
        }
    }

    private struct WaveData
    {
        public readonly Vector2 direction;
        public readonly float length;
        public readonly float amplitude;
        public readonly float phase;

        public WaveData(Vector2 direction, float length, float amplitude, float phase)
        {
            this.direction = direction.normalized;
            this.length = length;
            this.amplitude = amplitude;
            this.phase = phase;
        }
    }
}
