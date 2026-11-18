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
        private Vector3 _raisedScale;
        private Quaternion _initialRotation;
        private int _previousStep;
        private float _repeatRemaining;
        private float _deployment;

        public override PlayerControlMode ControlMode => PlayerControlMode.Sail;
        public SailPosition Position => _position;
        public float Angle => _angle;
        public float Deployment => _deployment;
        public float ThrustMultiplier => _position == SailPosition.Folded ? 0f : _deployment;
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
            _angle = Mathf.Clamp(angle, -limit, limit);
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
            SetAngle(_angle + input.x * _rotationSpeed * Time.deltaTime);
        }

        protected override void OnExited() => _previousStep = 0;

        private void Update()
        {
            _deployment = Mathf.MoveTowards(_deployment, TargetDeployment,
                Mathf.Max(0.01f, _stateChangeSpeed) * Time.deltaTime);
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
