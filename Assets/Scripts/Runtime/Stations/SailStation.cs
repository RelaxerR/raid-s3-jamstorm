using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Переключает парус при входе; W поднимает, S опускает его в режиме станции.</summary>
    public sealed class SailStation : StationInteraction
    {
        [SerializeField] private Transform _sailVisual;
        [SerializeField, Range(0.01f, 1f)] private float _loweredHeightRatio = 0.15f;
        private Vector3 _raisedScale;
        public override PlayerControlMode ControlMode => PlayerControlMode.Sail;

        private void Awake()
        {
            if (_sailVisual != null) _raisedScale = _sailVisual.localScale;
            UpdateVisual();
        }

        protected override void OnEntered()
        {
            Boat.SetSailRaised(!Boat.SailRaised);
            UpdateVisual();
        }

        public override void HandleInput(Vector2 input)
        {
            if (input.y > 0f) Boat.SetSailRaised(true);
            else if (input.y < 0f) Boat.SetSailRaised(false);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_sailVisual == null || Boat == null) return;
            Vector3 scale = _raisedScale;
            scale.y *= Boat.SailRaised ? 1f : _loweredHeightRatio;
            _sailVisual.localScale = scale;
        }
    }
}
