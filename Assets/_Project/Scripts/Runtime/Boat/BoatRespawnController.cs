using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Возвращает уничтоженную лодку и игрока в порт, освобождая станции и сбрасывая инерцию.</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class BoatRespawnController : MonoBehaviour
    {
        [SerializeField] private Transform _respawnPoint;
        [SerializeField] private PlayerController _player;
        [SerializeField, Min(0f)] private float _respawnDelay = 1.5f;
        private BoatController _boat;
        private BoatHealth _health;
        private BoatBuoyancy _buoyancy;
        private Rigidbody _body;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _remaining;

        private void Awake()
        {
            _boat = GetComponent<BoatController>();
            _health = GetComponent<BoatHealth>();
            _buoyancy = GetComponent<BoatBuoyancy>();
            _body = GetComponent<Rigidbody>();
            if (_player == null) _player = GetComponentInChildren<PlayerController>(true);
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }
        private void OnEnable()
        {
            if (_health != null) _health.Destroyed += BeginRespawn;
        }
        private void OnDisable()
        {
            if (_health != null) _health.Destroyed -= BeginRespawn;
        }
        private void Update() => Advance(Time.deltaTime);

        private void Advance(float dt)
        {
            if (_health == null || _boat == null) return;
            // Нулевой HP из сохранённой сцены обрабатывается после инициализации всех компонентов.
            if (_health.IsDestroyed && !_boat.IsRespawning) BeginRespawn();
            if (!_boat.IsRespawning) return;
            _remaining -= dt;
            if (_remaining <= 0f) CompleteRespawn();
        }
        private void BeginRespawn()
        {
            if (_boat == null || _health == null || _boat.IsRespawning) return;
            _boat.IsRespawning = true;
            _health.BeginRespawn();
            _remaining = _respawnDelay;
            if (_player != null) _player.ExitStation();
            foreach (StationInteraction station in GetComponentsInChildren<StationInteraction>(true))
                if (station.Operator != null) station.Operator.ExitStation();
            ResetMotion();
        }
        private void ResetMotion()
        {
            _boat.ResetMotion();
            if (_buoyancy != null) _buoyancy.ResetMotion();
            if (_body == null) return;
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }
            _body.Sleep();
        }
        private void CompleteRespawn()
        {
            Vector3 position = _respawnPoint != null ? _respawnPoint.position : _initialPosition;
            Quaternion rotation = _respawnPoint != null ? _respawnPoint.rotation : _initialRotation;
            transform.SetPositionAndRotation(position, rotation);
            if (_body != null)
            {
                _body.position = position;
                _body.rotation = rotation;
            }
            ResetMotion();
            if (_player != null) _player.ReturnToBoatStart();
            _boat.IsRespawning = false;
            _health.CompleteRespawn();
        }
    }
}
