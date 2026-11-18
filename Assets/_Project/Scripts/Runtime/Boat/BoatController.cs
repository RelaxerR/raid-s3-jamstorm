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
        private Vector3 _propulsionVelocity;
        private Vector3 _lastHullDirection = Vector3.forward;
        public bool IsRespawning { get; internal set; }
        public Vector3 TotalBoatVelocity => _propulsionVelocity + WaveDrift;
        public Vector3 Velocity => TotalBoatVelocity;
        public Vector3 WaveDrift => _waveVelocity;
        [SerializeField, Min(0f)] private float _sailThrustEpsilon = 0.001f;
        public float SailThrustEpsilon => Mathf.Max(0f, _sailThrustEpsilon);
        public float SailForwardThrust => SailSpeedContribution;
        public Vector3 SailThrust => SailForce;
        public bool RowingAvailable => !IsRespawning && SailForwardThrust <= SailThrustEpsilon;
        public float RowingInput => _helm != null ? _helm.RowingInput : 0f;
        public float RowingThrust => RowingSpeedContribution;
        public string RowingAvailabilityText => RowingAvailable ? "Гребля доступна"
            : IsRespawning ? "Гребля недоступна: возврат в порт" : "Гребля недоступна: работает парус";
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
        [SerializeField, Min(1f)] private float _mass = 1000f;
        [SerializeField, Min(1f)] private float _referenceMass = 1000f;
        [SerializeField, Min(0.01f)] private float _turnInertiaMultiplier = 1.5f;
        [SerializeField, Min(0.01f)] private float _turnAccelerationTime = 1.8f;
        [SerializeField, Min(0.01f)] private float _turnDecelerationTime = 2.5f;
        [SerializeField, Min(0f)] private float _movementResistance = 0.06f;
        [SerializeField, Min(0f)] private float _lateralResistance = 2.5f;
        [SerializeField, Min(0f)] private float _waveDriftResistance = 1f;
        private float _longitudinalSpeed;
        private float _yawSpeed;
        public float Mass => Mathf.Max(1f, _mass);
        public float YawSpeed => _yawSpeed;
        private float MassRatio => Mass / Mathf.Max(1f, _referenceMass);

        public WindController Wind => _wind != null && _wind.isActiveAndEnabled ? _wind : null;
        public float CurrentSpeed => Velocity.magnitude;
        public float LongitudinalSpeed => _longitudinalSpeed;
        public bool IsSailing => SailSpeedContribution > 0f || Mathf.Abs(RowingSpeedContribution) > 0f;
        public SailPosition SailPosition => _sail != null ? _sail.Position : SecondCatch.Stations.SailPosition.Folded;
        public bool SailRaised => SailPosition != SecondCatch.Stations.SailPosition.Folded;
        public float SailAngle => _sail != null ? _sail.Angle : 0f;
        public float SailDeployment => _sail != null ? _sail.Deployment : 0f;
        public float MaxSailAngle => _maxSailAngle;
        public Vector3 HullDirection
        {
            get
            {
                Vector3 direction = transform.forward;
                direction.y = 0f;
                return direction.sqrMagnitude > 0.0001f ? direction.normalized : _lastHullDirection;
            }
        }
        public Vector3 SailDirection => Quaternion.AngleAxis(SailAngle, Vector3.up) * HullDirection;
        public Vector3 MovementDirection => Velocity.normalized;
        public bool HelmActive => _helm != null && _helm.Active;
        public bool IsRowing => RowingAvailable && Mathf.Abs(RowingInput) > 0.001f;
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
            float dt = Time.deltaTime;
            if (IsRespawning || dt <= 0f) return;
            // Состояния хода и волн независимы; визуальная поверхность не владеет скоростью лодки.
            AdvancePropulsion(dt);
            AdvanceYaw(dt);
            ApplyWaveField(dt);
            _lastHullDirection = HullDirection;
            _longitudinalSpeed = Vector3.Dot(_propulsionVelocity, _lastHullDirection);
            transform.position += Velocity * dt;
        }

        private void AdvancePropulsion(float dt)
        {
            Vector3 forward = HullDirection;
            float currentSpeed = Vector3.Dot(_propulsionVelocity, forward);
            Vector3 lateral = _propulsionVelocity - forward * currentSpeed;
            float targetSpeed = Mathf.Clamp(SailSpeedContribution + RowingSpeedContribution,
                -Mathf.Max(0f, _reverseRowingSpeed), Mathf.Max(0f, _maxSpeed));
            bool reversing = currentSpeed * targetSpeed < 0f;
            bool accelerating = !reversing && Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed);
            float responseTime = accelerating ? _accelerationTime : _decelerationTime;
            float intermediateTarget = reversing ? 0f : targetSpeed;
            float speed = responseTime <= 0f ? intermediateTarget : Mathf.MoveTowards(currentSpeed,
                intermediateTarget, Mathf.Max(0.01f, _maxSpeed) / (responseTime * MassRatio) * dt);
            speed *= Mathf.Exp(-Mathf.Max(0f, _movementResistance) / MassRatio * dt);
            lateral *= Mathf.Exp(-Mathf.Max(0f, _lateralResistance) / MassRatio * dt);
            // Сохраняем рассчитанный разгон один раз. Дрейф не входит в обратную связь тяги.
            _propulsionVelocity = forward * speed + lateral;
            _longitudinalSpeed = speed;
        }

        private void AdvanceYaw(float dt)
        {
            float steering = _helm != null ? _helm.CourseCorrection : 0f;
            float authority = Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.01f, _fullSteeringSpeed));
            float reverseFactor = _longitudinalSpeed < 0f ? _reverseSteeringMultiplier : 1f;
            float inertia = Mathf.Max(0.01f, _turnInertiaMultiplier * MassRatio);
            float maximumYaw = Mathf.Max(0f, _turnSpeed) / Mathf.Sqrt(inertia);
            float targetYaw = steering * maximumYaw * authority * reverseFactor;
            bool buildingTurn = _yawSpeed * targetYaw >= 0f && Mathf.Abs(targetYaw) > Mathf.Abs(_yawSpeed);
            float turnTime = buildingTurn ? _turnAccelerationTime : _turnDecelerationTime;
            _yawSpeed = Mathf.MoveTowards(_yawSpeed, targetYaw,
                Mathf.Max(0.01f, maximumYaw) / (Mathf.Max(0.01f, turnTime) * inertia) * dt);
            // Поворачивается корпус; оба мировых вектора скорости сохраняют свой импульс.
            transform.Rotate(Vector3.up, _yawSpeed * dt, Space.World);
        }

        private void ApplyWaveField(float dt)
        {
            _waveVelocity *= Mathf.Exp(-Mathf.Max(0f, _waveDriftResistance) / MassRatio * dt);
            if (_waves == null || !_waves.isActiveAndEnabled) return;
            Vector3 inward = _waves.DirectionToCenter(transform.position);
            _waveVelocity += _waves.ReturnAcceleration(transform.position) / MassRatio * dt;
            float outwardSpeed = Mathf.Max(0f, -Vector3.Dot(Velocity, inward));
            float reduction = outwardSpeed * (1f - Mathf.Exp(-_waves.ResistanceAt(transform.position) / MassRatio * dt));
            // Уменьшаем только наружный ход каждого источника; не переносим дрейф обратно в тягу.
            float propulsionOutward = Mathf.Max(0f, -Vector3.Dot(_propulsionVelocity, inward));
            float waveOutward = Mathf.Max(0f, -Vector3.Dot(_waveVelocity, inward));
            float totalOutward = propulsionOutward + waveOutward;
            if (totalOutward <= 0f) return;
            Vector3 correction = inward * reduction;
            _propulsionVelocity += correction * (propulsionOutward / totalOutward);
            _waveVelocity += correction * (waveOutward / totalOutward);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>По явному запросу печатает один снимок цепочки движения без постоянного лога.</summary>
        [ContextMenu("Диагностика движения лодки")]
        private void LogMotionSnapshot()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            Vector3 waveAcceleration = _waves != null && _waves.isActiveAndEnabled
                ? _waves.ReturnAcceleration(transform.position) / MassRatio : Vector3.zero;
            string bodyState = body == null ? "нет" : $"kinematic={body.isKinematic}, constraints={body.constraints}, sleeping={body.IsSleeping()}";
            Debug.Log($"Лодка: active={isActiveAndEnabled}, respawn={IsRespawning}, dt={Time.deltaTime:0.0000}, " +
                $"massRatio={MassRatio:0.000}, helm={HelmActive}, rowingInput={(_helm != null ? _helm.RowingInput : 0f):0.000}, " +
                $"rowing={RowingSpeedContribution:0.000}, sail={SailSpeedContribution:0.000}, " +
                $"deployment={SailDeployment:0.000}, projection={Vector3.Dot(SailForce, HullDirection):0.000}, " +
                $"waves={waveAcceleration}, propulsion={_propulsionVelocity}, drift={_waveVelocity}, " +
                $"velocity={Velocity}, body=[{bodyState}]", this);
        }
#endif

        /// <summary>Сбрасывает инерцию и команды руля при возврате в порт.</summary>
        public void ResetMotion()
        {
            if (_helm != null) _helm.ResetMotion();
            _longitudinalSpeed = 0f;
            _yawSpeed = 0f;
            _waveVelocity = Vector3.zero;
            _propulsionVelocity = Vector3.zero;
        }

        private void OnDisable()
        {
            ResetMotion();
        }
    }
}
