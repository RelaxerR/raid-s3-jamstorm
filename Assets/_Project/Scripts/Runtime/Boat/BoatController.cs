using SecondCatch.Application;
using SecondCatch.World;
using UnityEngine;
namespace SecondCatch.Boats
{
    /// <summary>Заготовка моторики: внешний ввод, силы моря и усиление сессии; плавучесть остаётся у OceanBuoyancy.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BoatController : MonoBehaviour
    {
        [SerializeField] private SeaEnvironment _environment;
        [SerializeField, Min(0)] private float _thrust = 5;
        [SerializeField, Min(0)] private float _turn = 1;
        private Rigidbody _body;
        private Vector2 _input;
        private void Awake() { _body = GetComponent<Rigidbody>(); }
        /// <summary>Вызывается адаптером Input System или будущим автоматизированным рулём.</summary>
        public void SetInput(Vector2 input) { _input = Vector2.ClampMagnitude(input, 1); }
        private void OnDisable() { _input = Vector2.zero; }
        private void FixedUpdate()
        {
            var app = GameApplication.Instance;
            if (app == null || app.IsLoading || app.Session == null || !app.Session.AtSea) return;
            float wind = _environment == null ? 1 : Mathf.Max(0, Vector3.Dot(transform.forward, _environment.Wind));
            _body.AddForce(transform.forward * (_input.y * _thrust * wind * app.Session.Boat.SpeedMultiplier), ForceMode.Acceleration);
            _body.AddTorque(Vector3.up * (_input.x * _turn), ForceMode.Acceleration);
            if (_environment != null) _body.AddForce(_environment.SampleWaveForce(transform.position), ForceMode.Acceleration);
        }
    }
}
