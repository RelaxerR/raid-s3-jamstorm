using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Поддерживает лодку пружинами в четырёх точках, сохраняя отдельное управление горизонтальным ходом.</summary>
    [DefaultExecutionOrder(110)]
    [RequireComponent(typeof(BoatController))]
    [DisallowMultipleComponent]
    public sealed class BoatBuoyancy : MonoBehaviour
    {
        [SerializeField] private WaveController _waves;
        [SerializeField] private Transform _frontLeft;
        [SerializeField] private Transform _frontRight;
        [SerializeField] private Transform _rearLeft;
        [SerializeField] private Transform _rearRight;
        [SerializeField, Min(0f)] private float _draft = 0.1f;
        [SerializeField, Min(0f)] private float _supportStrength = 18f;
        [SerializeField, Min(0f)] private float _verticalDamping = 8f;
        [SerializeField, Min(0f)] private float _rotationSharpness = 4f;
        [SerializeField, Range(0f, 60f)] private float _maximumTilt = 20f;
        [SerializeField, Min(0f)] private float _maximumVerticalSpeed = 2f;
        private BoatController _boat;
        private float _verticalSpeed;
        public float VerticalSpeed => _verticalSpeed;
        public float AverageImmersion { get; private set; }
        public float SupportAcceleration { get; private set; }

        private void Awake() => _boat = GetComponent<BoatController>();
        private void Update() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            if (_boat.IsRespawning || _waves == null || _waves.Ocean == null || !_waves.Ocean.isActiveAndEnabled
                || _frontLeft == null || _frontRight == null || _rearLeft == null || _rearRight == null) return;
            Vector3 fl = SurfacePoint(_frontLeft);
            Vector3 fr = SurfacePoint(_frontRight);
            Vector3 rl = SurfacePoint(_rearLeft);
            Vector3 rr = SurfacePoint(_rearRight);
            AverageImmersion = ((fl.y - _frontLeft.position.y) + (fr.y - _frontRight.position.y)
                + (rl.y - _rearLeft.position.y) + (rr.y - _rearRight.position.y)) * 0.25f + _draft;
            // Ограниченный шаг удерживает пружину устойчивой при редких кадрах.
            float remaining = Mathf.Min(dt, 0.1f);
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, 0.01f);
                SupportAcceleration = AverageImmersion * _supportStrength - _verticalSpeed * _verticalDamping;
                _verticalSpeed = Mathf.Clamp(_verticalSpeed + SupportAcceleration * step, -_maximumVerticalSpeed, _maximumVerticalSpeed);
                float displacement = _verticalSpeed * step;
                transform.position += Vector3.up * displacement;
                AverageImmersion -= displacement;
                remaining -= step;
            }
            Vector3 across = (fr + rr - fl - rl) * 0.5f;
            Vector3 along = (fl + fr - rl - rr) * 0.5f;
            Vector3 normal = Vector3.Cross(along, across).normalized;
            normal = Vector3.RotateTowards(Vector3.up, normal, _maximumTilt * Mathf.Deg2Rad, 0f);
            Vector3 smoothedNormal = Vector3.Slerp(transform.up, normal,
                1f - Mathf.Exp(-_rotationSharpness * dt)).normalized;
            // Сохраняем горизонтальный курс руля при изменении крена и продольного наклона.
            Vector3 forward = _boat.HullDirection;
            forward.y = -Vector3.Dot(forward, smoothedNormal) / Mathf.Max(0.01f, smoothedNormal.y);
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward.normalized, smoothedNormal);
        }
        private Vector3 SurfacePoint(Transform point)
        {
            Vector3 position = point.position;
            position.y = _waves.Ocean.GetWaterHeight(position, out _, out _);
            return position;
        }
        public void ResetMotion()
        {
            _verticalSpeed = 0f;
            AverageImmersion = 0f;
            SupportAcceleration = 0f;
        }
        private void OnDisable() => ResetMotion();
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in new[] { _frontLeft, _frontRight, _rearLeft, _rearRight })
                if (point != null) Gizmos.DrawWireSphere(point.position, 0.12f);
        }
    }
}
