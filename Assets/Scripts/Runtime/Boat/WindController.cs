using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Хранит постоянный мировой ветер; направление показывает, куда он дует.</summary>
    [DisallowMultipleComponent]
    public sealed class WindController : MonoBehaviour
    {
        [SerializeField] private Vector3 _direction = Vector3.forward;
        [SerializeField, Min(0f)] private float _strength = 1f;
        [SerializeField, Min(0f)] private float _minSpeed;
        [SerializeField, Min(0f)] private float _maxSpeed = 3f;
        [SerializeField, Min(0f)] private float _speed = 3f;
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField, Min(0.1f)] private float _gizmoLength = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float _arrowHeadRatio = 0.2f;

        public Vector3 Direction
        {
            get
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(_direction, Vector3.up);
                return horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.forward;
            }
        }
        public float Strength => Mathf.Max(0f, _strength);
        public float MinSpeed => Mathf.Max(0f, _minSpeed);
        public float MaxSpeed => Mathf.Max(MinSpeed, _maxSpeed);
        public float Speed => Mathf.Clamp(_speed, MinSpeed, MaxSpeed);

        /// <summary>Точка подключения будущей погоды; собственного таймера смены ветра нет.</summary>
        public void SetWind(Vector3 direction, float speed, float strength)
        {
            _direction = direction;
            _speed = Mathf.Clamp(speed, MinSpeed, MaxSpeed);
            _strength = Mathf.Max(0f, strength);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Vector3 origin = transform.position;
            Vector3 tip = origin + Direction * _gizmoLength;
            Vector3 side = Vector3.Cross(Vector3.up, Direction);
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawLine(tip, tip + (-Direction + side) * (_gizmoLength * _arrowHeadRatio));
            Gizmos.DrawLine(tip, tip + (-Direction - side) * (_gizmoLength * _arrowHeadRatio));
        }
    }
}
