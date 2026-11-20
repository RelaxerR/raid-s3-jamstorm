using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Хранит команды гребли и физический угол руля, синхронизирует балку.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class HelmStation : StationInteraction
    {
        [SerializeField] private Transform _rudderVisual;
        [SerializeField, Range(1f, 80f)] private float _maxRudderAngle = 35f;
        [SerializeField, Min(0f)] private float _deflectionSpeed = 60f;
        [SerializeField, Min(0f)] private float _returnSpeed = 45f;
        [SerializeField, Min(0.01f)] private float _deflectionAccelerationTime = 0.5f;
        [SerializeField, Min(0.01f)] private float _deflectionBrakingTime = 0.7f;
        [SerializeField, Min(0.01f)] private float _rowingAccelerationTime = 1.2f;
        [SerializeField, Min(0.01f)] private float _rowingBrakingTime = 1.8f;
        private float _rowingCommand;
        public float RudderAngularVelocity { get; private set; }
        private Quaternion _initialRotation;
        private float _steeringInput;
        private bool _active;

        public override PlayerControlMode ControlMode => PlayerControlMode.Helm;
        public float RudderAngle { get; private set; }
        public float RowingInput { get; private set; }
        public bool Active => _active && isActiveAndEnabled;
        public float Steering => RudderAngle / Mathf.Max(1f, _maxRudderAngle);
        public float CourseCorrection => Steering;
        public bool RowingAvailable => Boat != null && Boat.RowingAvailable;

        private void Awake()
        {
            if (_rudderVisual != null) _initialRotation = _rudderVisual.localRotation;
        }

        protected override void OnEntered() => SetActive(true);
        protected override void OnExited() => SetActive(false);

        public void SetActive(bool active)
        {
            _active = active;
            _steeringInput = 0f;
            _rowingCommand = 0f;
        }

        public void SetSteering(float input) => _steeringInput = Active ? Mathf.Clamp(input, -1f, 1f) : 0f;
        public void SetRowing(float input) => _rowingCommand = Active ? Mathf.Clamp(input, -1f, 1f) : 0f;

        public override void HandleInput(Vector2 input)
        {
            SetSteering(input.x);
            SetRowing(input.y);
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            if (Boat == null || !Boat.isActiveAndEnabled) SetActive(false);
            if (dt <= 0f) return;
            float input = Active ? _steeringInput : 0f;
            bool returning = Mathf.Abs(input) < 0.001f;
            float targetVelocity = returning
                ? -Mathf.Sign(RudderAngle) * Mathf.Min(_returnSpeed, Mathf.Abs(RudderAngle) / dt)
                : input * _deflectionSpeed;
            bool accelerating = !returning && RudderAngularVelocity * targetVelocity >= 0f
                && Mathf.Abs(targetVelocity) > Mathf.Abs(RudderAngularVelocity);
            float time = accelerating ? _deflectionAccelerationTime : _deflectionBrakingTime;
            RudderAngularVelocity = Mathf.MoveTowards(RudderAngularVelocity, targetVelocity,
                Mathf.Max(_deflectionSpeed, _returnSpeed) / Mathf.Max(0.01f, time) * dt);
            float previousAngle = RudderAngle;
            float requested = RudderAngle + RudderAngularVelocity * dt;
            RudderAngle = Mathf.Clamp(requested, -_maxRudderAngle, _maxRudderAngle);
            if (!Mathf.Approximately(requested, RudderAngle)) RudderAngularVelocity = 0f;
            if (returning && previousAngle * RudderAngle <= 0f)
            {
                RudderAngle = 0f;
                RudderAngularVelocity = 0f;
            }
            float rowingTarget = Active ? _rowingCommand : 0f;
            bool buildingThrust = rowingTarget * RowingInput >= 0f && Mathf.Abs(rowingTarget) > Mathf.Abs(RowingInput);
            float rowingTime = buildingThrust ? _rowingAccelerationTime : _rowingBrakingTime;
            float intermediate = rowingTarget * RowingInput < 0f ? 0f : rowingTarget;
            RowingInput = Mathf.MoveTowards(RowingInput, intermediate, dt / Mathf.Max(0.01f, rowingTime));
            UpdateVisual();
        }

        /// <summary>Мгновенно сбрасывает ввод, угол руля и визуал при респавне.</summary>
        public void ResetMotion()
        {
            SetActive(false);
            RudderAngle = 0f;
            RudderAngularVelocity = 0f;
            RowingInput = 0f;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_rudderVisual != null)
                _rudderVisual.localRotation = Quaternion.AngleAxis(RudderAngle, Vector3.up) * _initialRotation;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetActive(false);
            RudderAngle = 0f;
            RudderAngularVelocity = 0f;
            RowingInput = 0f;
            UpdateVisual();
        }
    }
}
