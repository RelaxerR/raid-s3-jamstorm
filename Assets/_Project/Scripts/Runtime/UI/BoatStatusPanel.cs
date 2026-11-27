using SecondCatch.Boat;
using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает движение, парус, ветер, HP, получение урона и возврат в порт.</summary>
    public sealed class BoatStatusPanel : MonoBehaviour
    {
        [SerializeField] private Text _status;
        [SerializeField] private Image _hitPointsFill;

        [SerializeField] private Color _healthyColor = new Color(0.2f, 0.8f, 0.65f);
        [SerializeField] private Color _criticalColor = new Color(0.95f, 0.25f, 0.2f);
        [SerializeField] private Color _damageColor = new Color(1f, 0.65f, 0.15f);

        public void Render(BoatController boat)
        {
            BoatHealth health = boat != null ? boat.Health : null;
            if (_hitPointsFill != null)
            {
                float fraction = boat != null ? boat.HitPoints / boat.MaxHitPoints : 0f;
                _hitPointsFill.rectTransform.anchorMax = new Vector2(fraction, 1f);
                _hitPointsFill.color = health != null && health.DamageFeedbackActive ? _damageColor
                    : Color.Lerp(_criticalColor, _healthyColor, fraction);
            }
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
                : boat.CurrentSpeed > 0.005f ? "Инерция и волны" : "Стоянка";
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
                    (currentWind.IsInOuterCalm ? "Внешний штиль · тяга 0"
                        : $"Ветер {currentWind.Speed:0.0} ед/с · сила ×{currentWind.Strength:0.0}");
            }
            string damage = health != null && health.DamageFeedbackActive ? $" · −{health.LastDamage:0} HP" : "";
            string movement = boat.IsRespawning ? "Лодка возвращается в порт" : $"Движение {source}";
            string helmsman = boat.HelmActive ? "игрок" : boat.Helm != null && boat.Helm.CrewSteering ? "Иван" : "свободен";
            _status.text = $"ЛОДКА   HP {boat.HitPoints:0}/{boat.MaxHitPoints:0}{damage}\n" +
                $"Скорость {boat.LongitudinalSpeed:+0.0;-0.0;0.0} · ход {boat.CurrentSpeed:0.0} ед/с\n" +
                $"Парус {sailPosition} · {boat.SailAngle:+0;-0;0}°\n" +
                $"Эффективность вперёд {boat.SailEfficiency:0%}\n{wind}\n" +
                $"{movement}\n{boat.RowingAvailabilityText}\nРуль · {helmsman} · {steering} {boat.RudderAngle:+0;-0;0}°";
        }
    }
}
