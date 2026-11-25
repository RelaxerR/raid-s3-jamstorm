using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private HarpoonStation _harpoon;
        [SerializeField] private Transform _mapCenter;
        [SerializeField] private BoatStatusPanel _boatStatus;
        [SerializeField] private InteractionHintView _interactionHint;
        [SerializeField] private ControlModeView _controlMode;
        [SerializeField] private CompassView _compass;
        [SerializeField] private MinimapView _minimap;
        [SerializeField] private FutureGameplayIndicators _futureIndicators;
        [SerializeField] private BoatRepairView _repairView;
        [SerializeField] private UnityEngine.Camera _repairCamera;

        private GameObject _catchCard;
        private Text _catchDetails;
        private Text _keepLabel;

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
            foreach (Text label in GetComponentsInChildren<Text>(true))
                label.font = font;
            if (_harpoon != null) CreateCatchCard(font);
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
                    _futureIndicators.SetHarpoonLine(harpoon.Tension, harpoon.Length, harpoon.Progress);
                else
                    _futureIndicators.HideHarpoonLine();
            }
            if (_repairView != null) _repairView.Render(_voyage != null ? _voyage.Repair : null, _repairCamera);

            bool boatAvailable = _boat != null && _boat.isActiveAndEnabled;
            bool playerAvailable = boatAvailable && !_boat.IsRespawning && _player != null && _player.isActiveAndEnabled;
            if (_boatStatus != null) _boatStatus.Render(boatAvailable ? _boat : null);
            if (_controlMode != null) _controlMode.Render(playerAvailable ? _player.ControlMode : PlayerControlMode.Walking, playerAvailable);
            if (_interactionHint != null) _interactionHint.Render(playerAvailable ? _player : null, _deckCatch, _harpoon);
            if (_catchCard != null)
            {
                bool showCard = playerAvailable && _harpoon.CardOpen;
                if (showCard && !_catchCard.activeSelf)
                {
                    bool replace = _harpoon.WillReplace;
                    _catchDetails.text = "Багряная рыба\nВес 18 кг · Редкая"
                        + (replace ? "\nМесто занято.\nЗаменить тушу на палубе?" : "");
                    _keepLabel.text = replace ? "Заменить" : "Забрать";
                }
                _catchCard.SetActive(showCard);
            }
            if (_compass != null) _compass.Render(boatAvailable ? _boat.transform.forward : Vector3.zero);
            if (_minimap != null)
                _minimap.Render(boatAvailable ? _boat.transform : null, _mapCenter != null ? _mapCenter.position : Vector3.zero);
        }

        private void CreateCatchCard(Font font)
        {
            gameObject.AddComponent<GraphicRaycaster>();
            _catchCard = new GameObject("HarpoonCatchCard", typeof(RectTransform), typeof(Image));
            _catchCard.layer = gameObject.layer;
            var panel = (RectTransform)_catchCard.transform;
            panel.SetParent(transform, false);
            panel.sizeDelta = new Vector2(540f, 218f);
            _catchCard.GetComponent<Image>().color = new Color(0.045f, 0.10f, 0.13f, 0.96f);

            _catchDetails = new GameObject("CatchDetails", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            _catchDetails.gameObject.layer = gameObject.layer;
            _catchDetails.rectTransform.SetParent(panel, false);
            _catchDetails.rectTransform.anchoredPosition = new Vector2(0f, 29f);
            _catchDetails.rectTransform.sizeDelta = new Vector2(500f, 134f);
            _catchDetails.font = font;
            _catchDetails.fontSize = 26;
            _catchDetails.alignment = TextAnchor.MiddleCenter;
            _catchDetails.raycastTarget = false;
            _catchDetails.color = Color.white;

            AddCatchButton(panel, font, "ReleaseFish", "Отпустить", -112f, false);
            _keepLabel = AddCatchButton(panel, font, "KeepFish", "Забрать", 112f, true);
            _catchCard.SetActive(false);
        }

        private Text AddCatchButton(RectTransform panel, Font font, string name, string caption, float x, bool keep)
        {
            GameObject control = DefaultControls.CreateButton(new DefaultControls.Resources());
            control.name = name;
            control.layer = gameObject.layer;
            var bounds = (RectTransform)control.transform;
            bounds.SetParent(panel, false);
            bounds.anchoredPosition = new Vector2(x, -70f);
            bounds.sizeDelta = new Vector2(196f, 44f);
            var button = control.GetComponent<Button>();
            button.targetGraphic = control.GetComponent<Image>();
            button.onClick.AddListener(() => _harpoon.ResolveCatch(keep));
            var label = control.GetComponentInChildren<Text>();
            label.gameObject.layer = gameObject.layer;
            label.font = font;
            label.fontSize = 24;
            label.raycastTarget = false;
            label.text = caption;
            return label;
        }
    }
}
