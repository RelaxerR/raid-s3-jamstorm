using SecondCatch.Boat;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Показывает фактическую прочность, скорость, парус и команду движения лодки.</summary>
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
            string motion = boat.IsSailing ? "Ход задан" : boat.CurrentSpeed > 0.01f ? "Торможение" : "Стоянка";
            string steering = Mathf.Abs(boat.Steering) < 0.01f ? "прямо" : boat.Steering < 0f ? "влево" : "вправо";
            _status.text = $"ЛОДКА   HP {boat.HitPoints:0}/{boat.MaxHitPoints:0}\n" +
                $"Скорость: {boat.CurrentSpeed:0.0} ед/с\n" +
                $"Парус: {(boat.SailRaised ? "поднят" : "опущен")}\n{motion} · Руль: {steering}";
        }
    }
}
