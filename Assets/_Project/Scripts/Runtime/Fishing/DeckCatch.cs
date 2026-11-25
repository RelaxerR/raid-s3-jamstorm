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
        [SerializeField] private CargoHold _secondBarrel;
        [SerializeField] private Material _amberMaterial;
        [SerializeField] private AudioSource _feedbackAudio;
        [SerializeField] private AudioClip _pickupSound;
        [SerializeField] private AudioClip _fullSound;
        [SerializeField] private AudioClip _incompatibleSound;
        [SerializeField, Min(0.01f)] private float _lossCheckInterval = 1f;
        [SerializeField, Min(0f)] private float _lossEdgeDistance = 0.5f;
        [SerializeField, Range(0f, 0.3f)] private float _lossChance = 0.3f;

        private const int Columns = 4;
        private const int Rows = 5;
        private readonly FishOnDeck[] _slots = new FishOnDeck[Columns * Rows];
        private readonly List<int> _availableSlots = new(Columns * Rows);
        private readonly int[] _visibleBySpecies = new int[FishCargo.SpeciesCount];
        private readonly int[] _previousBySpecies = new int[FishCargo.SpeciesCount];
        private float _feedbackUntil;
        private string _feedback;
        private float _nextLossCheck;
        private float _lossFeedbackUntil;

        public int Count => _voyage != null ? _voyage.Session?.DeckFishCount ?? 0 : 0;
        public string Feedback => Time.time < _feedbackUntil ? _feedback : null;
        public bool HasRecentLoss => Time.time < _lossFeedbackUntil;
        public event System.Action<FishOnDeck> FishLost;

        private void LateUpdate()
        {
            if (_voyage == null || !_voyage.Voyaging || _fishPrefab == null) return;
            RefreshDeck(_voyage.Session);
            if (GameplayInput.TryGetPrimaryClick(out Vector2 position))
                TryPickFish(position);
            CheckForLoss(static () => Random.value);
            _previousBySpecies[0] = _voyage.Session.DeckCount(FishCargo.AzureFish);
            _previousBySpecies[1] = _voyage.Session.DeckCount(FishCargo.AmberFish);
        }

        private void CheckForLoss(System.Func<float> roll)
        {
            if (Count == 0 || _lossChance <= 0f || _lossEdgeDistance <= 0f
                || _player == null || _player.Boat == null)
            {
                _nextLossCheck = 0f;
                return;
            }

            if (_nextLossCheck == 0f)
                _nextLossCheck = Time.time + _lossCheckInterval;
            if (Time.time < _nextLossCheck) return;
            _nextLossCheck = Time.time + _lossCheckInterval;

            foreach (FishOnDeck fish in _slots)
            {
                if (fish == null || fish.IsResolved) continue;
                Vector3 position = fish.transform.position;
                float distance = _player.GetNearestDeckEdge(position, out Vector3 outward);
                if (distance < 0f || distance >= _lossEdgeDistance) continue;
                float chance = _lossChance * (1f - distance / _lossEdgeDistance);
                if (roll() > chance) continue;

                const float beyondEdge = 0.55f;
                Vector3 overboard = position + outward * (distance + beyondEdge);
                if (!fish.TryLose(_voyage.Session, overboard)) continue;
                _lossFeedbackUntil = Time.time + 2.5f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"Палуба: рыба {FishCargo.SpeciesName(fish.Species == 0 ? FishCargo.AzureFish : FishCargo.AmberFish)} ушла за борт. До борта: {distance:0.00}; шанс: {chance:P0}; осталось: {Count}.", fish);
#endif
                FishLost?.Invoke(fish);
            }
        }

        private void RefreshDeck(SoloGameSession session)
        {
            int azure = session.DeckCount(FishCargo.AzureFish);
            int amber = session.DeckCount(FishCargo.AmberFish);
            // Share the deck points when both species are present, including when a second
            // haul lands while every point is still occupied by the first.
            int azureLimit = Mathf.Min(azure, Mathf.Max(_slots.Length / 2, _slots.Length - amber));
            int amberLimit = Mathf.Min(amber, _slots.Length - azureLimit);

            _visibleBySpecies[0] = _visibleBySpecies[1] = 0;
            _availableSlots.Clear();
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                FishOnDeck fish = _slots[slot];
                if (fish != null && !fish.IsResolved)
                {
                    int limit = fish.Species == 0 ? azureLimit : amberLimit;
                    if (_visibleBySpecies[fish.Species] < limit)
                    {
                        _visibleBySpecies[fish.Species]++;
                        continue;
                    }
                    // This fish still belongs to the deck catch and can be shown in a later batch.
                    fish.gameObject.SetActive(false);
                    Destroy(fish.gameObject);
                    _slots[slot] = null;
                }
                _availableSlots.Add(slot);
            }

            int free = _availableSlots.Count;
            FillSlots(0, azure, azureLimit, ref free);
            FillSlots(1, amber, amberLimit, ref free);
        }

        private void FillSlots(int species, int count, int limit, ref int free)
        {
            int visible = _visibleBySpecies[species];
            if (visible > 0 && count <= _previousBySpecies[species]) return;
            for (int i = visible; i < limit; i++)
            {
                int choice = Random.Range(0, free);
                int slot = _availableSlots[choice];
                _availableSlots[choice] = _availableSlots[--free];

                Vector3 position = new(
                    (slot % Columns - 1.5f) * 0.38f + Random.Range(-0.035f, 0.035f), 0f,
                    (slot / Columns - 2) * 0.33f + Random.Range(-0.022f, 0.022f));
                FishOnDeck fish = Instantiate(_fishPrefab, transform);
                fish.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));
                fish.Configure(species, _amberMaterial, _cargoHold, _secondBarrel);
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
