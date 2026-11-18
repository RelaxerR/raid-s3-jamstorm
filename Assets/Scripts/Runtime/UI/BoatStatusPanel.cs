using SecondCatch.Boat;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает фактическую прочность, скорость, парус, ветер и независимые источники тяги.</summary>
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
            bool rowing = boat.RowingSpeedContribution > 0f;
            string source = sail && rowing ? "Парус + руль" : sail ? "Парус" : rowing ? "Руль"
                : boat.CurrentSpeed > 0f ? "Инерция" : "Стоянка";
            string steering = Mathf.Abs(boat.Steering) < 0.01f ? "прямо" : boat.Steering < 0f ? "влево" : "вправо";
            string wind = "Ветер: недоступен";
            if (boat.Wind != null)
            {
                Vector3 direction = boat.Wind.Direction;
                int heading = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f)) % 360;
                wind = $"Ветер → {heading:000}° · {boat.Wind.Speed:0.0} · сила {boat.Wind.Strength:0.0}";
            }
            _status.text = $"ЛОДКА   HP {boat.HitPoints:0}/{boat.MaxHitPoints:0}\n" +
                $"Скорость: {boat.CurrentSpeed:0.0} ед/с\n" +
                $"Парус: {(boat.SailRaised ? "поднят" : "опущен")} · {boat.SailAngle:+0;-0;0}°\n" +
                $"Эффективность угла: {boat.SailEfficiency:0%}\n{wind}\n" +
                $"Движение: {source}\nРуль: {steering}";
        }
    }
}
