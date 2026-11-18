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
        [SerializeField, Min(1f)] private float _maxHitPoints = 100f;
        [SerializeField, Min(0f)] private float _hitPoints = 100f;
        // Старые сцены передают сохранённые настройки станции при загрузке.
        [SerializeField, HideInInspector] private bool _sailRaised;
        [SerializeField, HideInInspector] private float _sailAngle;
        [SerializeField, HideInInspector] private bool _stationStateMigrated;
        private float _longitudinalSpeed;

        public WindController Wind => _wind != null && _wind.isActiveAndEnabled ? _wind : null;
        public float CurrentSpeed => Mathf.Abs(_longitudinalSpeed);
        public float LongitudinalSpeed => _longitudinalSpeed;
        public bool IsSailing => SailSpeedContribution > 0f || Mathf.Abs(RowingSpeedContribution) > 0f;
        public SailPosition SailPosition => _sail != null ? _sail.Position : SecondCatch.Stations.SailPosition.Folded;
        public bool SailRaised => SailPosition != SecondCatch.Stations.SailPosition.Folded;
        public float SailAngle => _sail != null ? _sail.Angle : 0f;
        public float SailDeployment => _sail != null ? _sail.Deployment : 0f;
        public float MaxSailAngle => _maxSailAngle;
        public Vector3 HullDirection => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        public Vector3 SailDirection => Quaternion.AngleAxis(SailAngle, Vector3.up) * HullDirection;
        public Vector3 MovementDirection => HullDirection * Mathf.Sign(_longitudinalSpeed);
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
        public float MaxHitPoints => Mathf.Max(1f, _maxHitPoints);
        public float HitPoints => Mathf.Clamp(_hitPoints, 0f, MaxHitPoints);

        private void Awake()
        {
            if (_helm == null) _helm = GetComponentInChildren<HelmStation>(true);
            if (_sail == null) _sail = GetComponentInChildren<SailStation>(true);
            if (!_stationStateMigrated && _sail != null)
            {
                if (_sailRaised) _sail.SetPosition(SecondCatch.Stations.SailPosition.Full);
                if (_sailAngle != 0f) _sail.SetAngle(_sailAngle);
                _stationStateMigrated = true;
            }
        }

        public void SetHitPoints(float hitPoints) => _hitPoints = Mathf.Clamp(hitPoints, 0f, MaxHitPoints);
        public void SetHelmActive(bool active) { if (_helm != null) _helm.SetActive(active); }
        public void SetSteering(float steering) { if (_helm != null) _helm.SetSteering(steering); }
        public void SetRowing(bool rowing) { if (_helm != null) _helm.SetRowing(rowing ? 1f : 0f); }
        public void SetSailRaised(bool raised)
        {
            if (_sail != null) _sail.SetPosition(raised ? SecondCatch.Stations.SailPosition.Full : SecondCatch.Stations.SailPosition.Folded);
        }
        public void SetSailAngle(float angle) { if (_sail != null) _sail.SetAngle(angle); }

        private void Update()
        {
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
            // Киль поглощает боковую силу, движение остаётся продольным без разворота корпуса при S.
            transform.position += HullDirection * (_longitudinalSpeed * Time.deltaTime);
        }

        private void OnDisable()
        {
            SetHelmActive(false);
            _longitudinalSpeed = 0f;
        }
    }
}
