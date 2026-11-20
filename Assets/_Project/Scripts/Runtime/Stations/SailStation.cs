using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Три сохраняемых положения паруса.</summary>
    public enum SailPosition { Folded, Partial, Full }

    /// <summary>Хранит положение и угол паруса, плавно меняет площадь полотна.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class SailStation : StationInteraction
    {
        [SerializeField] private Transform _sailVisual;
        [SerializeField, Range(0.01f, 1f)] private float _loweredHeightRatio = 0.15f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 45f;
        [SerializeField] private SailPosition _position;
        [SerializeField, Range(-180f, 180f)] private float _angle;
        [SerializeField, Range(0f, 1f)] private float _partialMultiplier = 0.5f;
        [SerializeField, Min(0.01f)] private float _stateChangeSpeed = 1f;
        [SerializeField, Min(0.01f)] private float _stepRepeatInterval = 0.4f;
        [SerializeField, Min(0.01f)] private float _rotationAccelerationTime = 0.6f;
        [SerializeField, Min(0.01f)] private float _rotationBrakingTime = 0.9f;
        [SerializeField, Min(0f)] private float _rotationResistance = 0.2f;
        [SerializeField] private Vector2 _angleRange = new Vector2(-180f, 180f);
        private float _rotationInput;
        public float AngularVelocity { get; private set; }
        private Vector3 _raisedScale;
        private Quaternion _initialRotation;
        private int _previousStep;
        private float _repeatRemaining;
        private float _deployment;

        public override PlayerControlMode ControlMode => PlayerControlMode.Sail;
        public SailPosition Position => _position;
        public float Angle => _angle;
        public float Deployment => _deployment;
        public float ThrustMultiplier => _deployment;
        private float TargetDeployment => _position == SailPosition.Full ? 1f
            : _position == SailPosition.Partial ? _partialMultiplier : 0f;

        private void Awake()
        {
            _deployment = TargetDeployment;
            if (_sailVisual != null)
            {
                _raisedScale = _sailVisual.localScale;
                _initialRotation = _sailVisual.localRotation;
            }
            UpdateVisual();
        }

        public void SetPosition(SailPosition position) => _position = (SailPosition)Mathf.Clamp((int)position, 0, 2);
        public void SetAngle(float angle)
        {
            float limit = Boat != null ? Boat.MaxSailAngle : 180f;
            _angle = Mathf.Clamp(angle, Mathf.Clamp(Mathf.Min(_angleRange.x, _angleRange.y), -limit, limit),
                Mathf.Clamp(Mathf.Max(_angleRange.x, _angleRange.y), -limit, limit));
        }

        public override void HandleInput(Vector2 input)
        {
            int step = input.y > 0f ? 1 : input.y < 0f ? -1 : 0;
            _repeatRemaining -= Time.deltaTime;
            if (step != 0 && (step != _previousStep || _repeatRemaining <= 0f))
            {
                SetPosition((SailPosition)((int)_position + step));
                _repeatRemaining = Mathf.Max(0.01f, _stepRepeatInterval);
            }
            _previousStep = step;
            _rotationInput = Mathf.Clamp(input.x, -1f, 1f);
        }

        protected override void OnExited()
        {
            _previousStep = 0;
            _rotationInput = 0f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _rotationInput = 0f;
            AngularVelocity = 0f;
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            float input = Operator != null && Boat != null && !Boat.IsRespawning ? _rotationInput : 0f;
            bool accelerating = input != 0f && AngularVelocity * input >= 0f;
            float time = accelerating ? _rotationAccelerationTime : _rotationBrakingTime;
            // Угловая скорость сохраняется при смене ступени и плавно затухает после выхода.
            float targetVelocity = input * _rotationSpeed;
            AngularVelocity = Mathf.MoveTowards(AngularVelocity, targetVelocity,
                _rotationSpeed / Mathf.Max(0.01f, time) * dt);
            if (input == 0f) AngularVelocity *= Mathf.Exp(-_rotationResistance * dt);
            float requested = _angle + AngularVelocity * dt;
            SetAngle(requested);
            if (!Mathf.Approximately(requested, _angle)) AngularVelocity = 0f;
            _deployment = Mathf.MoveTowards(_deployment, TargetDeployment,
                Mathf.Max(0.01f, _stateChangeSpeed) * dt);
        }

        private void LateUpdate() => UpdateVisual();

        private void UpdateVisual()
        {
            if (_sailVisual == null) return;
            Vector3 scale = _raisedScale;
            scale.y *= Mathf.Lerp(_loweredHeightRatio, 1f, _deployment);
            _sailVisual.localScale = scale;
            _sailVisual.localRotation = Quaternion.AngleAxis(_angle, Vector3.up) * _initialRotation;
        }
    }
}
