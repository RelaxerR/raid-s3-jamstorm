using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Spirit
{
    /// <summary>Создаёт одного духа на рейс с полностью заполненными бочками и ведёт его к лодке.</summary>
    public sealed class SeaSpiritDirector : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private BoatController _boat;
        [SerializeField] private GameObject _spiritPrefab;
        [SerializeField, Min(1f)] private float _spawnDistance = 18f;
        [SerializeField, Min(0f)] private float _spawnHeight = 1.5f;
        [SerializeField, Min(0f)] private float _pursuitSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float _attackDistance = 2.5f;
        [SerializeField, Min(0.1f)] private float _attackInterval = 2f;
        [SerializeField, Min(1)] private int _attackDamage = 5;

        public bool PursuitRequested { get; private set; }
        public bool IsPursuing => _activeSpirit != null;
        public int SpawnCount { get; private set; }
        public GameObject ActiveSpirit => _activeSpirit;

        private GameObject _activeSpirit;
        private float _nextAttackTime;
        private bool _spawnedThisVoyage;

        private void Awake()
        {
            if (_voyage == null) _voyage = GetComponent<HomeShallowsController>();
            if (_boat == null) _boat = FindAnyObjectByType<BoatController>();
        }

        private void Update()
        {
            var app = GameApplication.Instance;
            SoloGameSession session = _voyage != null ? _voyage.Session : app?.Session;
            bool activeVoyage = app != null && !app.IsLoading && session != null && session.AtSea;
            if (!activeVoyage)
            {
                PursuitRequested = false;
                ClearSpirit();
                _spawnedThisVoyage = false;
                return;
            }

            PursuitRequested = session.Cargo.IsFull;
            if (!PursuitRequested)
            {
                ClearSpirit();
                return;
            }

            if (_boat == null) _boat = FindAnyObjectByType<BoatController>();
            if (!_spawnedThisVoyage && _boat != null && _spiritPrefab != null)
                SpawnSpirit();
            if (_activeSpirit != null && _boat != null)
                PursueBoat();
        }

        private void SpawnSpirit()
        {
            Vector3 direction = _boat.HullDirection;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            Vector3 position = _boat.transform.position - direction.normalized * Mathf.Max(1f, _spawnDistance);
            position.y += _spawnHeight;
            _activeSpirit = Instantiate(_spiritPrefab, position,
                Quaternion.LookRotation(direction, Vector3.up), transform);
            _nextAttackTime = Time.time;
            _spawnedThisVoyage = true;
            SpawnCount++;
        }

        private void PursueBoat()
        {
            Vector3 target = _boat.transform.position + Vector3.up * _spawnHeight;
            Vector3 offset = target - _activeSpirit.transform.position;
            if (offset.sqrMagnitude > 0.001f)
            {
                _activeSpirit.transform.position = Vector3.MoveTowards(
                    _activeSpirit.transform.position, target, Mathf.Max(0f, _pursuitSpeed) * Time.deltaTime);
                _activeSpirit.transform.rotation = Quaternion.LookRotation(offset, Vector3.up);
            }

            if (offset.magnitude <= Mathf.Max(0.1f, _attackDistance) && Time.time >= _nextAttackTime)
            {
                _boat.TakeDamage(Mathf.Max(1, _attackDamage));
                _nextAttackTime = Time.time + Mathf.Max(0.1f, _attackInterval);
            }
        }

        private void ClearSpirit()
        {
            if (_activeSpirit == null) return;
            Destroy(_activeSpirit);
            _activeSpirit = null;
        }
    }
}
