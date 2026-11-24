using SecondCatch.Boat;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Привязывает подсказку и шкалу на HUD к выбранной зоне лодки.</summary>
    public sealed class BoatRepairView : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private RectTransform _track;
        [SerializeField] private RectTransform _fill;

        public void Render(BoatRepairController repair, UnityEngine.Camera camera)
        {
            if (repair == null || !repair.SelectedZone.HasValue || camera == null)
            {
                gameObject.SetActive(false);
                return;
            }

            Vector3 screenPoint = camera.WorldToScreenPoint(repair.PromptPosition);
            if (screenPoint.z <= 0f || !camera.pixelRect.Contains(screenPoint))
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position = (Vector2)screenPoint;
            _label.text = repair.HasWood ? "Удерживайте E для ремонта" : "Нужна древесина";
            _track.gameObject.SetActive(repair.IsRepairing);
            _fill.anchorMax = new Vector2(repair.Progress, 1f);
            gameObject.SetActive(true);
        }
    }
}
