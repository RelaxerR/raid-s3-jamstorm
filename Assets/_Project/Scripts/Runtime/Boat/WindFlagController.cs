using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Удерживает флаг по мировому ветру независимо от поворота и качки корпуса.</summary>
    [DisallowMultipleComponent]
    public sealed class WindFlagController : MonoBehaviour
    {
        [SerializeField] private WindController _wind;
        [SerializeField] private Transform _flagVisual;
        [SerializeField] private Vector3 _localDirectionAxis = Vector3.forward;
        [SerializeField] private float _angleOffset;
        [SerializeField, Min(0f)] private float _turnSpeed = 120f;
        [SerializeField, Min(0.01f)] private float _smoothingTime = 0.25f;
        [SerializeField, Range(0f, 30f)] private float _minimumDirectionChange = 0.5f;
        private float _targetHeading;
        private float _heading;
        private float _headingVelocity;

        private void OnEnable()
        {
            if (_flagVisual == null) _flagVisual = transform;
            _heading = _targetHeading = WindHeading();
            _headingVelocity = 0f;
            ApplyRotation();
        }

        private float WindHeading() => _wind == null ? _targetHeading
            : Mathf.Atan2(_wind.Direction.x, _wind.Direction.z) * Mathf.Rad2Deg;

        private void LateUpdate()
        {
            if (_wind == null || !_wind.isActiveAndEnabled) return;
            float heading = WindHeading();
            // Порог применяется к последней принятой цели: медленные изменения накапливаются.
            if (Mathf.Abs(Mathf.DeltaAngle(_targetHeading, heading)) >= _minimumDirectionChange)
                _targetHeading = heading;
            _heading = Mathf.SmoothDampAngle(_heading, _targetHeading, ref _headingVelocity,
                Mathf.Max(0.01f, _smoothingTime), Mathf.Max(0f, _turnSpeed), Time.deltaTime);
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            Vector3 axis = _localDirectionAxis.sqrMagnitude > 0.0001f ? _localDirectionAxis.normalized : Vector3.forward;
            _flagVisual.rotation = Quaternion.AngleAxis(_heading + _angleOffset, Vector3.up)
                * Quaternion.FromToRotation(axis, Vector3.forward);
        }
    }
}
