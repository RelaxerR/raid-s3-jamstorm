using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает груз и дополнительные индикаторы при поступлении данных.</summary>
    public sealed class FutureGameplayIndicators : MonoBehaviour
    {
        [SerializeField] private Text _cargo;
        [SerializeField] private Text _wood;
        [SerializeField] private Text _tension;
        [SerializeField] private Text _reeling;
        [SerializeField] private Text _lowHitPoints;
        [SerializeField] private Text _fishReceived;
        [SerializeField] private Text _spiritAppeared;

        public void HideAll()
        {
            SetVisible(_cargo, false);
            SetVisible(_wood, false);
            SetVisible(_tension, false);
            SetVisible(_reeling, false);
            SetVisible(_lowHitPoints, false);
            SetVisible(_fishReceived, false);
            SetVisible(_spiritAppeared, false);
        }

        public void SetCargo(int count, int capacity) => Show(_cargo, $"Улов {Mathf.Clamp(count, 0, Mathf.Max(0, capacity))}/{Mathf.Max(0, capacity)}");
        public void SetWood(int count) => Show(_wood, $"Древесина: {count}");
        public void SetTension(float normalized) => Show(_tension, $"Натяжение троса: {Mathf.Clamp01(normalized):0%}");
        public void SetReeling(float normalized) => Show(_reeling, $"Вываживание: {Mathf.Clamp01(normalized):0%}");
        public void SetLowHitPoints(bool visible) => SetVisible(_lowHitPoints, visible);
        public void SetFishNotification(bool visible) => SetVisible(_fishReceived, visible);
        public void SetSpiritNotification(bool visible) => SetVisible(_spiritAppeared, visible);

        private static void Show(Text label, string value)
        {
            if (label == null) return;
            if (label.text != value) label.text = value;
            SetVisible(label, true);
        }

        private static void SetVisible(Text label, bool visible)
        {
            if (label != null) label.transform.parent.gameObject.SetActive(visible);
        }
    }
}
