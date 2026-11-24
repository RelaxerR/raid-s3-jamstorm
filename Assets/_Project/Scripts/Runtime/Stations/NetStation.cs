using SecondCatch.Application;
using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    public enum NetState { Stowed, Lowering, Deployed, Raising }

    [DisallowMultipleComponent]
    public sealed class NetStation : StationInteraction
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private Transform _netVisual;
        [SerializeField] private Vector3 _deployedOffset = new(-0.8f, -0.18f, -0.45f);
        [SerializeField, Min(1)] private int _capacity = 100;
        [SerializeField, Min(0.01f)] private float _lowerSeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float _raiseSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float _minimumCaptureSpeed = 0.05f;
        [SerializeField, Min(0f)] private float _spillDelay = 3f;
        [SerializeField, Range(0, 100)] private int _spillPercentPerSecond = 10;
        [SerializeField, Min(0f)] private float _spillSpeed = 0.2f;

        private Vector3 _stowedPosition;
        private float _deployment;
        private float _movingWithFish;
        private float _spillSeconds;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _loggedSpill;
#endif

        public override PlayerControlMode ControlMode => PlayerControlMode.Net;
        public NetState State { get; private set; }
        public int Count => _voyage != null ? _voyage.Session?.NetFishCount ?? 0 : 0;
        public string Status => $"Сеть: {StateLabel} · {Count}/{_capacity}";
        public string ActionHint => State switch
        {
            NetState.Stowed => "E — опустить сеть",
            NetState.Deployed => "E — поднять сеть",
            _ => "Дождитесь завершения"
        };
        private string StateLabel => State switch
        {
            NetState.Lowering => "опускается",
            NetState.Deployed when Count >= _capacity => "заполнена",
            NetState.Deployed => "в воде",
            NetState.Raising => "поднимается",
            _ => "поднята"
        };

        private void Start()
        {
            if (_netVisual != null) _stowedPosition = _netVisual.localPosition;
            if (Count == 0) return;
            // Возвращаем улов после продолжения рейса из меню.
            State = NetState.Deployed;
            _deployment = 1f;
            UpdateVisual();
        }

        public override bool TryInteract()
        {
            if (Operator == null || _voyage == null || !_voyage.Voyaging) return true;
            if (State == NetState.Stowed)
            {
                State = NetState.Lowering;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _loggedSpill = false;
#endif
            }
            else if (State == NetState.Deployed) State = NetState.Raising;
            return true;
        }

        public override void HandleInput(Vector2 input) { }

        public bool TryCapture(int schoolId, int portion) => isActiveAndEnabled && State == NetState.Deployed
            && Boat != null && Boat.CurrentSpeed >= _minimumCaptureSpeed
            && _voyage != null && _voyage.Voyaging
            && _voyage.Session.TakeSchoolIntoNet(schoolId, portion, _capacity) > 0;

        private void Update()
        {
            if (_voyage == null || !_voyage.Voyaging) return;
            Advance(Time.deltaTime);
        }

        private void Advance(float dt)
        {
            switch (State)
            {
                case NetState.Lowering:
                    _deployment = Mathf.MoveTowards(_deployment, 1f, dt / _lowerSeconds);
                    if (_deployment == 1f) State = NetState.Deployed;
                    break;
                case NetState.Deployed:
                    SpillWhileMoving(dt);
                    break;
                case NetState.Raising:
                    _deployment = Mathf.MoveTowards(_deployment, 0f, dt / _raiseSeconds);
                    if (_deployment == 0f)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        int landed = Count;
#endif
                        _voyage.Session.LandNetCatch();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"Сети: подъём завершён, на палубу выложено рыб: {landed}.", this);
#endif
                        State = NetState.Stowed;
                        ResetSpill();
                    }
                    break;
            }
            UpdateVisual();
        }

        private void SpillWhileMoving(float dt)
        {
            if (Count == 0) { ResetSpill(); return; }
            if (Boat.CurrentSpeed < _spillSpeed || _spillPercentPerSecond <= 0) return;

            float beforeDelay = Mathf.Max(0f, _spillDelay - _movingWithFish);
            _movingWithFish = Mathf.Min(_spillDelay, _movingWithFish + dt);
            _spillSeconds += Mathf.Max(0f, dt - beforeDelay);
            while (_spillSeconds >= 1f && Count > 0)
            {
                _spillSeconds -= 1f;
                // Каждая секунда использует текущий остаток и округляет потерю вверх до целой рыбы.
                int lost = (int)(((long)Count * _spillPercentPerSecond + 99) / 100);
                _voyage.Session.SpillNetFish(lost);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!_loggedSpill)
                {
                    _loggedSpill = true;
                    Debug.Log($"Сети: началась потеря улова, потеряно рыб: {lost}, осталось: {Count}.", this);
                }
#endif
            }
            if (Count == 0) ResetSpill();
        }

        private void ResetSpill()
        {
            _movingWithFish = 0f;
            _spillSeconds = 0f;
        }

        private void UpdateVisual()
        {
            if (_netVisual != null) _netVisual.localPosition = _stowedPosition + _deployedOffset * _deployment;
        }
    }
}
