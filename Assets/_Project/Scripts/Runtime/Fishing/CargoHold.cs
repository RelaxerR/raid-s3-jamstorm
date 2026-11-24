using System.Collections;
using System.Collections.Generic;
using SecondCatch.Application;
using UnityEngine;

namespace SecondCatch.Fishing
{
    /// <summary>Draws the session cargo on the boat deck.</summary>
    [DisallowMultipleComponent]
    public sealed class CargoHold : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private CargoFish _fishPrefab;

        private readonly List<CargoFish> _visuals = new();
        private bool _initialized;

        private void LateUpdate()
        {
            var session = _voyage != null ? _voyage.Session : null;
            if (session == null || _fishPrefab == null) return;
            SyncVisuals(session.Cargo.Count);
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
            int row = slot / 5 % 5;
            Vector3 position = new((slot % 5 - 2) * 0.15f, slot / 25 * 0.085f, (row - 2) * 0.25f);
            Quaternion rotation = Quaternion.Euler(0f, (slot % 3 - 1) * 12f, 0f);
            CargoFish fish = Instantiate(_fishPrefab, transform);
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
