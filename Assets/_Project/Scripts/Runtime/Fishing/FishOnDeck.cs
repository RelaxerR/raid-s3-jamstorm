using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Fishing
{
    /// <summary>Одна доступная для клика рыба из улова на палубе.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FishOnDeck : MonoBehaviour
    {
        [SerializeField] private string _fishId = "fish.greybox";
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _phase;
        private CargoHold _cargoHold;

        internal bool IsStored { get; private set; }

        internal void SetCargoHold(CargoHold hold) => _cargoHold = hold;

        private void Start()
        {
            _restPosition = transform.localPosition;
            _restRotation = transform.localRotation;
            _phase = _restPosition.x + _restPosition.z;
        }

        private void Update()
        {
            if (IsStored) return;
            float hop = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 5f + _phase)), 4);
            transform.localPosition = _restPosition + Vector3.up * (hop * 0.06f);
            transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, hop * 19f);
        }

        public DeckFishStoreResult TryStore(SoloGameSession session)
        {
            if (IsStored || !isActiveAndEnabled || session == null) return DeckFishStoreResult.Unavailable;
            DeckFishStoreResult result = session.StoreDeckFish(_fishId);
            if (result != DeckFishStoreResult.Stored) return result;

            IsStored = true;
            GetComponent<BoxCollider>().enabled = false;
            if (_cargoHold == null || !_cargoHold.TryAnimateTransfer(this, session.Cargo.Count - 1))
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            return result;
        }
    }
}
