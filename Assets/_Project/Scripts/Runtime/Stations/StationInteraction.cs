using SecondCatch.Boat;
using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Определяет доступность станции и эксклюзивно передаёт ей управление игрока.</summary>
    public abstract class StationInteraction : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField, Min(0.1f)] private float _interactionRadius = 0.8f;
        [SerializeField] private Vector3 _cameraOffset;
        public Vector3 CameraOffset => _cameraOffset;
        protected BoatController Boat => _boat;
        public PlayerController Operator { get; private set; }
        public abstract PlayerControlMode ControlMode { get; }
        private Vector3 InteractionPosition => _interactionPoint != null ? _interactionPoint.position : transform.position;

        public float DistanceSquared(PlayerController player) => (player.transform.position - InteractionPosition).sqrMagnitude;

        public bool CanEnter(PlayerController player) => isActiveAndEnabled && _boat != null && _boat.isActiveAndEnabled && !_boat.IsRespawning
            && player != null && player.isActiveAndEnabled && player.Boat == _boat && Operator == null
            && player.ControlMode == PlayerControlMode.Walking
            && DistanceSquared(player) <= _interactionRadius * _interactionRadius;

        public bool TryEnter(PlayerController player)
        {
            if (!CanEnter(player)) return false;
            Operator = player;
            player.BeginStation(this);
            OnEntered();
            return true;
        }

        /// <summary>Освобождает станцию только для её текущего оператора.</summary>
        public void Release(PlayerController player)
        {
            if (Operator != player) return;
            OnExited();
            Operator = null;
        }

        public abstract void HandleInput(Vector2 input);
        protected virtual void OnEntered() { }
        protected virtual void OnExited() { }

        protected virtual void OnDisable()
        {
            if (Operator != null) Operator.ExitStation();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(InteractionPosition, _interactionRadius);
        }
    }
}
