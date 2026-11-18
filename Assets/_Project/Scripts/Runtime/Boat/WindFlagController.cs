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
            Vector3 axis = HorizontalModelAxis();
            Vector3 worldAxis = _flagVisual.rotation * axis;
            worldAxis.y = 0f;
            _heading = _targetHeading = worldAxis.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(worldAxis.x, worldAxis.z) * Mathf.Rad2Deg - _angleOffset : 0f;
            if (TryGetWindHeading(out float heading)) _heading = _targetHeading = heading;
            _headingVelocity = 0f;
            ApplyRotation();
        }

        private bool TryGetWindHeading(out float heading)
        {
            heading = _targetHeading;
            if (_wind == null || !_wind.isActiveAndEnabled
                || !_wind.TryGetHorizontalDirection(out Vector3 direction)) return false;
            // После удаления Y направление нормализуется самим источником; используем только мировой yaw.
            heading = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return true;
        }

        private void LateUpdate()
        {
            if (TryGetWindHeading(out float heading)
                && Mathf.Abs(Mathf.DeltaAngle(_targetHeading, heading)) >= _minimumDirectionChange)
                _targetHeading = heading;
            if (Time.deltaTime > 0f)
                _heading = Mathf.SmoothDampAngle(_heading, _targetHeading, ref _headingVelocity,
                    Mathf.Max(0.01f, _smoothingTime), Mathf.Max(0f, _turnSpeed), Time.deltaTime);
            // Даже без нового ветра компенсируем вращение родителя по последнему корректному направлению.
            ApplyRotation();
        }

        private Vector3 HorizontalModelAxis()
        {
            Vector3 axis = _localDirectionAxis;
            axis.y = 0f;
            return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
        }

        private void ApplyRotation()
        {
            Vector3 axis = HorizontalModelAxis();
            float modelHeading = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
            // Меняем только вращение вокруг мировой Y; локальная позиция крепления и масштаб не записываются.
            _flagVisual.rotation = Quaternion.AngleAxis(_heading + _angleOffset - modelHeading, Vector3.up);
        }
    }
}
