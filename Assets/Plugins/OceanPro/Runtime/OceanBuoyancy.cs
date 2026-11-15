using UnityEngine;

/// <summary>Simple multi-point buoyancy for boats and the included test cube.</summary>
[RequireComponent(typeof(Rigidbody))]
public class OceanBuoyancy : MonoBehaviour
{
    [SerializeField] private OceanSystem ocean;
    [SerializeField] private Vector3[] localFloatPoints;
    [Min(0.05f)] [SerializeField] private float floatDepth = 0.7f;
    [Min(0f)] [SerializeField] private float draft = 0.12f;
    [Min(0.1f)] [SerializeField] private float buoyancyStrength = 2f;
    [Min(0f)] [SerializeField] private float waterDrag = 1.8f;
    [Min(0f)] [SerializeField] private float normalAlignStrength = 0.8f;
    [Header("След и пена за движущимся объектом")]
    [Min(0.1f)] [SerializeField] private float wakeRadius = 3f;
    [Range(0f, 2f)] [SerializeField] private float wakeStrength = 1f;

    private Rigidbody body;

    public void SetOcean(OceanSystem oceanSystem) => ocean = oceanSystem;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (localFloatPoints == null || localFloatPoints.Length == 0)
            CreateDefaultFloatPoints();
    }

    private void FixedUpdate()
    {
        if (ocean == null || body == null || localFloatPoints == null || localFloatPoints.Length == 0)
            return;

        Vector3 averageNormal = Vector3.zero;
        int wetPoints = 0;
        float totalImmersion = 0f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 point = transform.TransformPoint(localFloatPoints[i]);
            float waterY = ocean.GetWaterHeight(point, out Vector3 waterNormal, out _);
            float submerged = waterY - point.y + draft;
            if (submerged <= 0f)
                continue;

            float immersion = Mathf.Clamp01(submerged / floatDepth);
            float acceleration = gravity * buoyancyStrength * immersion / localFloatPoints.Length;
            body.AddForceAtPosition(Vector3.up * acceleration, point, ForceMode.Acceleration);
            averageNormal += waterNormal;
            totalImmersion += immersion;
            wetPoints++;
        }

        if (wetPoints == 0)
        {
            ocean.RemoveWakeSource(transform);
            return;
        }

        float wetFraction = totalImmersion / localFloatPoints.Length;
        body.AddForce(-body.linearVelocity * waterDrag * wetFraction, ForceMode.Acceleration);
        ocean.SetWakeSource(transform, body.linearVelocity, wakeRadius, wakeStrength * wetFraction);
        averageNormal.Normalize();
        Vector3 alignTorque = Vector3.Cross(transform.up, averageNormal) * normalAlignStrength;
        body.AddTorque(alignTorque, ForceMode.Acceleration);
    }

    private void OnDisable()
    {
        if (ocean != null)
            ocean.RemoveWakeSource(transform);
    }

    private void CreateDefaultFloatPoints()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Vector3 half = box.size * 0.42f;
            Vector3 c = box.center;
            float y = c.y - box.size.y * 0.42f;
            localFloatPoints = new[]
            {
                new Vector3(c.x - half.x, y, c.z - half.z),
                new Vector3(c.x - half.x, y, c.z + half.z),
                new Vector3(c.x + half.x, y, c.z - half.z),
                new Vector3(c.x + half.x, y, c.z + half.z)
            };
        }
        else
        {
            localFloatPoints = new[]
            {
                new Vector3(-0.45f, -0.45f, -0.45f),
                new Vector3(-0.45f, -0.45f, 0.45f),
                new Vector3(0.45f, -0.45f, -0.45f),
                new Vector3(0.45f, -0.45f, 0.45f)
            };
        }
    }
}
