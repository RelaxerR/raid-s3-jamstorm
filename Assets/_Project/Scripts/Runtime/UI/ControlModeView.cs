using SecondCatch.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Отображает текущий режим и предмет управления игрока.</summary>
    public sealed class ControlModeView : MonoBehaviour
    {
        [SerializeField] private Text _label;

        public void Render(PlayerControlMode mode, bool available)
        {
            if (_label == null) return;
            if (!available) { _label.text = "Управление недоступно"; return; }
            switch (mode)
            {
                case PlayerControlMode.Helm: _label.text = "Режим: Руль · Управление лодкой"; break;
                case PlayerControlMode.Sail: _label.text = "Режим: Парус · Управление парусом"; break;
                case PlayerControlMode.Net: _label.text = "Режим: Сети"; break;
                default: _label.text = "Режим: Ходьба · На палубе"; break;
            }
        }
    }
}
