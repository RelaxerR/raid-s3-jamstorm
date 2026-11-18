using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.Serialization;

namespace SecondCatch.Boat
{
    /// <summary>Проецирует силу паруса на корпус, объединяет греблю, инерцию и поворот рулём.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class BoatController : MonoBehaviour
    {
        [SerializeField] private WindController _wind;
        [SerializeField] private WaveController _waves;
        private Vector3 _waveVelocity;
        public bool IsRespawning { get; internal set; }
        public Vector3 Velocity => HullDirection * _longitudinalSpeed + _waveVelocity;
        [SerializeField] private HelmStation _helm;
        [SerializeField] private SailStation _sail;
        [SerializeField, Min(0f)] private float _maxSpeed = 4f;
        [SerializeField, Min(0f)] private float _sailSpeedMultiplier = 1f;
        [FormerlySerializedAs("_speedWithoutSail")]
        [SerializeField, Min(0f)] private float _rowingSpeed = 0.6f;
        [SerializeField, Min(0f)] private float _reverseRowingSpeed = 0.2f;
        [SerializeField, Min(0f)] private float _accelerationTime = 2f;
        [SerializeField, Min(0f)] private float _decelerationTime = 24f;
        [SerializeField, Min(0f)] private float _turnSpeed = 12f;
        [SerializeField, Min(0.01f)] private float _fullSteeringSpeed = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _reverseSteeringMultiplier = 0.35f;
        [SerializeField, Range(0f, 180f)] private float _maxSailAngle = 180f;
        [SerializeField, Min(0.01f)] private float _sailEfficiencyExponent = 1f;
        [SerializeField, Range(0f, 1f)] private float _minimumSailEfficiency;
        [SerializeField, Range(0f, 1f)] private float _sailDirectionInfluence = 0.35f;
        [SerializeField, HideInInspector] private bool _healthSettingsMigrated;
        [SerializeField, HideInInspector] private float _maxHitPoints = 100f;
        [SerializeField, HideInInspector] private float _hitPoints = 100f;
        private BoatHealth _health;
        public BoatHealth Health => _health != null ? _health : GetComponent<BoatHealth>();
        // Старые сцены передают сохранённые настройки станции при загрузке.
        [SerializeField, HideInInspector] private bool _sailRaised;
        [SerializeField, HideInInspector] private float _sailAngle;
        [SerializeField, HideInInspector] private bool _stationStateMigrated;
        private float _longitudinalSpeed;

        public WindController Wind => _wind != null && _wind.isActiveAndEnabled ? _wind : null;
        public float CurrentSpeed => Velocity.magnitude;
        public float LongitudinalSpeed => _longitudinalSpeed;
        public bool IsSailing => SailSpeedContribution > 0f || Mathf.Abs(RowingSpeedContribution) > 0f;
        public SailPosition SailPosition => _sail != null ? _sail.Position : SecondCatch.Stations.SailPosition.Folded;
        public bool SailRaised => SailPosition != SecondCatch.Stations.SailPosition.Folded;
        public float SailAngle => _sail != null ? _sail.Angle : 0f;
        public float SailDeployment => _sail != null ? _sail.Deployment : 0f;
        public float MaxSailAngle => _maxSailAngle;
        public Vector3 HullDirection => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        public Vector3 SailDirection => Quaternion.AngleAxis(SailAngle, Vector3.up) * HullDirection;
        public Vector3 MovementDirection => Velocity.normalized;
        public bool HelmActive => _helm != null && _helm.Active;
        public bool IsRowing => HelmActive && Mathf.Abs(_helm.RowingInput) > 0f;
        public float Steering => _helm != null ? _helm.Steering : 0f;
        public float RudderAngle => _helm != null ? _helm.RudderAngle : 0f;
        private float WindCapture => Wind == null ? 0f : Mathf.Lerp(_minimumSailEfficiency, 1f,
            Mathf.Pow(Mathf.Clamp01(Vector3.Dot(Wind.Direction, SailDirection)),
                Mathf.Max(0.01f, _sailEfficiencyExponent)));
        private Vector3 ForceDirection => Wind == null ? Vector3.zero
            : Vector3.Lerp(Wind.Direction, SailDirection, _sailDirectionInfluence).normalized;
        public float SailEfficiency => WindCapture * Mathf.Max(0f, Vector3.Dot(ForceDirection, HullDirection));
        public Vector3 SailForce => Wind == null || _sail == null ? Vector3.zero
            : ForceDirection * (Wind.Speed * Wind.Strength * Wind.SailInfluence * Mathf.Max(0f, _sailSpeedMultiplier)
                * WindCapture * _sail.ThrustMultiplier);
        public float SailSpeedContribution => Mathf.Max(0f, Vector3.Dot(SailForce, HullDirection));
        public float ForwardRowingContribution => IsRowing ? Mathf.Max(0f, _helm.RowingInput) * _rowingSpeed : 0f;
        public float ReverseRowingContribution => IsRowing ? Mathf.Max(0f, -_helm.RowingInput) * _reverseRowingSpeed : 0f;
        public float RowingSpeedContribution => ForwardRowingContribution - ReverseRowingContribution;
        public float MaxHitPoints => Health != null ? Health.Maximum : Mathf.Max(1f, _maxHitPoints);
        public float HitPoints => Health != null ? Health.Current : Mathf.Clamp(_hitPoints, 0f, MaxHitPoints);

        private void Awake()
        {
            _health = GetComponent<BoatHealth>();
            if (_health == null) _health = gameObject.AddComponent<BoatHealth>();
            if (GetComponent<BoatRespawnController>() == null) gameObject.AddComponent<BoatRespawnController>();
            if (!_healthSettingsMigrated)
            {
                _health.ImportLegacy(_maxHitPoints, _hitPoints);
                _healthSettingsMigrated = true;
            }
            if (_helm == null) _helm = GetComponentInChildren<HelmStation>(true);
            if (_sail == null) _sail = GetComponentInChildren<SailStation>(true);
            if (!_stationStateMigrated && _sail != null)
            {
                if (_sailRaised) _sail.SetPosition(SecondCatch.Stations.SailPosition.Full);
                if (_sailAngle != 0f) _sail.SetAngle(_sailAngle);
                _stationStateMigrated = true;
            }
        }

        public void SetHitPoints(float hitPoints)
        {
            if (Health != null) Health.SetCurrent(hitPoints);
            else _hitPoints = Mathf.Clamp(hitPoints, 0f, MaxHitPoints);
        }
        public void TakeDamage(float amount) { if (Health != null) Health.TakeDamage(amount); }
        public void SetHelmActive(bool active) { if (_helm != null) _helm.SetActive(active && !IsRespawning); }
        public void SetSteering(float steering) { if (_helm != null) _helm.SetSteering(IsRespawning ? 0f : steering); }
        public void SetRowing(bool rowing) { if (_helm != null) _helm.SetRowing(rowing && !IsRespawning ? 1f : 0f); }
        public void SetSailRaised(bool raised)
        {
            if (_sail != null) _sail.SetPosition(raised ? SecondCatch.Stations.SailPosition.Full : SecondCatch.Stations.SailPosition.Folded);
        }
        public void SetSailAngle(float angle) { if (_sail != null) _sail.SetAngle(angle); }

        private void Update()
        {
            if (IsRespawning) return;
            float targetSpeed = Mathf.Clamp(SailSpeedContribution + RowingSpeedContribution,
                -Mathf.Max(0f, _reverseRowingSpeed), Mathf.Max(0f, _maxSpeed));
            // При смене знака сначала гасим старую скорость, затем разгоняемся в обратную сторону.
            bool reversing = _longitudinalSpeed * targetSpeed < 0f;
            bool accelerating = !reversing && Mathf.Abs(targetSpeed) > Mathf.Abs(_longitudinalSpeed);
            float responseTime = accelerating ? _accelerationTime : _decelerationTime;
            float intermediateTarget = reversing ? 0f : targetSpeed;
            _longitudinalSpeed = responseTime <= 0f ? intermediateTarget
                : Mathf.MoveTowards(_longitudinalSpeed, intermediateTarget,
                    Mathf.Max(0.01f, _maxSpeed) / responseTime * Time.deltaTime);

            float steering = _helm != null ? _helm.CourseCorrection : 0f;
            float authority = Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.01f, _fullSteeringSpeed));
            float reverseFactor = _longitudinalSpeed < 0f ? _reverseSteeringMultiplier : 1f;
            transform.Rotate(Vector3.up, steering * _turnSpeed * authority * reverseFactor * Time.deltaTime, Space.World);
            // Тяга сохраняет продольную инерцию, море добавляет независимый дрейф к острову.
            _waveVelocity *= Mathf.Exp(-Time.deltaTime);
            if (_waves != null && _waves.isActiveAndEnabled)
            {
                Vector3 inward = _waves.DirectionToCenter(transform.position);
                _waveVelocity += _waves.ReturnAcceleration(transform.position) * Time.deltaTime;
                float outwardSpeed = Mathf.Max(0f, -Vector3.Dot(Velocity, inward));
                float reduction = outwardSpeed * (1f - Mathf.Exp(-_waves.ResistanceAt(transform.position) * Time.deltaTime));
                float alignment = Vector3.Dot(HullDirection, inward);
                float longitudinalReduction = reduction * alignment;
                _longitudinalSpeed += longitudinalReduction;
                _waveVelocity += inward * reduction - HullDirection * longitudinalReduction;
            }
            transform.position += Velocity * Time.deltaTime;
        }

        /// <summary>Сбрасывает инерцию и команды руля при возврате в порт.</summary>
        public void ResetMotion()
        {
            if (_helm != null) _helm.ResetMotion();
            _longitudinalSpeed = 0f;
            _waveVelocity = Vector3.zero;
        }

        private void OnDisable()
        {
            ResetMotion();
        }
    }
}
