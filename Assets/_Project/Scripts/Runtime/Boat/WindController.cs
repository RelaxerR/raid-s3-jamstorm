using System;
using UnityEngine;

namespace SecondCatch.Boat
{
    /// <summary>Типы ветра прототипа.</summary>
    public enum WindType { Calm, Normal, Storm }

    /// <summary>Параметры типа ветра и цвет его стрелки.</summary>
    [Serializable]
    public sealed class WindProfile
    {
        [Min(0f)] public float Strength = 1f;
        [Min(0f)] public float MinSpeed = 1f;
        [Min(0f)] public float MaxSpeed = 3f;
        [Min(0f)] public float SailInfluence = 1f;
        [Min(0f)] public float SelectionWeight = 1f;
        public Color Color = Color.cyan;
    }

    /// <summary>Плавно меняет ветер по таймеру с вероятностным направлением к острову.</summary>
    [DisallowMultipleComponent]
    public sealed class WindController : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private Vector3 _direction = Vector3.forward;
        [SerializeField, Min(0f)] private float _strength = 1f;
        [SerializeField, Min(0f)] private float _minSpeed;
        [SerializeField, Min(0f)] private float _maxSpeed = 3f;
        [SerializeField, Min(0f)] private float _speed = 3f;
        [SerializeField] private Color _gizmoColor = Color.cyan;
        [SerializeField, Min(0.1f)] private float _gizmoLength = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float _arrowHeadRatio = 0.2f;
        [SerializeField] private WindType _type = WindType.Normal;
        [SerializeField] private WindProfile _calm = new WindProfile
        {
            Strength = 0f, MinSpeed = 0f, MaxSpeed = 0f, SailInfluence = 0f,
            SelectionWeight = 0.15f, Color = Color.gray
        };
        [SerializeField] private WindProfile _normal = new WindProfile { SelectionWeight = 0.7f };
        [SerializeField] private WindProfile _storm = new WindProfile
        {
            Strength = 1.5f, MinSpeed = 3f, MaxSpeed = 5f, SailInfluence = 1.3f,
            SelectionWeight = 0.15f, Color = new Color(1f, 0.35f, 0.15f)
        };
        [SerializeField, HideInInspector] private bool _profilesMigrated;
        [SerializeField] private bool _randomChanges = true;
        [SerializeField] private Vector2 _changeInterval = new Vector2(20f, 40f);
        [SerializeField, Min(0.01f)] private float _transitionDuration = 6f;
        [SerializeField, Range(0f, 180f)] private float _maxDirectionChange = 100f;
        [SerializeField] private Vector2 _strengthVariation = new Vector2(0.8f, 1.2f);
        [SerializeField] private Transform _boat;
        [SerializeField] private Transform _islandCenter;
        [SerializeField] private BoxCollider _seaSurface;
        [SerializeField, Min(0.1f)] private float _maxZoneRadius = 50f;
        [SerializeField, Range(0f, 1f)] private float _centerReturnProbability = 0.05f;
        [SerializeField, Min(0.01f)] private float _distanceExponent = 2f;
        [SerializeField] private AnimationCurve _returnProbabilityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private WindType _targetType;
        private float _startHeading;
        private float _targetHeading;
        private float _startSpeed;
        private float _targetSpeed;
        private float _startStrength;
        private float _targetStrength;
        private float _startInfluence;
        private float _targetInfluence;
        private float _sailInfluence = 1f;
        private Color _startColor;
        private Color _targetColor;
        private float _elapsed;
        private bool _transitioning;
        private float _startStormIntensity;
        private float _targetStormIntensity;

        public Vector3 Direction
        {
            get
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(_direction, Vector3.up);
                return horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.forward;
            }
        }
        public WindType Type => _type;
        public WindType TargetType => _targetType;
        public float Strength => Mathf.Max(0f, _strength);
        public float MinSpeed => Mathf.Max(0f, Profile(_type).MinSpeed);
        public float MaxSpeed => Mathf.Max(MinSpeed, Profile(_type).MaxSpeed);
        public float Speed => Mathf.Max(0f, _speed);
        public float SailInfluence => _type == WindType.Calm ? 0f : Mathf.Max(0f, _sailInfluence);
        public Color VisualColor => _gizmoColor;
        public float TimeUntilChange { get; private set; }
        /// <summary>Плавная интенсивность шторма для будущих волн и урона.</summary>
        public float StormIntensity { get; private set; }
        /// <summary>Будущая погода может подписаться на смену типа и читать StormIntensity.</summary>
        public event Action<WindType> WindTypeChanged;
        public float NormalizedDistance => _boat == null || _islandCenter == null ? 0f
            : Mathf.Clamp01(Vector3.ProjectOnPlane(_boat.position - _islandCenter.position, Vector3.up).magnitude
                / Mathf.Max(0.1f, _maxZoneRadius));
        public float IslandReturnProbability
        {
            get
            {
                if (_boat == null || _islandCenter == null) return 0f;
                if (IsAtSeaBoundary()) return 1f;
                float distance = Mathf.Pow(NormalizedDistance, Mathf.Max(0.01f, _distanceExponent));
                float curve = _returnProbabilityCurve == null ? distance : _returnProbabilityCurve.Evaluate(distance);
                return Mathf.Lerp(_centerReturnProbability, 1f, Mathf.Clamp01(curve));
            }
        }

        private WindProfile Profile(WindType type)
        {
            switch (type)
            {
                case WindType.Calm: return _calm;
                case WindType.Storm: return _storm;
                default: return _normal;
            }
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (!_profilesMigrated)
            {
                _normal.Strength = _strength;
                _normal.MinSpeed = _minSpeed;
                _normal.MaxSpeed = _maxSpeed;
                _normal.Color = _gizmoColor;
                _profilesMigrated = true;
            }
        }

        private void Awake()
        {
            _targetType = _type;
            WindProfile profile = Profile(_type);
            _speed = Mathf.Clamp(_speed, Mathf.Max(0f, profile.MinSpeed), Mathf.Max(profile.MinSpeed, profile.MaxSpeed));
            _strength = Mathf.Max(0f, profile.Strength);
            _sailInfluence = profile.SailInfluence;
            _gizmoColor = profile.Color;
            if (_type == WindType.Calm) _speed = _strength = _sailInfluence = 0f;
            StormIntensity = _type == WindType.Storm ? 1f : 0f;
            ScheduleChange();
        }

        private void ScheduleChange()
        {
            float min = Mathf.Max(0.01f, _changeInterval.x);
            TimeUntilChange = UnityEngine.Random.Range(min, Mathf.Max(min, _changeInterval.y));
        }

        private void Update()
        {
            if (_randomChanges)
            {
                TimeUntilChange = Mathf.Max(0f, TimeUntilChange - Time.deltaTime);
                if (TimeUntilChange <= 0f) ChooseNextWind();
            }
            if (!_transitioning) return;
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _transitionDuration));
            float t = Mathf.SmoothStep(0f, 1f, progress);
            _direction = Quaternion.AngleAxis(Mathf.LerpAngle(_startHeading, _targetHeading, t), Vector3.up) * Vector3.forward;
            _speed = Mathf.Lerp(_startSpeed, _targetSpeed, t);
            _strength = Mathf.Lerp(_startStrength, _targetStrength, t);
            _sailInfluence = Mathf.Lerp(_startInfluence, _targetInfluence, t);
            _gizmoColor = Color.Lerp(_startColor, _targetColor, t);
            StormIntensity = Mathf.Lerp(_startStormIntensity, _targetStormIntensity, t);
            if (progress >= 1f)
            {
                _transitioning = false;
                SetType(_targetType);
            }
        }

        private bool IsAtSeaBoundary()
        {
            if (_seaSurface == null) return NormalizedDistance >= 1f;
            Vector3 local = _seaSurface.transform.InverseTransformPoint(_boat.position) - _seaSurface.center;
            Vector3 half = _seaSurface.size * 0.5f;
            return Mathf.Abs(local.x) >= half.x || Mathf.Abs(local.z) >= half.z;
        }

        /// <summary>Выбирает новое состояние только по запросу или таймеру; граница сама смену не запускает.</summary>
        public void ChooseNextWind()
        {
            float calmWeight = Mathf.Max(0f, _calm.SelectionWeight);
            float normalWeight = Mathf.Max(0f, _normal.SelectionWeight);
            float total = calmWeight + normalWeight + Mathf.Max(0f, _storm.SelectionWeight);
            float roll = UnityEngine.Random.value * total;
            WindType type = total <= 0f ? _type : roll < calmWeight ? WindType.Calm
                : roll < calmWeight + normalWeight ? WindType.Normal : WindType.Storm;
            WindProfile profile = Profile(type);
            Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-_maxDirectionChange, _maxDirectionChange),
                Vector3.up) * Direction;
            float returnProbability = IslandReturnProbability;
            if (_boat != null && _islandCenter != null
                && (returnProbability >= 1f || UnityEngine.Random.value < returnProbability))
            {
                Vector3 toIsland = Vector3.ProjectOnPlane(_islandCenter.position - _boat.position, Vector3.up);
                if (toIsland.sqrMagnitude > 0.0001f) direction = toIsland.normalized;
            }
            float min = Mathf.Max(0f, profile.MinSpeed);
            float speed = UnityEngine.Random.Range(min, Mathf.Max(min, profile.MaxSpeed));
            float minStrength = Mathf.Max(0f, _strengthVariation.x);
            float strength = profile.Strength * UnityEngine.Random.Range(minStrength, Mathf.Max(minStrength, _strengthVariation.y));
            BeginTransition(type, direction, speed, strength, profile.SailInfluence, profile.Color);
            ScheduleChange();
        }

        /// <summary>Внешняя команда обычного ветра также применяет плавный переход.</summary>
        public void SetWind(Vector3 direction, float speed, float strength)
        {
            BeginTransition(WindType.Normal, direction, Mathf.Clamp(speed, Mathf.Max(0f, _normal.MinSpeed),
                Mathf.Max(_normal.MinSpeed, _normal.MaxSpeed)), strength, _normal.SailInfluence, _normal.Color);
            ScheduleChange();
        }

        private void BeginTransition(WindType type, Vector3 direction, float speed, float strength, float influence, Color color)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Direction;
            _startHeading = Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg;
            _targetHeading = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
            _startSpeed = Speed;
            _startStrength = Strength;
            _startInfluence = SailInfluence;
            _startColor = _gizmoColor;
            _startStormIntensity = StormIntensity;
            _targetType = type;
            _targetSpeed = type == WindType.Calm ? 0f : Mathf.Max(0f, speed);
            _targetStrength = type == WindType.Calm ? 0f : Mathf.Max(0f, strength);
            _targetInfluence = type == WindType.Calm ? 0f : Mathf.Max(0f, influence);
            _targetColor = color;
            _targetStormIntensity = type == WindType.Storm ? 1f : 0f;
            // Штиль объявляется после затухания, выход из штиля начинается с нулевой силы.
            if (type != WindType.Calm) SetType(type);
            _elapsed = 0f;
            _transitioning = true;
        }

        private void SetType(WindType type)
        {
            if (_type == type) return;
            _type = type;
            WindTypeChanged?.Invoke(type);
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
