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
        [SerializeField] private AudioSource _feedbackAudio;
        [SerializeField] private AudioClip _lossSound;
        [SerializeField] private AudioClip _aimSound;
        [SerializeField] private AudioClip _shotSound;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _tensionSound;

        private const float ShotCooldown = 0.45f;
        private const float TraceSeconds = 0.23f;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _lineProperties;
        private HarpoonLine _fight;
        private float _openingDistanceLeft;
        private Vector3 _openingDirection;
        private LineRenderer _line;
        private LineRenderer _shotHead;
        private Color _normalLineColor;
        private Vector3 _patrolCenter;
        private Vector3 _shotOrigin;
        private Vector3 _shotEnd;
        private Vector3 _fixedOffset;
        private float _nextShot;
        private float _traceUntil;
        private float _feedbackUntil;
        private string _feedback;
        private bool _missed;
        private bool _cast;
        private bool _wasVoyaging;
        private bool _tensionWarning;

        public override PlayerControlMode ControlMode => PlayerControlMode.Harpoon;
        public bool TakingLine => _openingDistanceLeft > 0f;
        public override bool AllowsCameraOrbit => !TakingLine && !Fighting && !CardOpen;
        public bool Fighting => _fight != null;
        public bool Secured { get; private set; }
        public bool CardOpen { get; private set; }
        public bool WillReplace => _voyage?.Session?.UniqueFishFull == true;
        public bool Running => _fight?.Running == true;
        public int RunDirection { get; private set; }
        public float Tension => _fight?.Tension ?? 0f;
        public float Length => _fight?.Length ?? 0f;
        public float Progress => _fight?.Progress(Vector3.ProjectOnPlane(_target.transform.position - Muzzle, Vector3.up).magnitude) ?? 0f;
        public string RecentLoss { get; private set; }
        public bool OnTarget { get; private set; }
        public string Status => TakingLine ? "Рыба отходит от борта" : Fighting ? (Running ? $"Рывок {(RunDirection < 0 ? "←" : "→")}" : "Рыба отдыхает")
            : Secured ? "Рыба зафиксирована у борта"
            : Time.time < _feedbackUntil ? _feedback
            : _voyage?.Session?.UniqueFishCaughtThisVoyage == true ? "Рыба этого рейса уже поймана"
            : OnTarget ? "Рыба в прицеле" : "Наведите курсор на багряную рыбу";

        private Vector3 Muzzle => _launcher.TransformPoint(Vector3.forward * 0.55f);

        private void Awake()
        {
            if (_target == null || _launcher == null || _carcass == null || _camera == null || _voyage == null
                || Boat == null || _ocean == null || _lineMaterial == null || _feedbackAudio == null || _lossSound == null)
            {
                Debug.LogError("Гарпун: нужны лодка, рейс, камера, крепление, рыба, туша, вода, материал троса и звук потери.", this);
                enabled = false;
                return;
            }

            _patrolCenter = _target.transform.position;
            _lineProperties = new MaterialPropertyBlock();
            _line = gameObject.AddComponent<LineRenderer>();
            _line.sharedMaterial = _lineMaterial;
            _normalLineColor = _lineMaterial.GetColor(BaseColor);
            _line.widthMultiplier = 0.04f;
            _line.positionCount = 2;
            _line.enabled = false;

            var head = new GameObject("HarpoonShotHead");
            head.transform.SetParent(transform, false);
            _shotHead = head.AddComponent<LineRenderer>();
            _shotHead.sharedMaterial = _lineMaterial;
            _shotHead.widthMultiplier = 0.11f;
            _shotHead.positionCount = 2;
            _shotHead.enabled = false;
        }

        public override void HandleInput(Vector2 input) { }

        protected override void OnEntered() => _voyage.Audio?.Play(_aimSound, 0.35f);

        protected override void OnExited()
        {
            CardOpen = false;
            OnTarget = false;
        }

        private void LateUpdate()
        {
            var session = _voyage.Session;
            bool voyaging = _voyage.Voyaging;
            _carcass.SetActive(session?.UniqueFishCount > 0);
            _target.gameObject.SetActive(voyaging && !session.UniqueFishCaughtThisVoyage
                && (RecentLoss == null || Time.time >= _feedbackUntil));
            if (!voyaging || Boat.IsRespawning)
            {
                if (_wasVoyaging)
                {
                    _fight = null;
                    _openingDistanceLeft = 0f;
                    Secured = CardOpen = _cast = false;
                    _traceUntil = 0f;
                    RecentLoss = null;
                    _feedbackUntil = 0f;
                }
                _wasVoyaging = false;
                _line.enabled = _shotHead.enabled = false;
                return;
            }

            _wasVoyaging = true;
            if (Time.time >= _feedbackUntil) RecentLoss = null;
            if (TakingLine) AdvanceOpeningRun();
            else if (Fighting) AdvanceFight();
            else if (Secured)
            {
                MoveFish(Muzzle + Boat.transform.TransformDirection(_fixedOffset), Vector3.zero);
                if (Operator != null && !CardOpen && GameplayInput.TryGetPrimaryClick(out Vector2 pointer)
                    && TryAim(pointer, out _, out _))
                    CardOpen = true;
            }
            else
            {
                if (_cast && Time.time >= _traceUntil)
                {
                    _cast = false;
                    AttachLine();
                }
                if (!Fighting && !TakingLine)
                {
                    Patrol();
                    if (Operator != null && RecentLoss == null) Aim();
                }
            }
            RenderLine();
        }

        private void Patrol()
        {
            if (!_target.gameObject.activeSelf) return;
            float phase = Time.time * 0.7f;
            Vector3 position = _patrolCenter + new Vector3(Mathf.Sin(phase), 0f, 0.8f * Mathf.Cos(phase));
            MoveFish(position, new Vector3(Mathf.Cos(phase), 0f, -0.8f * Mathf.Sin(phase)));
        }

        private void MoveFish(Vector3 position, Vector3 direction)
        {
            position.y = _ocean.GetWaterHeight(position, out _, out _) + 0.18f;
            _target.transform.position = position;
            if (direction.sqrMagnitude > 0.001f) _target.transform.rotation = Quaternion.LookRotation(direction);
        }

        private bool TryAim(Vector2 pointer, out Vector3 aimPoint, out Vector3 hitPoint)
        {
            aimPoint = hitPoint = Vector3.zero;
            if (!_camera.pixelRect.Contains(pointer)) return false;
            Ray ray = _camera.ScreenPointToRay(pointer);
            Physics.SyncTransforms();
            bool blocked = Physics.Raycast(ray, out RaycastHit obstacle, _camera.farClipPlane,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            aimPoint = blocked ? obstacle.point : ray.GetPoint(HarpoonLine.MaximumLength + Vector3.Distance(ray.origin, Muzzle));
            if (!_target.gameObject.activeSelf || !_target.Raycast(ray, out RaycastHit impact, blocked ? obstacle.distance : _camera.farClipPlane))
                return false;
            hitPoint = aimPoint = impact.point;
            return true;
        }

        private void Aim()
        {
            bool shot = GameplayInput.TryGetPrimaryClick(out Vector2 pointer);
            if (!_camera.pixelRect.Contains(pointer))
            {
                OnTarget = false;
                return;
            }

            Vector3 muzzle = Muzzle;
            OnTarget = TryAim(pointer, out Vector3 aimPoint, out Vector3 hitPoint)
                && Vector3.Distance(muzzle, hitPoint) <= HarpoonLine.MaximumLength
                && !Physics.Linecast(muzzle, hitPoint, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!shot || Time.time < _nextShot || _voyage.Session.UniqueFishCaughtThisVoyage) return;
            _nextShot = Time.time + ShotCooldown;
            _traceUntil = Time.time + TraceSeconds;
            _voyage.Audio?.Play(_shotSound, 0.5f);
            _shotOrigin = muzzle;
            _shotEnd = Vector3.MoveTowards(muzzle, aimPoint, HarpoonLine.MaximumLength);
            _missed = !OnTarget;
            _cast = OnTarget;
            if (_missed) SetFeedback("Промах · подойдите ближе или наведите курсор");
        }

        private void AttachLine()
        {
            // The hit was checked at firing time. Account for the distance to the fish's centre as it takes the line.
            Vector3 muzzle = Muzzle;
            Vector3 position = Vector3.MoveTowards(muzzle, _target.transform.position, HarpoonLine.MaximumLength - 0.12f);
            MoveFish(position, Vector3.zero);
            Vector3 offset = _target.transform.position - muzzle;
            Vector3 horizontal = Vector3.ProjectOnPlane(offset, Vector3.up);
            _voyage.Audio?.Play(_hitSound, 0.45f);
            float distance = horizontal.magnitude;
            _openingDistanceLeft = Mathf.Max(0f, HarpoonLine.MinimumFightDistance - distance);
            if (TakingLine)
                _openingDirection = distance > 0.001f ? horizontal / distance
                    : Vector3.ProjectOnPlane(Boat.transform.forward, Vector3.up).normalized;
            else
                BeginFight(horizontal, offset.y);
        }

        private void AdvanceOpeningRun()
        {
            // Ход рывка относительно лодки сохраняется, даже если она продолжает идти под парусом.
            _openingDistanceLeft = Mathf.MoveTowards(_openingDistanceLeft, 0f, HarpoonLine.EscapeSpeed * Time.deltaTime);
            Vector3 horizontal = _openingDirection * (HarpoonLine.MinimumFightDistance - _openingDistanceLeft);
            Vector3 muzzle = Muzzle;
            MoveFish(muzzle + horizontal, _openingDirection);
            if (!TakingLine) BeginFight(horizontal, _target.transform.position.y - muzzle.y);
        }

        private void BeginFight(Vector3 horizontal, float height)
        {
            _fight = new HarpoonLine(horizontal.magnitude, height);
            _tensionWarning = false;
            UpdateRunDirection(horizontal);
        }

        private void AdvanceFight()
        {
            Vector3 muzzle = Muzzle;
            Vector3 position = _target.transform.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(position - muzzle, Vector3.up);
            float vertical = position.y - muzzle.y;
            float mouse = Operator == null ? 0f : GameplayInput.LookDelta.x;
            if (Mathf.Abs(mouse) < 0.01f) mouse = 0f;
            float reelInput = Operator == null ? 0f : GameplayInput.Movement.y;

            HarpoonOutcome outcome = HarpoonOutcome.Fighting;
            for (float remaining = Time.deltaTime; outcome == HarpoonOutcome.Fighting && remaining > 0f;)
            {
                UpdateRunDirection(horizontal);
                float step = Mathf.Min(remaining, 0.05f);
                outcome = _fight.Advance(step, -mouse * RunDirection, reelInput, vertical, ref horizontal);
                remaining -= step;
            }

            UpdateRunDirection(horizontal);
            Vector3 destination = muzzle + horizontal;
            MoveFish(destination, Vector3.ProjectOnPlane(destination - position, Vector3.up));
            if (outcome == HarpoonOutcome.Fighting
                && Vector3.Distance(muzzle, _target.transform.position) >= HarpoonLine.MaximumLength)
                outcome = HarpoonOutcome.Escaped;
            if (outcome == HarpoonOutcome.Fighting)
            {
                if (!_tensionWarning && Tension >= HarpoonLine.WarningTension)
                {
                    _voyage.Audio?.Play(_tensionSound, 0.38f);
                    _tensionWarning = true;
                }
                else if (Tension < HarpoonLine.WarningTension - 0.1f)
                {
                    _tensionWarning = false;
                }
                return;
            }

            _fight = null;
            _nextShot = Time.time + ShotCooldown;
            if (outcome == HarpoonOutcome.Secured)
            {
                Secured = true;
                _fixedOffset = Boat.transform.InverseTransformDirection(horizontal);
                LogTransition(Status);
            }
            else
            {
                RecentLoss = outcome == HarpoonOutcome.Broken ? "Трос оборвался" : "Рыба сорвалась на пределе лески";
                SetFeedback(RecentLoss);
                _feedbackAudio.PlayOneShot(_lossSound);
                _target.gameObject.SetActive(false);
                LogTransition(RecentLoss);
            }
        }

        private void UpdateRunDirection(Vector3 horizontal)
        {
            Vector3 sideways = Vector3.Cross(Vector3.up, horizontal);
            int projection = Vector3.Dot(sideways, _camera.transform.right) < 0 ? -1 : 1;
            RunDirection = _fight.RunDirection * projection;
        }

        public void ResolveCatch(bool keep)
        {
            if (Operator == null || !Secured || !CardOpen || !_voyage.Voyaging) return;
            bool replacing = keep && WillReplace;
            if (keep && !_voyage.Session.TryStowUniqueFish(replacing)) return;
            CardOpen = Secured = false;
            _nextShot = Time.time + ShotCooldown;
            if (keep)
            {
                _target.gameObject.SetActive(false);
                _carcass.SetActive(true);
            }
            SetFeedback(keep ? (replacing ? "Туша заменена" : "Багряная рыба на палубе") : "Багряная рыба отпущена");
            LogTransition(_feedback);
        }

        private void SetFeedback(string feedback)
        {
            _feedback = feedback;
            _feedbackUntil = Time.time + 2.5f;
        }

        private void RenderLine()
        {
            bool attached = TakingLine || Fighting || Secured;
            _shotHead.enabled = !attached && Time.time < _traceUntil;
            _line.enabled = attached || _shotHead.enabled;
            if (!_line.enabled) return;
            _line.SetPosition(0, attached ? Muzzle : _shotOrigin);
            Vector3 tip = _target.transform.position;
            if (!attached)
            {
                float travel = Mathf.Clamp01(1f - (_traceUntil - Time.time) / TraceSeconds);
                tip = Vector3.Lerp(_shotOrigin, _shotEnd, Mathf.Min(1f, travel * 1.5f));
                Vector3 direction = (_shotEnd - _shotOrigin).normalized;
                if (_missed && travel > 0.67f)
                {
                    tip += Vector3.down * (travel - 0.67f) * 3f;
                    direction = Vector3.Slerp(direction, Vector3.down, (travel - 0.67f) * 3f);
                }
                _shotHead.SetPosition(0, tip);
                _shotHead.SetPosition(1, tip - direction * Mathf.Min(0.32f, Vector3.Distance(tip, _shotOrigin)));
            }
            _line.SetPosition(1, tip);
            Color color = Tension >= HarpoonLine.WarningTension ? Color.red : _normalLineColor;
            _lineProperties.SetColor(BaseColor, color);
            _line.SetPropertyBlock(_lineProperties);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogTransition(string message) => Debug.Log($"Гарпун: {message}.", this);
    }
}
