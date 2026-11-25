using SecondCatch.Fishing;
using SecondCatch.Solo;
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

        public void SetCargo(SoloGameSession session) => Show(_cargo,
            $"Улов · Бочка 1 {DescribeBarrel(session.Cargo.First)}\nБочка 2 {DescribeBarrel(session.Cargo.Second)}"
            + $"\nТуша {session.UniqueFishCount}/{session.UniqueFishCapacity}");

        internal static string DescribeBarrel(FishBarrel barrel) =>
            $"{barrel.Count}/{barrel.Capacity} · {FishCargo.SpeciesName(barrel.FishId)}";
        public void SetWood(int count) => Show(_wood, $"Древесина: {count}");
        public void SetHarpoonLine(float tension, float length, float progress)
        {
            if (_tension != null)
                _tension.color = tension >= HarpoonLine.WarningTension ? new Color(1f, 0.42f, 0.38f) : new Color(0.9f, 0.97f, 1f);
            Show(_tension, $"Натяжение троса: {Mathf.Clamp01(tension):0%}");
            Show(_reeling, $"Леска: {length:0.0}/{HarpoonLine.MaximumLength:0} · Вываживание: {Mathf.Clamp01(progress):0%}");
        }

        public void HideHarpoonLine()
        {
            SetVisible(_tension, false);
            SetVisible(_reeling, false);
        }
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
