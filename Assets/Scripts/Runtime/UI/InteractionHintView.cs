using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Выбирает подсказки по режиму и общей с контроллером доступной станции.</summary>
    public sealed class InteractionHintView : MonoBehaviour
    {
        [SerializeField] private Text _label;

        public void Render(PlayerController player)
        {
            if (_label == null) return;
            if (player == null) { _label.text = "Управление недоступно"; return; }
            switch (player.ControlMode)
            {
                case PlayerControlMode.Helm:
                    _label.text = "A/D — корректировать курс · W/S — грести / прекратить\nEsc / E — выйти (парус продолжит работать)";
                    return;
                case PlayerControlMode.Sail:
                    _label.text = "W/S — поднять / опустить · A/D — повернуть парус\nEsc / E — выйти (настройка сохраняется)";
                    return;
            }
            StationInteraction station = player.GetAvailableStation();
            string action = station == null ? "" : station is HelmStation ? "\nE — управлять рулём"
                : station is SailStation ? "\nE — управлять парусом" : "\nE — взаимодействовать";
            _label.text = "WASD — перемещение" + action;
        }
    }
}
