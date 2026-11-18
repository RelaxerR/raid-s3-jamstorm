using System;
using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Хранит HP, принимает урон и сообщает об уничтожении лодки.</summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class BoatHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maximum = 100f;
        [SerializeField, Min(0f)] private float _current = 100f;
        [SerializeField, Min(0f)] private float _damageFeedbackDuration = 1.2f;
        private bool _initialized;
        private float _lastDamageTime = float.NegativeInfinity;
        public float Maximum => Mathf.Max(1f, _maximum);
        public float Current => Mathf.Clamp(_current, 0f, Maximum);
        public bool IsDestroyed => Current <= 0f;
        public bool IsRespawning { get; private set; }
        public float LastDamage { get; private set; }
        public bool DamageFeedbackActive => Time.time - _lastDamageTime < _damageFeedbackDuration;
        public event Action<float, float> Changed;
        public event Action<float> Damaged;
        public event Action Destroyed;

        private void Awake() => EnsureInitialized();
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
            if (IsRespawning || float.IsNaN(value)) return;
            bool wasDestroyed = IsDestroyed;
            _current = Mathf.Clamp(value, 0f, Maximum);
            Changed?.Invoke(Current, Maximum);
            if (!wasDestroyed && IsDestroyed) Destroyed?.Invoke();
        }
        public void TakeDamage(float amount)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || IsDestroyed || IsRespawning || float.IsNaN(amount) || amount <= 0f) return;
            LastDamage = Mathf.Min(Current, amount);
            _lastDamageTime = Time.time;
            float remaining = Current - LastDamage;
            Damaged?.Invoke(LastDamage);
            SetCurrent(remaining);
        }
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
