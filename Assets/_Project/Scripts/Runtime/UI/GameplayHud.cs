using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.UI
{
    /// <summary>Передаёт состояние контроллеров отдельным представлениям после движения лодки.</summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private PlayerController _player;
        [SerializeField] private DeckCatch _deckCatch;
        [SerializeField] private Transform _mapCenter;
        [SerializeField] private BoatStatusPanel _boatStatus;
        [SerializeField] private InteractionHintView _interactionHint;
        [SerializeField] private ControlModeView _controlMode;
        [SerializeField] private CompassView _compass;
        [SerializeField] private MinimapView _minimap;
        [SerializeField] private FutureGameplayIndicators _futureIndicators;
        [SerializeField] private BoatRepairView _repairView;
        [SerializeField] private UnityEngine.Camera _repairCamera;

        /// <summary>Подключает prefab HUD к контроллерам другой сцены без поиска объектов.</summary>
        public void Bind(BoatController boat, PlayerController player, Transform mapCenter = null)
        {
            _boat = boat;
            _player = player;
            _mapCenter = mapCenter;
            Refresh();
        }

        private void Awake()
        {
            // Встроенный шрифт Unity поддерживает русский текст без внешних ассетов.
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (UnityEngine.UI.Text label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                label.font = font;
        }

        private void OnEnable()
        {
            if (_futureIndicators != null) _futureIndicators.HideAll();
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            var session = _voyage != null ? _voyage.Session : null;
            if (_futureIndicators != null && session != null)
            {
                _futureIndicators.SetCargo(session);
                _futureIndicators.SetWood(session.Wood);
                if (_player != null && _player.ActiveStation is HarpoonStation { Fighting: true } harpoon)
                    _futureIndicators.SetHarpoonLine(harpoon.Tension, harpoon.Progress);
                else
                    _futureIndicators.HideHarpoonLine();
            }
            if (_repairView != null) _repairView.Render(_voyage != null ? _voyage.Repair : null, _repairCamera);

            bool boatAvailable = _boat != null && _boat.isActiveAndEnabled;
            bool playerAvailable = boatAvailable && !_boat.IsRespawning && _player != null && _player.isActiveAndEnabled;
            if (_boatStatus != null) _boatStatus.Render(boatAvailable ? _boat : null);
            if (_controlMode != null) _controlMode.Render(playerAvailable ? _player.ControlMode : PlayerControlMode.Walking, playerAvailable);
            if (_interactionHint != null) _interactionHint.Render(playerAvailable ? _player : null, _deckCatch);
            if (_compass != null) _compass.Render(boatAvailable ? _boat.transform.forward : Vector3.zero);
            if (_minimap != null)
                _minimap.Render(boatAvailable ? _boat.transform : null, _mapCenter != null ? _mapCenter.position : Vector3.zero);
        }
    }
}
