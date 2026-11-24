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
        [SerializeField] private CargoHold _cargoHold;
        [SerializeField] private AudioSource _feedbackAudio;
        [SerializeField] private AudioClip _pickupSound;
        [SerializeField] private AudioClip _fullSound;
        [SerializeField] private AudioClip _incompatibleSound;

        private const int Columns = 4;
        private const int Rows = 5;
        private readonly FishOnDeck[] _slots = new FishOnDeck[Columns * Rows];
        private readonly List<int> _availableSlots = new(Columns * Rows);
        private int _lastCount;
        private float _feedbackUntil;
        private string _feedback;

        public int Count => _voyage != null ? _voyage.Session?.DeckFishCount ?? 0 : 0;
        public string Feedback => Time.time < _feedbackUntil ? _feedback : null;

        private void LateUpdate()
        {
            if (_voyage == null || !_voyage.Voyaging || _fishPrefab == null) return;
            if (Count > 0) RefreshDeck(Count);
            if (GameplayInput.TryGetPrimaryClick(out Vector2 position))
                TryPickFish(position);
            _lastCount = Count;
        }

        private void RefreshDeck(int count)
        {
            _availableSlots.Clear();
            for (int slot = 0; slot < _slots.Length; slot++)
                if (_slots[slot] == null || _slots[slot].IsStored)
                    _availableSlots.Add(slot);

            int visible = _slots.Length - _availableSlots.Count;
            // Дополняем порцию новым уловом или выкладываем остаток после сбора текущей.
            if (visible > 0 && count <= _lastCount) return;
            int toSpawn = Mathf.Min(count, _slots.Length) - visible;
            int remaining = _availableSlots.Count;
            for (int i = 0; i < toSpawn; i++)
            {
                int choice = Random.Range(0, remaining);
                int slot = _availableSlots[choice];
                _availableSlots[choice] = _availableSlots[--remaining];

                Vector3 position = new(
                    (slot % Columns - 1.5f) * 0.38f + Random.Range(-0.035f, 0.035f), 0f,
                    (slot / Columns - 2) * 0.33f + Random.Range(-0.022f, 0.022f));
                FishOnDeck fish = Instantiate(_fishPrefab, transform);
                fish.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));
                fish.SetCargoHold(_cargoHold);
                _slots[slot] = fish;
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
                || !hit.collider.TryGetComponent(out FishOnDeck fish) || fish.transform.parent != transform) return;

            DeckFishStoreResult result = fish.TryStore(_voyage.Session);
            _feedback = result switch
            {
                DeckFishStoreResult.Full => "Бочка заполнена",
                DeckFishStoreResult.Incompatible => "Этот вид рыбы не помещается в бочку",
                _ => null
            };
            _feedbackUntil = Time.time + 2.5f;

            AudioClip sound = result switch
            {
                DeckFishStoreResult.Stored => _pickupSound,
                DeckFishStoreResult.Full => _fullSound,
                DeckFishStoreResult.Incompatible => _incompatibleSound,
                _ => null
            };
            if (_feedbackAudio != null && sound != null)
            {
                _feedbackAudio.spatialBlend = result == DeckFishStoreResult.Stored ? 1f : 0f;
                _feedbackAudio.clip = sound;
                _feedbackAudio.Play();
            }
        }
    }
}
