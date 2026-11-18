using UnityEngine;
namespace SecondCatch.World
{
    /// <summary>Контракт сил моря: направление к порту, рост давления и постоянный ветер серого показа.</summary>
    public sealed class SeaEnvironment : MonoBehaviour
    {
        [SerializeField] private Transform _port;
        [SerializeField, Min(1)] private float _radius = 100;
        [SerializeField, Min(0)] private float _waveForce = 5;
        [SerializeField] private Vector3 _wind = Vector3.forward;
        public Vector3 Wind => _wind;
        public Vector3 SampleWaveForce(Vector3 position)
        {
            var offset = (_port != null ? _port.position : Vector3.zero) - position;
            offset.y = 0;
            return offset.normalized * Mathf.Clamp01(offset.magnitude / _radius) * _waveForce;
        }
    }
}
