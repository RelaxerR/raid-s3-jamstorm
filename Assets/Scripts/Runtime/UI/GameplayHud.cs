using SecondCatch.Boat;
using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.UI
{
    /// <summary>Передаёт состояние контроллеров отдельным представлениям после движения лодки.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _mapCenter;
        [SerializeField] private BoatStatusPanel _boatStatus;
        [SerializeField] private InteractionHintView _interactionHint;
        [SerializeField] private ControlModeView _controlMode;
        [SerializeField] private CompassView _compass;
        [SerializeField] private MinimapView _minimap;
        [SerializeField] private FutureGameplayIndicators _futureIndicators;

        /// <summary>Подключает prefab HUD к контроллерам другой сцены без поиска объектов.</summary>
        public void Bind(BoatController boat, PlayerController player, Transform mapCenter = null)
        {
            _boat = boat;
            _player = player;
            _mapCenter = mapCenter;
            Refresh();
        }

        private void OnEnable()
        {
            if (_futureIndicators != null) _futureIndicators.HideAll();
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            bool boatAvailable = _boat != null && _boat.isActiveAndEnabled;
            bool playerAvailable = boatAvailable && _player != null && _player.isActiveAndEnabled;
            if (_boatStatus != null) _boatStatus.Render(boatAvailable ? _boat : null);
            if (_controlMode != null) _controlMode.Render(playerAvailable ? _player.ControlMode : PlayerControlMode.Walking, playerAvailable);
            if (_interactionHint != null) _interactionHint.Render(playerAvailable ? _player : null);
            if (_compass != null) _compass.Render(boatAvailable ? _boat.transform.forward : Vector3.zero);
            if (_minimap != null)
                _minimap.Render(boatAvailable ? _boat.transform : null, _mapCenter != null ? _mapCenter.position : Vector3.zero);
        }
    }
}
