using UnityEngine;

namespace SecondCatch.Application
{
    /// <summary>Причаливает только корпус лодки после его первого полного выхода из зоны.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PortZone : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private Collider _hull;
        private BoxCollider _zone;
        public bool ReadyToDock { get; private set; }

        private void Awake() => _zone = GetComponent<BoxCollider>();

        private void FixedUpdate()
        {
            if (!ReadyToDock && !_zone.bounds.Intersects(_hull.bounds)) ReadyToDock = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ReadyToDock && other == _hull) _voyage.ReturnToPort();
        }
    }
}
