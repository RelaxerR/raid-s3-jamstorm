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
        private Quaternion _modelRotation;
        private float _targetHeading;
        private float _heading;
        private float _headingVelocity;

        private void OnEnable()
        {
            if (_flagVisual == null) _flagVisual = transform;
            Vector3 axis = ModelAxis();
            // Сохраняем авторский наклон модели и совмещаем её указанную ось с локальной +Z.
            Quaternion authored = _flagVisual.localRotation;
            _modelRotation = Quaternion.FromToRotation(authored * axis, Vector3.forward) * authored;
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

        private void LateUpdate() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            if (TryGetWindHeading(out float heading)
                && Mathf.Abs(Mathf.DeltaAngle(_targetHeading, heading)) >= _minimumDirectionChange)
                _targetHeading = heading;
            if (dt > 0f)
                _heading = Mathf.SmoothDampAngle(_heading, _targetHeading, ref _headingVelocity,
                    Mathf.Max(0.01f, _smoothingTime), Mathf.Max(0f, _turnSpeed), dt);
            // Даже без нового ветра компенсируем вращение родителя по последнему корректному направлению.
            ApplyRotation();
        }

        private Vector3 ModelAxis() => _localDirectionAxis.sqrMagnitude > 0.0001f
            ? _localDirectionAxis.normalized : Vector3.forward;

        private void ApplyRotation()
        {
            // Позиция крепления остаётся дочерней лодке, мировое направление компенсирует родителя.
            _flagVisual.rotation = Quaternion.AngleAxis(_heading + _angleOffset, Vector3.up) * _modelRotation;
        }
    }
}
