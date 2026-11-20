using System.Collections.Generic;
using UnityEngine;

namespace SecondCatch.Boat
{
    public enum BoatObstacleType { Reef, Rock, Debris }

    /// <summary>Наносит контактный урон с отдельной задержкой для каждой лодки.</summary>
    [DisallowMultipleComponent]
    public sealed class BoatDamageSource : MonoBehaviour
    {
        [SerializeField] private BoatObstacleType _type;
        [SerializeField, Min(0f)] private float _reefDamage = 15f;
        [SerializeField, Min(0f)] private float _rockDamage = 25f;
        [SerializeField, Min(0f)] private float _debrisDamage = 5f;
        [SerializeField, Min(0.01f)] private float _cooldown = 1f;
        private readonly Dictionary<BoatHealth, float> _nextDamageTimes = new Dictionary<BoatHealth, float>();
        public float Damage => Mathf.Max(0f, _type == BoatObstacleType.Reef ? _reefDamage
            : _type == BoatObstacleType.Rock ? _rockDamage : _debrisDamage);

        private void OnTriggerEnter(Collider other) => TryDamage(other);
        private void OnTriggerStay(Collider other) => TryDamage(other);
        private void OnCollisionEnter(Collision collision) => TryDamage(collision.collider);
        private void OnCollisionStay(Collision collision) => TryDamage(collision.collider);
        private void TryDamage(Collider other)
        {
            BoatHealth health = other.GetComponentInParent<BoatHealth>();
            if (health == null || health.IsDestroyed || health.IsRespawning || !health.isActiveAndEnabled) return;
            if (_nextDamageTimes.TryGetValue(health, out float nextTime) && Time.time < nextTime) return;
            _nextDamageTimes[health] = Time.time + Mathf.Max(0.01f, _cooldown);
            health.TakeDamage(Damage);
        }
        private void OnDisable() => _nextDamageTimes.Clear();
    }
}
