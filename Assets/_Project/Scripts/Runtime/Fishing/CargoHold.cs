using System.Collections;
using System.Collections.Generic;
using SecondCatch.Application;
using SecondCatch.Player;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Fishing
{
    /// <summary>Draws the session cargo on the boat deck.</summary>
    [DisallowMultipleComponent]
    public sealed class CargoHold : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private CargoFish _fishPrefab;
        [SerializeField] private Material _amberMaterial;
        [SerializeField] private int _barrelIndex;
        [SerializeField, Min(0.1f)] private float _releaseRadius = 0.55f;
        [SerializeField, Min(0.1f)] private float _releaseSeconds = 1.2f;

        private readonly List<CargoFish> _visuals = new();
        private bool _initialized;
        private float _holdTime;
        private float _feedbackUntil;
        private string _feedback;

        private FishBarrel Barrel => _voyage != null ? _voyage.Session?.Cargo.GetBarrel(_barrelIndex) : null;
        public string ReleaseHint => Time.time < _feedbackUntil ? _feedback
            : _holdTime > 0f ? $"Выпуск рыбы · {_holdTime / _releaseSeconds:0%}"
            : Barrel?.Count > 0 ? $"Удерживайте E · выпустить рыбу из бочки {_barrelIndex + 1}"
            : $"Бочка {_barrelIndex + 1} пуста";

        internal bool CanReleaseNear(PlayerController player, out float distance)
        {
            Vector3 offset = player.transform.localPosition - transform.localPosition;
            distance = offset.x * offset.x + offset.z * offset.z;
            return isActiveAndEnabled && _voyage != null && _voyage.Voyaging
                && distance <= _releaseRadius * _releaseRadius;
        }

        internal bool AdvanceRelease(float dt)
        {
            if (Barrel is not { Count: > 0 }) return true;
            _holdTime += dt;
            if (_holdTime < _releaseSeconds) return false;
            _holdTime = 0f;
            if (_voyage.Session.ReleaseBarrel(_barrelIndex))
            {
                _feedback = $"Рыба из бочки {_barrelIndex + 1} выпущена";
                _feedbackUntil = Time.time + 2f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"Бочка {_barrelIndex + 1}: улов выпущен.", this);
#endif
            }
            return true;
        }

        internal void CancelRelease() => _holdTime = 0f;

        private void LateUpdate()
        {
            FishBarrel barrel = Barrel;
            if (barrel == null || _fishPrefab == null) return;
            SyncVisuals(barrel.Count);
            _initialized = true;
        }

        internal bool TryAnimateTransfer(FishOnDeck selected, int slot)
        {
            if (!isActiveAndEnabled || _fishPrefab == null) return false;
            SyncVisuals(slot);
            CargoFish stored = AddVisual(animateDrop: false);
            stored.gameObject.SetActive(false);
            selected.transform.SetParent(transform, true);
            StartCoroutine(Transfer(selected, stored));
            return true;
        }

        private void SyncVisuals(int count)
        {
            while (_visuals.Count > count)
            {
                int last = _visuals.Count - 1;
                Destroy(_visuals[last].gameObject);
                _visuals.RemoveAt(last);
            }
            while (_visuals.Count < count) AddVisual(_initialized);
        }

        private CargoFish AddVisual(bool animateDrop)
        {
            int slot = _visuals.Count;
            float angle = slot * 137.5f * Mathf.Deg2Rad;
            float radius = 0.19f * Mathf.Sqrt((slot % 12) / 11f);
            Vector3 position = new(Mathf.Cos(angle) * radius, -0.11f + slot / 12 * 0.035f, Mathf.Sin(angle) * radius);
            Quaternion rotation = Quaternion.Euler(0f, (slot % 3 - 1) * 12f, 0f);
            CargoFish fish = Instantiate(_fishPrefab, transform);
            fish.transform.localScale *= 0.65f;
            if (Barrel?.FishId == FishCargo.AmberFish && _amberMaterial != null)
                fish.GetComponentInChildren<Renderer>().sharedMaterial = _amberMaterial;
            fish.TryStow(transform, position, rotation, animateDrop);
            _visuals.Add(fish);
            return fish;
        }

        private static IEnumerator Transfer(FishOnDeck selected, CargoFish stored)
        {
            const float duration = 0.2f;
            Transform movingFish = selected.transform;
            Transform destination = stored.transform;
            Vector3 start = movingFish.localPosition;
            Quaternion rotation = movingFish.localRotation;
            Vector3 scale = movingFish.localScale;
            for (float elapsed = 0f; elapsed < duration && selected != null && stored != null; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;
                Vector3 lift = Vector3.up * (1.4f * progress * (1f - progress));
                movingFish.SetLocalPositionAndRotation(
                    Vector3.Lerp(start, destination.localPosition, progress) + lift,
                    Quaternion.Slerp(rotation, destination.localRotation, progress));
                movingFish.localScale = Vector3.Lerp(scale, destination.localScale, progress);
                yield return null;
            }
            if (selected != null)
            {
                selected.gameObject.SetActive(false);
                Destroy(selected.gameObject);
            }
            if (stored != null) stored.gameObject.SetActive(true);
        }
    }
}
