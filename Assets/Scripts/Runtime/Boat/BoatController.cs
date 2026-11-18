using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Перемещает лодку по плоскости и хранит команды руля и состояние паруса.</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class BoatController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _maxSpeed = 4f;
        [SerializeField, Min(0f)] private float _speedWithSail = 3f;
        [SerializeField, Min(0f)] private float _speedWithoutSail = 0.6f;
        [SerializeField, Min(0f)] private float _accelerationTime = 2f;
        [SerializeField, Min(0f)] private float _turnSpeed = 50f;
        [SerializeField, Min(0f)] private float _steeringResponse = 4f;
        [SerializeField] private bool _sailRaised;
        [SerializeField, Min(1f)] private float _maxHitPoints = 100f;
        [SerializeField, Min(0f)] private float _hitPoints = 100f;

        private float _steeringInput;
        private float _steering;
        public float CurrentSpeed { get; private set; }
        public bool IsSailing { get; private set; }
        public bool SailRaised => _sailRaised;
        public float MaxHitPoints => Mathf.Max(1f, _maxHitPoints);
        public float HitPoints => Mathf.Clamp(_hitPoints, 0f, MaxHitPoints);
        public float Steering => _steering;

        /// <summary>Принимает состояние прочности от будущего источника урона или ремонта.</summary>
        public void SetHitPoints(float hitPoints) => _hitPoints = Mathf.Clamp(hitPoints, 0f, MaxHitPoints);

        /// <summary>Сохраняет команду поворота; ноль постепенно возвращает руль в нейтраль.</summary>
        public void SetSteering(float steering) => _steeringInput = Mathf.Clamp(steering, -1f, 1f);
        public void SetSailing(bool sailing) => IsSailing = sailing;
        public void SetSailRaised(bool raised) => _sailRaised = raised;

        private void Update()
        {
            float targetSpeed = IsSailing ? Mathf.Clamp(_sailRaised ? _speedWithSail : _speedWithoutSail, 0f, _maxSpeed) : 0f;
            CurrentSpeed = _accelerationTime <= 0f ? targetSpeed : Mathf.MoveTowards(CurrentSpeed, targetSpeed,
                _maxSpeed / _accelerationTime * Time.deltaTime);
            _steering = Mathf.MoveTowards(_steering, _steeringInput, _steeringResponse * Time.deltaTime);
            // На стоящей лодке сохраняется возможность выбрать направление перед разгоном.
            transform.Rotate(Vector3.up, _steering * _turnSpeed * Time.deltaTime, Space.World);
            transform.position += transform.forward * (CurrentSpeed * Time.deltaTime);
        }

        private void OnDisable()
        {
            _steeringInput = _steering = CurrentSpeed = 0f;
            IsSailing = false;
        }
    }
}
