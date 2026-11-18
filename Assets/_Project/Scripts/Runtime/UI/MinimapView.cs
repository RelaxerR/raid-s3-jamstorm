using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Проецирует мировые X/Z в схему моря с фиксированным севером, без навигационной системы.</summary>
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField] private RectTransform _mapArea;
        [SerializeField] private RectTransform _boatMarker;
        [SerializeField] private Text _caption;
        [SerializeField] private RectTransform _futureMarkers;
        [SerializeField] private Vector2 _worldSize = new Vector2(400f, 400f);
        [SerializeField, Min(0f)] private float _markerPadding = 14f;
        public RectTransform FutureMarkers => _futureMarkers;

        public void Render(Transform boat, Vector3 center)
        {
            if (_boatMarker == null || _mapArea == null) return;
            _boatMarker.gameObject.SetActive(boat != null);
            if (boat == null)
            {
                if (_caption != null) _caption.text = "СХЕМА · Лодка недоступна";
                return;
            }
            Vector3 offset = boat.position - center;
            Vector2 size = _mapArea.rect.size;
            Vector2 position = new Vector2(offset.x / Mathf.Max(1f, _worldSize.x) * size.x,
                offset.z / Mathf.Max(1f, _worldSize.y) * size.y);
            Vector2 limit = new Vector2(Mathf.Max(0f, size.x * 0.5f - _markerPadding), Mathf.Max(0f, size.y * 0.5f - _markerPadding));
            bool outside = Mathf.Abs(position.x) > limit.x || Mathf.Abs(position.y) > limit.y;
            _boatMarker.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -limit.x, limit.x), Mathf.Clamp(position.y, -limit.y, limit.y));
            _boatMarker.localRotation = Quaternion.Euler(0f, 0f, -boat.eulerAngles.y);
            if (_caption != null) _caption.text = outside ? "СХЕМА · Лодка за краем" : "СХЕМА · Север сверху";
        }
    }
}
