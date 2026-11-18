using SecondCatch.Boat;
using SecondCatch.Input;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Player
{
    /// <summary>Перемещает игрока внутри палубы и направляет ввод в одну активную станцию.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        [SerializeField] private Transform _movementReference;
        [SerializeField, Min(0f)] private float _walkSpeed = 1.5f;
        [SerializeField] private Vector2 _deckCenter = Vector2.zero;
        [SerializeField] private Vector2 _deckHalfExtents = new Vector2(1f, 2f);
        [SerializeField, Min(0f)] private float _edgePadding = 0.18f;
        [SerializeField] private StationInteraction[] _stations;
        private StationInteraction _activeStation;
        private float _deckHeight;

        public BoatController Boat => _boat;
        public PlayerControlMode ControlMode { get; private set; } = PlayerControlMode.Walking;
        /// <summary>Множитель скорости для будущего бега; Shift пока не обрабатывается.</summary>
        public float MovementSpeedMultiplier { get; set; } = 1f;

        private void Awake()
        {
            if (_boat == null) _boat = GetComponentInParent<BoatController>();
            if (_boat == null)
            {
                Debug.LogError("Игроку нужна ссылка на лодку.", this);
                enabled = false;
                return;
            }
            transform.SetParent(_boat.transform, true);
            _deckHeight = transform.localPosition.y;
            ConstrainToDeck();
        }

        private void Update()
        {
            if (_boat == null || !_boat.isActiveAndEnabled)
            {
                ExitStation();
                return;
            }
            if (ControlMode != PlayerControlMode.Walking)
            {
                if (_activeStation == null || !_activeStation.isActiveAndEnabled || GameplayInput.InteractPressed || GameplayInput.CancelPressed)
                    ExitStation();
                else
                    _activeStation.HandleInput(GameplayInput.Movement);
                return;
            }
            if (GameplayInput.InteractPressed && TryEnterNearestStation()) return;
            Move(GameplayInput.Movement);
        }

        private void Move(Vector2 input)
        {
            Vector3 forward = _movementReference != null ? Vector3.ProjectOnPlane(_movementReference.forward, Vector3.up).normalized : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
            transform.position += direction * (_walkSpeed * Mathf.Max(0f, MovementSpeedMultiplier) * Time.deltaTime);
            ConstrainToDeck();
        }

        private void ConstrainToDeck()
        {
            Vector2 limit = new Vector2(Mathf.Max(0f, _deckHalfExtents.x - _edgePadding),
                Mathf.Max(0f, _deckHalfExtents.y - _edgePadding));
            Vector3 position = transform.localPosition;
            position.x = Mathf.Clamp(position.x, _deckCenter.x - limit.x, _deckCenter.x + limit.x);
            position.z = Mathf.Clamp(position.z, _deckCenter.y - limit.y, _deckCenter.y + limit.y);
            position.y = _deckHeight;
            transform.localPosition = position;
        }

        private bool TryEnterNearestStation()
        {
            StationInteraction nearest = GetAvailableStation();
            return nearest != null && nearest.TryEnter(this);
        }

        /// <summary>Возвращает ту же доступную станцию, которую выберет взаимодействие по E.</summary>
        public StationInteraction GetAvailableStation()
        {
            if (!isActiveAndEnabled || ControlMode != PlayerControlMode.Walking || _stations == null) return null;
            StationInteraction nearest = null;
            float distance = float.PositiveInfinity;
            foreach (StationInteraction station in _stations)
            {
                if (station == null || !station.CanEnter(this)) continue;
                float candidateDistance = station.DistanceSquared(this);
                if (candidateDistance >= distance) continue;
                nearest = station;
                distance = candidateDistance;
            }
            return nearest;
        }

        /// <summary>Фиксирует режим после проверки доступности станции.</summary>
        internal void BeginStation(StationInteraction station)
        {
            _activeStation = station;
            ControlMode = station.ControlMode;
        }

        /// <summary>Возвращает ходьбу и освобождает ввод станции, сохраняя позицию на палубе.</summary>
        public void ExitStation()
        {
            StationInteraction station = _activeStation;
            _activeStation = null;
            ControlMode = PlayerControlMode.Walking;
            if (station != null) station.Release(this);
        }

        private void OnDisable() => ExitStation();
    }
}
