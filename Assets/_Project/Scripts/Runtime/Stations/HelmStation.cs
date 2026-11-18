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
        private Quaternion _initialRotation;
        private float _steeringInput;
        private bool _active;

        public override PlayerControlMode ControlMode => PlayerControlMode.Helm;
        public float RudderAngle { get; private set; }
        public float RowingInput { get; private set; }
        public bool Active => _active && isActiveAndEnabled;
        public float Steering => RudderAngle / Mathf.Max(1f, _maxRudderAngle);
        public float CourseCorrection => Active && Mathf.Abs(_steeringInput) > 0f ? Steering : 0f;

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
            RowingInput = 0f;
        }

        public void SetSteering(float input) => _steeringInput = Active ? Mathf.Clamp(input, -1f, 1f) : 0f;
        public void SetRowing(float input) => RowingInput = Active ? Mathf.Clamp(input, -1f, 1f) : 0f;

        public override void HandleInput(Vector2 input)
        {
            SetSteering(input.x);
            SetRowing(input.y);
        }

        private void Update()
        {
            if (Boat == null || !Boat.isActiveAndEnabled) SetActive(false);
            float target = Active ? _steeringInput * _maxRudderAngle : 0f;
            float speed = Mathf.Abs(target) > 0f ? _deflectionSpeed : _returnSpeed;
            RudderAngle = Mathf.MoveTowards(RudderAngle, target, Mathf.Max(0f, speed) * Time.deltaTime);
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
            UpdateVisual();
        }
    }
}
