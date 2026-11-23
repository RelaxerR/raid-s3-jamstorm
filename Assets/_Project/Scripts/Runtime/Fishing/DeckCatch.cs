using System.Collections.Generic;
using SecondCatch.Application;
using SecondCatch.Input;
using SecondCatch.Player;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Fishing
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class DeckCatch : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private PlayerController _player;
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private FishOnDeck _fishPrefab;
        private const int Columns = 4;
        private const int Rows = 5;
        private readonly List<FishOnDeck> _batch = new();
        private float _feedbackUntil;
        private string _feedback;

        public int Count => _voyage != null ? _voyage.Session?.DeckFishCount ?? 0 : 0;
        public string Feedback => Time.time < _feedbackUntil ? _feedback : null;

        private void LateUpdate()
        {
            if (_voyage == null || !_voyage.Voyaging || _fishPrefab == null) return;
            _batch.RemoveAll(fish => fish == null);
            if (_batch.Count == 0 && Count > 0) SpawnBatch();
            if (GameplayInput.TryGetPrimaryClick(out Vector2 position))
                TryPickFish(position);
        }

        private void SpawnBatch()
        {
            // Освобождаем место для следующей порции по мере сбора.
            int count = Mathf.Min(Count, Columns * Rows);
            for (int i = 0; i < count; i++)
            {
                FishOnDeck fish = Instantiate(_fishPrefab, transform);
                Vector3 slot = new((i % Columns - 1.5f) * 0.38f, 0f, (i / Columns - 2) * 0.33f);
                fish.transform.SetLocalPositionAndRotation(slot, Quaternion.Euler(0f, i % 2 == 0 ? -25f : 24f, 0f));
                _batch.Add(fish);
            }
        }

        private void TryPickFish(Vector2 position)
        {
            if (_camera == null || _player == null || !_player.isActiveAndEnabled
                || _player.ControlMode != PlayerControlMode.Walking || _player.Boat.IsRespawning
                || !_camera.pixelRect.Contains(position)) return;

            // Камера, палуба и рыба меняют Transform в этом же кадре.
            Physics.SyncTransforms();
            Ray ray = _camera.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hit, _camera.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                || !hit.collider.TryGetComponent(out FishOnDeck fish) || !_batch.Contains(fish)) return;

            _feedback = fish.TryStore(_voyage.Session) switch
            {
                DeckFishStoreResult.Full => "Бочка заполнена",
                DeckFishStoreResult.Incompatible => "Этот вид рыбы не помещается в бочку",
                _ => null
            };
            _feedbackUntil = Time.time + 2.5f;
        }
    }
}
