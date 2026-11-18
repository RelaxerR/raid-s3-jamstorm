using SecondCatch.Boat;
using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает фактическую скорость, положение паруса, тип ветра и угол руля.</summary>
    public sealed class BoatStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text _status;
        [SerializeField] private Image _hitPointsFill;

        public void Render(BoatController boat)
        {
            if (_hitPointsFill != null)
                _hitPointsFill.rectTransform.anchorMax = new Vector2(boat != null ? boat.HitPoints / boat.MaxHitPoints : 0f, 1f);
            if (_status == null) return;
            if (boat == null)
            {
                _status.text = "Лодка недоступна";
                return;
            }
            bool sail = boat.SailSpeedContribution > 0f;
            bool rowing = Mathf.Abs(boat.RowingSpeedContribution) > 0f;
            string rowingSource = boat.RowingSpeedContribution < 0f ? "гребля назад" : "гребля вперёд";
            string source = sail && rowing ? "Парус + " + rowingSource : sail ? "Парус" : rowing ? rowingSource
                : boat.CurrentSpeed > 0f ? "Инерция" : "Стоянка";
            string steering = Mathf.Abs(boat.RudderAngle) < 0.1f ? "прямо" : boat.RudderAngle < 0f ? "влево" : "вправо";
            string sailPosition = boat.SailPosition == SailPosition.Full ? "полностью"
                : boat.SailPosition == SailPosition.Partial ? "частично" : "сложен";
            string wind = "Ветер недоступен";
            if (boat.Wind != null)
            {
                WindController currentWind = boat.Wind;
                Vector3 direction = currentWind.Direction;
                int heading = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f)) % 360;
                string type = currentWind.Type == WindType.Calm ? "штиль" : currentWind.Type == WindType.Storm ? "шторм" : "обычный";
                string color = ColorUtility.ToHtmlStringRGB(currentWind.VisualColor);
                wind = $"Ветер <color=#{color}>{type}</color> → {heading:000}°\n" +
                    $"Ветер {currentWind.Speed:0.0} ед/с · сила ×{currentWind.Strength:0.0}";
            }
            _status.text = $"ЛОДКА   HP {boat.HitPoints:0}/{boat.MaxHitPoints:0}\n" +
                $"Скорость {boat.LongitudinalSpeed:+0.0;-0.0;0.0} ед/с\n" +
                $"Парус {sailPosition} · {boat.SailAngle:+0;-0;0}°\n" +
                $"Эффективность вперёд {boat.SailEfficiency:0%}\n{wind}\n" +
                $"Движение {source}\nРуль {steering} · {boat.RudderAngle:+0;-0;0}°";
        }
    }
}
