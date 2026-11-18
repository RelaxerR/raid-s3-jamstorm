using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Задаёт радиальное давление моря и амплитуду существующей поверхности OceanPro.</summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class WaveController : MonoBehaviour
    {
        [SerializeField] private Transform _islandCenter;
        [SerializeField] private OceanSystem _ocean;
        [SerializeField, Min(0f)] private float _calmRadius = 12f;
        [SerializeField, Min(0.01f)] private float _maximumForceRadius = 50f;
        [SerializeField, Min(0f)] private float _minimumForce = 0.015f;
        [SerializeField, Min(0f)] private float _maximumForce = 1.2f;
        [SerializeField] private AnimationCurve _forceByDistance = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float _opposingResistance = 1.5f;
        [SerializeField, Min(0f)] private float _returnMultiplier = 1f;
        [SerializeField, Min(0f)] private float _centerAmplitude = 0.08f;
        [SerializeField, Min(0f)] private float _outerAmplitude = 0.9f;

        public OceanSystem Ocean => _ocean;
        public Vector3 Center => _islandCenter != null ? _islandCenter.position : Vector3.zero;
        public Vector3 DirectionToCenter(Vector3 position) => Vector3.ProjectOnPlane(Center - position, Vector3.up).normalized;
        public float DistanceFactor(Vector3 position)
        {
            float distance = Vector3.ProjectOnPlane(position - Center, Vector3.up).magnitude;
            return Mathf.Clamp01((distance - _calmRadius) / Mathf.Max(0.01f, _maximumForceRadius - _calmRadius));
        }
        public float ForceAt(Vector3 position)
        {
            float t = DistanceFactor(position);
            float strength = _forceByDistance == null ? t : Mathf.Clamp01(_forceByDistance.Evaluate(t));
            return Mathf.Lerp(_minimumForce, Mathf.Max(_minimumForce, _maximumForce), strength);
        }
        public Vector3 ReturnAcceleration(Vector3 position) => DirectionToCenter(position) * (ForceAt(position) * _returnMultiplier);
        public float ResistanceAt(Vector3 position) => _opposingResistance * ForceAt(position);

        private void Awake() => ConfigureSurface();
        private void Update() => ConfigureSurface();
        private void ConfigureSurface()
        {
            if (_ocean != null)
                _ocean.ConfigureAmplitudeField(Center, _calmRadius, _maximumForceRadius, _centerAmplitude, _outerAmplitude);
        }
    }
}
