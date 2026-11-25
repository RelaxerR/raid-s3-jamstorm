using SecondCatch.Fishing;
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

        private const string CameraHint = "\nПКМ + мышь — обзор · колесо — приближение · V — вернуть камеру";

        public void Render(PlayerController player, DeckCatch deckCatch = null)
        {
            if (_label == null) return;
            if (player == null) { _label.text = "Управление недоступно"; return; }
            if (player.ActiveStation is NetStation net)
            {
                _label.text = net.Status + "\n" + net.ActionHint + " · Esc — выйти" + CameraHint;
                return;
            }
            switch (player.ControlMode)
            {
                case PlayerControlMode.Helm:
                    _label.text = "W/S — гребля вперёд / назад · A/D — инерционный руль\nEsc / E — выйти (тяга и руль плавно затухают)" + "\n" + player.Boat.RowingAvailabilityText + CameraHint;
                    return;
                case PlayerControlMode.Sail:
                    _label.text = "W/S — больше / меньше паруса · A/D — разгон поворота\nEsc / E — выйти (угол сохраняется после выбега)" + CameraHint;
                    return;
            }
            StationInteraction station = player.GetAvailableStation();
            string action = station == null ? "" : station is HelmStation ? "\nE — управлять рулём"
                : station is SailStation ? "\nE — управлять парусом"
                : station is NetStation ? "\nE — занять станцию сетей" : "\nE — взаимодействовать";
            CargoHold barrel = player.GetAvailableBarrel();
            if (barrel != null) action = "\n" + barrel.ReleaseHint;
            if (deckCatch != null && deckCatch.Count > 0)
            {
                string catchHint = deckCatch.Feedback ?? $"ЛКМ по рыбе — в бочку · На палубе {deckCatch.Count}";
                _label.text = "WASD — перемещение" + action + "\n" + catchHint + CameraHint;
                return;
            }
            _label.text = "WASD — перемещение" + action + CameraHint;
        }
    }
}
