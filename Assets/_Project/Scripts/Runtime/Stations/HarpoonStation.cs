using SecondCatch.Application;
using SecondCatch.Fishing;
using SecondCatch.Input;
using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(80)]
    public sealed class HarpoonStation : StationInteraction
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private Transform _launcher;
        [SerializeField] private Collider _target;
        [SerializeField] private GameObject _carcass;
        [SerializeField] private OceanSystem _ocean;
        [SerializeField] private Material _lineMaterial;
        [SerializeField, Min(1f)] private float _shotRange = 22f;

        private const float ShotCooldown = 0.45f;
        private const float TraceSeconds = 0.2f;
        private HarpoonLine _fight;
        private LineRenderer _line;
        private Vector3 _patrolCenter;
        private Vector3 _lastImpact;
        private float _nextShot;
        private float _traceUntil;
        private float _feedbackUntil;
        private string _feedback;

        public override PlayerControlMode ControlMode => PlayerControlMode.Harpoon;
        public override bool AllowsCameraOrbit => !Fighting;
        public bool Fighting => _fight != null;
        public float Tension => _fight?.Tension ?? 0f;
        public float Progress => _fight?.Progress ?? 0f;
        public bool OnTarget { get; private set; }
        public string Status => Fighting ? (_fight.Surging ? "Рыба рвётся" : "Рыба устала")
            : Time.time < _feedbackUntil ? _feedback
            : _voyage?.Session?.UniqueFishCaughtThisVoyage == true ? "Рыба этого рейса уже поймана"
            : _voyage?.Session?.UniqueFishFull == true ? "Место для туши занято"
            : OnTarget ? "Рыба в прицеле" : "Наведите курсор на багряную рыбу";

        private Vector3 Muzzle => _launcher.TransformPoint(Vector3.forward * 0.55f);

        private void Awake()
        {
            if (_target == null || _launcher == null || _carcass == null || _camera == null || _voyage == null)
            {
                enabled = false;
                return;
            }

            _patrolCenter = _target.transform.position;
            _line = gameObject.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMaterial;
            _line.widthMultiplier = 0.04f;
            _line.positionCount = 2;
            _line.enabled = false;
        }

        public override void HandleInput(Vector2 input) { }

        protected override void OnExited()
        {
            _fight = null;
            _traceUntil = 0f;
            OnTarget = false;
            if (_line != null) _line.enabled = false;
        }

        private void LateUpdate()
        {
            var session = _voyage.Session;
            _carcass.SetActive(session?.UniqueFishCount > 0);
            _target.gameObject.SetActive(_voyage.Voyaging && !session.UniqueFishCaughtThisVoyage);
            if (!_voyage.Voyaging || Operator == null)
            {
                if (_fight != null) OnExited();
                _line.enabled = false;
                if (_target.gameObject.activeSelf) MoveFish();
                return;
            }

            if (Fighting) AdvanceFight();
            MoveFish();
            if (!Fighting) Aim();
            RenderLine();
        }

        private void MoveFish()
        {
            if (!_target.gameObject.activeSelf) return;
            float phase = Time.time * 0.7f;
            Vector3 patrol = _patrolCenter + new Vector3(1f * Mathf.Sin(phase), 0f, 0.8f * Mathf.Cos(phase));
            Vector3 direction = new(Mathf.Cos(phase), 0f, -0.8f * Mathf.Sin(phase));
            if (Fighting)
            {
                Vector3 alongside = Muzzle + Boat.transform.forward * 1.4f + Boat.transform.right * 0.8f;
                patrol = Vector3.Lerp(patrol, alongside, Progress);
                direction = Vector3.ProjectOnPlane(alongside - _target.transform.position, Vector3.up);
            }

            if (_ocean != null) patrol.y = _ocean.GetWaterHeight(patrol, out _, out _) + 0.18f;
            patrol.y += 0.06f * Mathf.Sin(phase * 5f);
            _target.transform.position = patrol;
            if (direction.sqrMagnitude > 0.001f) _target.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Aim()
        {
            OnTarget = false;
            bool shot = GameplayInput.TryGetPrimaryClick(out Vector2 pointer);
            if (!_camera.pixelRect.Contains(pointer)) return;
            Ray ray = _camera.ScreenPointToRay(pointer);
            Physics.SyncTransforms();
            bool blocked = Physics.Raycast(ray, out RaycastHit obstacle, _camera.farClipPlane,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float sightRange = blocked ? obstacle.distance : _camera.farClipPlane;
            Vector3 muzzle = Muzzle;
            OnTarget = _target.gameObject.activeSelf && _target.Raycast(ray, out RaycastHit impact, sightRange)
                && Vector3.Distance(muzzle, impact.point) <= _shotRange
                && !Physics.Linecast(muzzle, impact.point, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!shot || Time.time < _nextShot) return;
            _nextShot = Time.time + ShotCooldown;

            var session = _voyage.Session;
            if (session.UniqueFishFull || session.UniqueFishCaughtThisVoyage)
            {
                SetFeedback(session.UniqueFishFull ? "Место для туши занято" : "Рыба этого рейса уже поймана");
                return;
            }

            Vector3 point = blocked ? obstacle.point : ray.GetPoint(_shotRange + Vector3.Distance(ray.origin, muzzle));
            _lastImpact = Vector3.MoveTowards(muzzle, point, _shotRange);
            _traceUntil = Time.time + TraceSeconds;
            if (OnTarget)
                _fight = new HarpoonLine();
            else
                SetFeedback("Промах · подойдите ближе или наведите курсор");
        }

        private void AdvanceFight()
        {
            // Step the line in small increments after a long frame so a surge can't be skipped.
            HarpoonOutcome outcome = HarpoonOutcome.Fighting;
            if (Vector3.Distance(Muzzle, _target.transform.position) > _shotRange + 2f)
                outcome = HarpoonOutcome.Broken;
            bool reel = GameplayInput.PrimaryHeld;
            bool slack = GameplayInput.OrbitHeld;
            for (float remaining = Time.deltaTime; outcome == HarpoonOutcome.Fighting && remaining > 0f;)
            {
                float step = Mathf.Min(remaining, 0.05f);
                outcome = _fight.Advance(step, reel, slack);
                remaining -= step;
            }
            if (outcome == HarpoonOutcome.Fighting) return;

            _fight = null;
            _traceUntil = 0f;
            _nextShot = Time.time + ShotCooldown;
            if (outcome == HarpoonOutcome.Broken)
            {
                SetFeedback("Трос оборвался · можно выстрелить снова");
                return;
            }

            var session = _voyage.Session;
            if (session.TryStowUniqueFish())
            {
                _target.gameObject.SetActive(false);
                _carcass.SetActive(true);
                SetFeedback("Багряная рыба на палубе");
            }
            else
            {
                SetFeedback("Место для туши занято");
            }
        }

        private void SetFeedback(string feedback)
        {
            _feedback = feedback;
            _feedbackUntil = Time.time + 2.5f;
        }

        private void RenderLine()
        {
            _line.enabled = Fighting || Time.time < _traceUntil;
            if (!_line.enabled) return;
            _line.SetPosition(0, Muzzle);
            _line.SetPosition(1, Fighting ? _target.transform.position : _lastImpact);
        }
    }
}
