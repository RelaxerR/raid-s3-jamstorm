using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает курс носа лодки; мировой +Z принят за север, +X за восток.</summary>
    public sealed class CompassView : MonoBehaviour
    {
        [SerializeField] private Text _heading;
        [SerializeField] private RectTransform _futureMarkers;
        public RectTransform FutureMarkers => _futureMarkers;
        private static readonly string[] Directions = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };

        public void Render(Vector3 forward)
        {
            if (_heading == null) return;
            if (forward.sqrMagnitude < 0.001f) { _heading.text = "КУРС —"; return; }
            float heading = Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
            int degrees = Mathf.RoundToInt(heading) % 360;
            int sector = Mathf.RoundToInt(heading / 45f) % Directions.Length;
            _heading.text = $"КУРС  {Directions[sector]}  {degrees:000}°";
        }
    }
}
