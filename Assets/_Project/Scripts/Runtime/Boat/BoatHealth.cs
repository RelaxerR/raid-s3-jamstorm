using System;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Принимает контактный урон и показывает HP сессии или автономного graybox.</summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class BoatHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maximum = 100f;
        [SerializeField, Min(0f)] private float _current = 100f;
        [SerializeField, Min(0f)] private float _damageFeedbackDuration = 1.2f;
        private bool _initialized;
        private float _lastDamageTime = float.NegativeInfinity;
        private BoatState _voyageBoat;
        private Func<int, Vector3?, int> _applyVoyageDamage;
        internal bool UsesVoyageState => _voyageBoat != null;
        public float Maximum => UsesVoyageState ? _voyageBoat.MaxHp : Mathf.Max(1f, _maximum);
        public float Current => UsesVoyageState ? _voyageBoat.Hp : Mathf.Clamp(_current, 0f, Maximum);
        public bool IsDestroyed => Current <= 0f;
        public bool IsRespawning { get; private set; }
        public float LastDamage { get; private set; }
        public bool DamageFeedbackActive => Time.time - _lastDamageTime < _damageFeedbackDuration;
        public event Action<float, float> Changed;
        public event Action<float> Damaged;
        internal event Action<BoatObstacleType> ObstacleHit;
        public event Action Destroyed;

        private void Awake() => EnsureInitialized();

        internal void BindVoyage(BoatState boat, Func<int, Vector3?, int> applyDamage)
        {
            _voyageBoat = boat;
            _applyVoyageDamage = applyDamage;
            Changed?.Invoke(Current, Maximum);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _maximum = Mathf.Max(1f, _maximum);
            _current = Mathf.Clamp(_current, 0f, _maximum);
        }
        /// <summary>Однократно принимает сериализованные HP из старого контроллера.</summary>
        internal void ImportLegacy(float maximum, float current)
        {
            EnsureInitialized();
            _maximum = Mathf.Max(1f, maximum);
            SetCurrent(current);
        }
        public void SetCurrent(float value)
        {
            EnsureInitialized();
            if (UsesVoyageState || IsRespawning || float.IsNaN(value)) return;
            bool wasDestroyed = IsDestroyed;
            _current = Mathf.Clamp(value, 0f, Maximum);
            Changed?.Invoke(Current, Maximum);
            if (!wasDestroyed && IsDestroyed) Destroyed?.Invoke();
        }
        public void TakeDamage(float amount) => TakeDamage(amount, null);

        public void TakeDamage(float amount, Vector3? worldImpact) => TakeDamage(amount, worldImpact, null);

        internal void TakeDamage(float amount, Vector3? worldImpact, BoatObstacleType? obstacle)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || IsDestroyed || IsRespawning || float.IsNaN(amount) || amount <= 0f) return;
            float appliedDamage = Mathf.Min(Current, amount);
            if (UsesVoyageState)
            {
                appliedDamage = _applyVoyageDamage(Mathf.CeilToInt(appliedDamage), worldImpact);
                if (appliedDamage <= 0f) return;
            }
            LastDamage = appliedDamage;
            _lastDamageTime = Time.time;
            if (UsesVoyageState) Changed?.Invoke(Current, Maximum);
            else SetCurrent(Current - appliedDamage);
            Damaged?.Invoke(appliedDamage);
            if (obstacle.HasValue) ObstacleHit?.Invoke(obstacle.Value);
        }

        internal void NotifyVoyageRepair() => Changed?.Invoke(Current, Maximum);

        internal void BeginRespawn() => IsRespawning = true;
        internal void CompleteRespawn()
        {
            _current = Maximum;
            IsRespawning = false;
            LastDamage = 0f;
            _lastDamageTime = float.NegativeInfinity;
            Changed?.Invoke(Current, Maximum);
        }
    }
}
