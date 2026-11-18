using UnityEngine;
using UnityEngine.Serialization;

namespace SecondCatch.Boat
{
    /// <summary>Объединяет независимую тягу паруса и греблю, применяет скорость и курс лодки.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class BoatController : MonoBehaviour
    {
        [SerializeField] private WindController _wind;
        [SerializeField, Min(0f)] private float _maxSpeed = 4f;
        [SerializeField, Min(0f)] private float _sailSpeedMultiplier = 1f;
        [FormerlySerializedAs("_speedWithoutSail")]
        [SerializeField, Min(0f)] private float _rowingSpeed = 0.6f;
        [SerializeField, Min(0f)] private float _accelerationTime = 2f;
        [SerializeField, Min(0f)] private float _decelerationTime = 1.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 12f;
        [SerializeField, Min(0f)] private float _steeringResponse = 2f;
        [SerializeField] private bool _sailRaised;
        [SerializeField, Range(-180f, 180f)] private float _sailAngle;
        [SerializeField, Range(0f, 180f)] private float _maxSailAngle = 180f;
        [SerializeField, Min(0.01f)] private float _sailEfficiencyExponent = 1f;
        [SerializeField, Range(0f, 1f)] private float _minimumSailEfficiency;
        [SerializeField, Range(0f, 1f)] private float _sailDirectionInfluence = 0.35f;
        [SerializeField, Min(1f)] private float _maxHitPoints = 100f;
        [SerializeField, Min(0f)] private float _hitPoints = 100f;

        private bool _helmActive;
        private bool _rowing;
        private float _steeringInput;
        private float _steering;
        private Vector3 _movementDirection;

        public WindController Wind => _wind != null && _wind.isActiveAndEnabled ? _wind : null;
        public float CurrentSpeed { get; private set; }
        public bool IsSailing => SailSpeedContribution + RowingSpeedContribution > 0f;
        public bool SailRaised => _sailRaised;
        public float SailAngle => Mathf.Clamp(_sailAngle, -_maxSailAngle, _maxSailAngle);
        public float MaxSailAngle => _maxSailAngle;
        public Vector3 HullDirection => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        public Vector3 SailDirection => Quaternion.AngleAxis(SailAngle, Vector3.up) * HullDirection;
        public Vector3 MovementDirection => _movementDirection;
        public bool HelmActive => _helmActive;
        public bool IsRowing => _helmActive && _rowing;
        public float SailEfficiency => Wind == null ? 0f : Mathf.Lerp(_minimumSailEfficiency, 1f,
            Mathf.Pow(Mathf.Clamp01((Vector3.Dot(Wind.Direction, SailDirection) + 1f) * 0.5f),
                Mathf.Max(0.01f, _sailEfficiencyExponent)));
        public float SailSpeedContribution => _sailRaised && Wind != null
            ? Wind.Speed * Wind.Strength * Mathf.Max(0f, _sailSpeedMultiplier) * SailEfficiency : 0f;
        public float RowingSpeedContribution => IsRowing ? Mathf.Max(0f, _rowingSpeed) : 0f;
        public float MaxHitPoints => Mathf.Max(1f, _maxHitPoints);
        public float HitPoints => Mathf.Clamp(_hitPoints, 0f, MaxHitPoints);
        public float Steering => _steering;

        /// <summary>Принимает состояние прочности от будущего источника урона или ремонта.</summary>
        public void SetHitPoints(float hitPoints) => _hitPoints = Mathf.Clamp(hitPoints, 0f, MaxHitPoints);

        /// <summary>Гребля и поворот доступны только занятому рулю; выход сразу убирает обе команды.</summary>
        public void SetHelmActive(bool active)
        {
            _helmActive = active;
            _rowing = false;
            _steeringInput = _steering = 0f;
        }

        public void SetSteering(float steering) => _steeringInput = _helmActive ? Mathf.Clamp(steering, -1f, 1f) : 0f;
        public void SetRowing(bool rowing) => _rowing = _helmActive && rowing;
        public void SetSailRaised(bool raised) => _sailRaised = raised;
        public void SetSailAngle(float angle) => _sailAngle = Mathf.Clamp(angle, -_maxSailAngle, _maxSailAngle);

        private void Update()
        {
            _steering = Mathf.MoveTowards(_steering, _helmActive ? _steeringInput : 0f,
                Mathf.Max(0f, _steeringResponse) * Time.deltaTime);
            transform.Rotate(Vector3.up, _steering * Mathf.Max(0f, _turnSpeed) * Time.deltaTime, Space.World);

            float sailSpeed = SailSpeedContribution;
            float rowingSpeed = RowingSpeedContribution;
            float targetSpeed = Mathf.Clamp(sailSpeed + rowingSpeed, 0f, _maxSpeed);
            float responseTime = targetSpeed > CurrentSpeed ? _accelerationTime : _decelerationTime;
            CurrentSpeed = responseTime <= 0f ? targetSpeed : Mathf.MoveTowards(CurrentSpeed, targetSpeed,
                Mathf.Max(0f, _maxSpeed) / responseTime * Time.deltaTime);

            // Корпус ограничивает боковую тягу паруса, но не заставляет его совпадать с курсом.
            Vector3 sailTravel = Vector3.Slerp(HullDirection, SailDirection, _sailDirectionInfluence).normalized;
            Vector3 propulsion = sailTravel * sailSpeed + HullDirection * rowingSpeed;
            if (propulsion.sqrMagnitude > 0.0001f) _movementDirection = propulsion.normalized;
            // Без тяги сохраняется направление инерции до плавной остановки.
            transform.position += _movementDirection * (CurrentSpeed * Time.deltaTime);
        }

        private void OnDisable()
        {
            SetHelmActive(false);
            CurrentSpeed = 0f;
            _movementDirection = Vector3.zero;
        }
    }
}
