using SecondCatch.Boat;
using SecondCatch.Input;
using SecondCatch.Fishing;
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
        [SerializeField] private CargoHold[] _barrels;
        private CargoHold _releasingBarrel;
        private StationInteraction _activeStation;
        private float _deckHeight;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        public Vector3 LocalWalkDirection { get; private set; }
        public BoatController Boat => _boat;
        public StationInteraction ActiveStation => _activeStation;
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
            _startPosition = transform.localPosition;
            _startRotation = transform.localRotation;
        }

        private void Update()
        {
            LocalWalkDirection = Vector3.zero;
            if (_boat == null || !_boat.isActiveAndEnabled || _boat.IsRespawning)
            {
                CancelBarrelRelease();
                ExitStation();
                return;
            }
            if (ControlMode != PlayerControlMode.Walking)
            {
                if (_activeStation == null || !_activeStation.isActiveAndEnabled || GameplayInput.CancelPressed)
                    ExitStation();
                else if (GameplayInput.InteractPressed && !_activeStation.TryInteract())
                    ExitStation();
                else
                    _activeStation.HandleInput(GameplayInput.Movement);
                return;
            }
            if (GameplayInput.InteractPressed)
            {
                _releasingBarrel = GetAvailableBarrel();
                if (_releasingBarrel == null && TryEnterNearestStation()) return;
            }
            Move(GameplayInput.Movement);
            if (_releasingBarrel != null && (!GameplayInput.InteractHeld
                || _releasingBarrel != GetAvailableBarrel() || _releasingBarrel.AdvanceRelease(Time.deltaTime)))
                CancelBarrelRelease();
        }

        private void Move(Vector2 input)
        {
            Vector3 forward = _movementReference != null ? Vector3.ProjectOnPlane(_movementReference.forward, Vector3.up).normalized : Vector3.forward;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
            Vector3 before = transform.localPosition;
            transform.position += direction * (_walkSpeed * Mathf.Max(0f, MovementSpeedMultiplier) * Time.deltaTime);
            ConstrainToDeck();
            Vector3 localStep = transform.localPosition - before;
            LocalWalkDirection = Vector3.ClampMagnitude(localStep / Mathf.Max(0.001f, _walkSpeed * Time.deltaTime), 1f);
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

        public CargoHold GetAvailableBarrel()
        {
            if (ControlMode != PlayerControlMode.Walking || _barrels == null) return null;
            StationInteraction station = GetAvailableStation();
            float bestDistance = station != null ? station.DistanceSquared(this) : float.PositiveInfinity;
            CargoHold nearest = null;
            foreach (CargoHold barrel in _barrels)
            {
                if (barrel == null || !barrel.CanReleaseNear(this, out float distance) || distance >= bestDistance)
                    continue;
                bestDistance = distance;
                nearest = barrel;
            }
            return nearest;
        }

        private void CancelBarrelRelease()
        {
            if (_releasingBarrel != null) _releasingBarrel.CancelRelease();
            _releasingBarrel = null;
        }

        internal float GetNearestDeckEdge(Vector3 worldPosition, out Vector3 outward)
        {
            Vector3 local = _boat.transform.InverseTransformPoint(worldPosition);
            float x = local.x - _deckCenter.x;
            float z = local.z - _deckCenter.y;
            float sideDistance = _deckHalfExtents.x - Mathf.Abs(x);
            float endDistance = _deckHalfExtents.y - Mathf.Abs(z);
            Vector3 direction = sideDistance <= endDistance
                ? Vector3.right * Mathf.Sign(x)
                : Vector3.forward * Mathf.Sign(z);
            outward = _boat.transform.TransformVector(direction);
            return Mathf.Min(sideDistance, endDistance);
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
            CancelBarrelRelease();
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

        /// <summary>Освобождает станцию и возвращает игрока к исходной позиции палубы.</summary>
        public void ReturnToBoatStart()
        {
            ExitStation();
            transform.SetParent(_boat.transform, false);
            transform.localPosition = _startPosition;
            transform.localRotation = _startRotation;
            ConstrainToDeck();
        }

        private void OnDisable()
        {
            CancelBarrelRelease();
            ExitStation();
        }
    }
}
