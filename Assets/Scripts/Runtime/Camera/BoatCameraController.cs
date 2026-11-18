using UnityEngine;

namespace SecondCatch.Camera
{
    /// <summary>Плавно держит лодку в центре кадра независимо от положения игрока на палубе.</summary>
    [DisallowMultipleComponent]
    public sealed class BoatCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _boat;
        [SerializeField, Min(0.1f)] private float _distance = 5.5f;
        [SerializeField, Min(0.1f)] private float _height = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.4f, 0f);
        [SerializeField, Min(0f)] private float _positionSmoothTime = 0.2f;
        [SerializeField, Min(0f)] private float _rotationSharpness = 12f;
        private Vector3 _velocity;

        private void OnEnable()
        {
            if (_boat == null) return;
            _velocity = Vector3.zero;
            Vector3 focus = _boat.position + _offset;
            transform.position = focus + new Vector3(0f, _height, -_distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        private void LateUpdate()
        {
            if (_boat == null || Time.deltaTime <= 0f) return;
            // Мировое смещение сохраняет удобные экранные направления при поворотах лодки.
            Vector3 focus = _boat.position + _offset;
            Vector3 destination = focus + new Vector3(0f, _height, -_distance);
            transform.position = _positionSmoothTime <= 0f ? destination : Vector3.SmoothDamp(
                transform.position, destination, ref _velocity, _positionSmoothTime);
            Quaternion rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            transform.rotation = _rotationSharpness <= 0f ? rotation : Quaternion.Slerp(transform.rotation,
                rotation, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime));
        }
    }
}
