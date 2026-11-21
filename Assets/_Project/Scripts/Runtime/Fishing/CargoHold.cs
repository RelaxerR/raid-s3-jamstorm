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

            int count = session.Cargo.Count;
            while (_visuals.Count > count)
            {
                int last = _visuals.Count - 1;
                Destroy(_visuals[last].gameObject);
                _visuals.RemoveAt(last);
            }
            while (_visuals.Count < count)
            {
                int slot = _visuals.Count;
                int row = slot / 5 % 5;
                Vector3 position = new((slot % 5 - 2) * 0.15f, slot / 25 * 0.085f, (row - 2) * 0.25f);
                Quaternion rotation = Quaternion.Euler(0f, (slot % 3 - 1) * 12f, 0f);
                var fish = Instantiate(_fishPrefab, transform);
                fish.TryStow(transform, position, rotation, _initialized);
                _visuals.Add(fish);
            }
            _initialized = true;
        }
    }
}
