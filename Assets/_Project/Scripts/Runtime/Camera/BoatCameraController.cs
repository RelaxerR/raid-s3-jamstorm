using SecondCatch.Input;
using SecondCatch.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace SecondCatch.Camera
{
    /// <summary>Орбитальная камера с мировым горизонтом, обзором мышью и плавным возвратом за корму.</summary>
    [DisallowMultipleComponent]
    public sealed class BoatCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _boat;
        [SerializeField] private PlayerController _player;
        [SerializeField] private float _defaultAzimuth;
        [SerializeField, Range(1f, 89f)] private float _defaultElevation = 42f;
        [FormerlySerializedAs("_distance")]
        [SerializeField, Min(0.1f)] private float _defaultDistance = 7.4f;
        [SerializeField, Min(0.1f)] private float _minimumDistance = 3f;
        [SerializeField, Min(0.1f)] private float _maximumDistance = 15f;
        [SerializeField, Min(0f)] private float _mouseSensitivity = 0.2f;
        [SerializeField, Min(0f)] private float _wheelSensitivity = 1.2f;
        [SerializeField, Range(1f, 89f)] private float _minimumElevation = 12f;
        [SerializeField, Range(1f, 89f)] private float _maximumElevation = 80f;
        [SerializeField, Min(0.01f)] private float _followSpeed = 8f;
        [SerializeField, Min(0.01f)] private float _returnSpeed = 4f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private Vector3 _stationOffset = new Vector3(0f, 0.3f, 0.4f);
        [SerializeField] private bool _invertVerticalAxis;
        private float _azimuth;
        private float _elevation;
        private float _distance;
        private float _targetDistance;
        private Vector3 _focus;
        private bool _returning = true;

        private float BoatHeading
        {
            get
            {
                Vector3 forward = Vector3.ProjectOnPlane(_boat.forward, Vector3.up);
                return forward.sqrMagnitude > 0.0001f ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg : _azimuth;
            }
        }
        private float ClampDistance(float distance) => Mathf.Clamp(distance,
            Mathf.Max(0.1f, _minimumDistance), Mathf.Max(_minimumDistance, _maximumDistance));
        private float ClampElevation(float angle) => Mathf.Clamp(angle,
            Mathf.Clamp(_minimumElevation, 1f, 89f), Mathf.Clamp(Mathf.Max(_minimumElevation, _maximumElevation), 1f, 89f));

        private void OnEnable()
        {
            if (_boat == null) return;
            if (transform.IsChildOf(_boat)) transform.SetParent(null, true);
            if (_player == null) _player = _boat.GetComponentInChildren<PlayerController>();
            _returning = true;
            _azimuth = BoatHeading + _defaultAzimuth;
            _elevation = ClampElevation(_defaultElevation);
            _distance = _targetDistance = ClampDistance(_defaultDistance);
            _focus = FocusPosition();
            transform.position = OrbitPosition();
            transform.rotation = Quaternion.LookRotation(_focus - transform.position, Vector3.up);
        }

        private Vector3 FocusPosition()
        {
            Vector3 offset = _offset;
            if (_player != null && _player.isActiveAndEnabled && _player.ActiveStation != null)
                offset += _stationOffset + _player.ActiveStation.CameraOffset;
            // Используем только курс: крен корпуса не меняет горизонт и ось орбиты.
            return _boat.position + Quaternion.AngleAxis(BoatHeading, Vector3.up) * offset;
        }

        private Vector3 OrbitPosition() => _focus + Quaternion.Euler(_elevation, _azimuth, 0f) * Vector3.back * _distance;

        private void LateUpdate()
        {
            if (_boat == null || Time.deltaTime <= 0f) return;
            float dt = Time.deltaTime;
            if (GameplayInput.CameraResetPressed)
            {
                _returning = true;
                _targetDistance = ClampDistance(_defaultDistance);
            }
            Vector2 look = GameplayInput.OrbitHeld ? GameplayInput.LookDelta : Vector2.zero;
            float zoom = GameplayInput.ZoomDelta;
            if (look.sqrMagnitude > 0f || Mathf.Abs(zoom) > 0.001f) _returning = false;
            _azimuth = Mathf.Repeat(_azimuth + look.x * _mouseSensitivity, 360f);
            _elevation = ClampElevation(_elevation + look.y * _mouseSensitivity * (_invertVerticalAxis ? 1f : -1f));
            _targetDistance = ClampDistance(_targetDistance - zoom * _wheelSensitivity);
            float returning = 1f - Mathf.Exp(-Mathf.Max(0.01f, _returnSpeed) * dt);
            if (_returning)
            {
                _azimuth = Mathf.LerpAngle(_azimuth, BoatHeading + _defaultAzimuth, returning);
                _elevation = Mathf.Lerp(_elevation, ClampElevation(_defaultElevation), returning);
            }
            _distance = Mathf.Lerp(_distance, _targetDistance, returning);
            float following = 1f - Mathf.Exp(-Mathf.Max(0.01f, _followSpeed) * dt);
            _focus = Vector3.Lerp(_focus, FocusPosition(), following);
            transform.position = Vector3.Lerp(transform.position, OrbitPosition(), following);
            Quaternion rotation = Quaternion.LookRotation(_focus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, following);
        }
    }
}
