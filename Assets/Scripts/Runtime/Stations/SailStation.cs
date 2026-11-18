using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Меняет состояние и угол паруса; выбранная настройка сохраняется после выхода.</summary>
    public sealed class SailStation : StationInteraction
    {
        [SerializeField] private Transform _sailVisual;
        [SerializeField, Range(0.01f, 1f)] private float _loweredHeightRatio = 0.15f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 45f;
        private Vector3 _raisedScale;
        private Quaternion _initialRotation;
        public override PlayerControlMode ControlMode => PlayerControlMode.Sail;

        private void Awake()
        {
            if (_sailVisual != null)
            {
                _raisedScale = _sailVisual.localScale;
                _initialRotation = _sailVisual.localRotation;
            }
            UpdateVisual();
        }

        public override void HandleInput(Vector2 input)
        {
            if (input.y > 0f) Boat.SetSailRaised(true);
            else if (input.y < 0f) Boat.SetSailRaised(false);
            Boat.SetSailAngle(Boat.SailAngle + input.x * _rotationSpeed * Time.deltaTime);
        }

        private void LateUpdate() => UpdateVisual();

        private void UpdateVisual()
        {
            if (_sailVisual == null || Boat == null) return;
            Vector3 scale = _raisedScale;
            scale.y *= Boat.SailRaised ? 1f : _loweredHeightRatio;
            _sailVisual.localScale = scale;
            _sailVisual.localRotation = Quaternion.AngleAxis(Boat.SailAngle, Vector3.up) * _initialRotation;
        }
    }
}
