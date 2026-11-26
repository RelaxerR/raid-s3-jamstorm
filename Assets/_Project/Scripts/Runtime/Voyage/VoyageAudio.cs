using SecondCatch.Boat;
using SecondCatch.Stations;
using UnityEngine;
using UnityEngine.Audio;

namespace SecondCatch.Application
{
    [DisallowMultipleComponent]
    public sealed class VoyageAudio : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private BoatController _boat;
        [SerializeField] private HelmStation _helm;
        [SerializeField] private BoatBuoyancy _buoyancy;
        [SerializeField] private AudioSource _effects;
        [SerializeField] private AudioMixerGroup _ambience;
        [SerializeField] private AudioClip _ocean;
        [SerializeField] private AudioClip _shore;
        [SerializeField] private AudioClip[] _windLoops;
        [SerializeField] private AudioClip[] _hullLoops;
        [SerializeField] private AudioClip[] _slowWaves;
        [SerializeField] private AudioClip[] _mediumWaves;
        [SerializeField] private AudioClip[] _strongWaves;
        [SerializeField] private AudioClip[] _woodCreaks;
        [SerializeField] private AudioClip[] _helmTurns;
        [SerializeField] private AudioClip _raiseSail;
        [SerializeField] private AudioClip _lowerSail;
        [SerializeField] private AudioClip _reefHit;
        [SerializeField] private AudioClip _rockHit;
        [SerializeField] private AudioClip _debrisHit;

        private readonly System.Random _random = new();
        private AudioSource _oceanSource;
        private AudioSource _shoreSource;
        private AudioSource[] _winds;
        private AudioSource[] _hull;
        private AudioSource _portCreak;
        private AudioSource _starboardCreak;
        private BoatHealth _health;
        private SailPosition _previousSail;
        private bool _helmMoving;
        private float _nextHelm;
        private float _peakDescentSpeed;
        private float _nextWave;
        private float _nextCreak;

        private void Awake()
        {
            _oceanSource = CreateLoop(_ocean);
            _shoreSource = CreateLoop(_shore);
            _winds = System.Array.ConvertAll(_windLoops, CreateLoop);
            _hull = System.Array.ConvertAll(_hullLoops, CreateLoop);
            _portCreak = CreateCreakSource("Port Creak", -0.95f);
            _starboardCreak = CreateCreakSource("Starboard Creak", 0.95f);
            _health = _boat.Health;
        }

        private void OnEnable()
        {
            if (_health != null) _health.ObstacleHit += OnObstacleHit;
            _previousSail = _boat.SailPosition;
            _nextWave = Time.time;
            ScheduleCreak();
        }

        private void OnDisable()
        {
            if (_health != null) _health.ObstacleHit -= OnObstacleHit;
            Silence(_oceanSource);
            Silence(_shoreSource);
            foreach (AudioSource source in _winds) Silence(source);
            foreach (AudioSource source in _hull) Silence(source);
            _effects.Stop();
            _portCreak.Stop();
            _starboardCreak.Stop();
            _helmMoving = false;
            _peakDescentSpeed = 0f;
        }

        private AudioSource CreateLoop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.outputAudioMixerGroup = _ambience;
            return source;
        }

        private AudioSource CreateCreakSource(string name, float side)
        {
            var emitter = new GameObject(name);
            emitter.transform.SetParent(transform, false);
            emitter.transform.localPosition = new Vector3(side, 0f, 0f);

            var source = emitter.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _ambience;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            source.minDistance = 5f;
            source.maxDistance = 18f;
            return source;
        }

        private static void Silence(AudioSource source)
        {
            source.Stop();
            source.volume = 0f;
        }

        private static void Fade(AudioSource source, float level)
        {
            source.volume = Mathf.MoveTowards(source.volume, level, Time.unscaledDeltaTime * 0.2f);
            if (source.volume > 0f)
            {
                if (!source.isPlaying && source.clip != null) source.Play();
            }
            else if (source.isPlaying) source.Stop();
        }

        private void LateUpdate()
        {
            bool underway = _voyage.Voyaging && !_boat.IsRespawning;
            float speed = underway ? _boat.CurrentSpeed : 0f;
            WindController wind = _boat.Wind;
            float offshore = Mathf.InverseLerp(0.1f, 0.45f, wind != null ? wind.NormalizedDistance : 0f);
            Fade(_oceanSource, underway ? 0.018f : 0f);
            Fade(_shoreSource, underway ? 0.15f * (1f - offshore) : 0f);

            int windIndex = wind != null ? (int)wind.Type : (int)WindType.Calm;
            float sailGain = underway ? Mathf.Lerp(0.025f, 0.14f, _boat.SailDeployment) : 0f;
            for (int i = 0; i < _winds.Length; i++)
                Fade(_winds[i], i == windIndex ? sailGain : 0f);

            float moving = Mathf.InverseLerp(0.05f, 0.5f, speed);
            float hullBlend = Mathf.InverseLerp(0.7f, 1.8f, speed);
            Fade(_hull[0], 0.14f * moving * (1f - hullBlend));
            Fade(_hull[1], 0.14f * moving * hullBlend);

            if (underway)
            {
                PlayBoatControls();
                PlayPassingWave(speed);
                PlayCreak();
            }
            else _peakDescentSpeed = 0f;
            _previousSail = _boat.SailPosition;
        }

        private void PlayBoatControls()
        {
            if (_boat.SailPosition != _previousSail)
                Play(_boat.SailPosition > _previousSail ? _raiseSail : _lowerSail, 0.38f);

            bool turning = _helm.Active && Mathf.Abs(_helm.RudderAngularVelocity) > 8f;
            if (turning && !_helmMoving && Time.time >= _nextHelm)
            {
                PlayVariation(_helmTurns, 0.28f);
                _nextHelm = Time.time + 0.45f;
            }
            _helmMoving = turning;
        }

        private void PlayPassingWave(float speed)
        {
            if (speed < 0.12f || _buoyancy == null || !_buoyancy.isActiveAndEnabled)
            {
                _peakDescentSpeed = 0f;
                return;
            }

            float verticalSpeed = _buoyancy.VerticalSpeed;
            if (verticalSpeed < 0f)
            {
                _peakDescentSpeed = Mathf.Max(_peakDescentSpeed, -verticalSpeed);
                return;
            }

            // The hull has finished descending into a trough. Use the actual motion to choose the splash.
            const float minimumDescentSpeed = 0.035f;
            float descentSpeed = _peakDescentSpeed;
            _peakDescentSpeed = 0f;
            if (descentSpeed < minimumDescentSpeed || Time.time < _nextWave) return;
            AudioClip[] waves = descentSpeed >= 0.34f ? _strongWaves
                : descentSpeed >= 0.15f ? _mediumWaves : _slowWaves;
            float intensity = Mathf.InverseLerp(minimumDescentSpeed, 0.45f, descentSpeed);
            PlayVariation(waves, Mathf.Lerp(0.15f, 0.34f, intensity));
            _nextWave = Time.time + 1.25f;
        }

        private void PlayCreak()
        {
            if (Time.time < _nextCreak) return;
            AudioClip clip = PickClip(_woodCreaks);
            if (clip != null)
            {
                AudioSource source = _random.Next(2) == 0 ? _portCreak : _starboardCreak;
                source.PlayOneShot(clip, 0.23f);
            }
            ScheduleCreak();
        }

        private void ScheduleCreak() =>
            _nextCreak = Time.time + Mathf.Lerp(5f, 15f, (float)_random.NextDouble());

        internal void Play(AudioClip clip, float volume)
        {
            if (isActiveAndEnabled && clip != null) _effects.PlayOneShot(clip, volume);
        }

        internal void PlayVariation(AudioClip[] clips, float volume) => Play(PickClip(clips), volume);

        private AudioClip PickClip(AudioClip[] clips) =>
            clips != null && clips.Length > 0 ? clips[_random.Next(clips.Length)] : null;

        private void OnObstacleHit(BoatObstacleType type)
        {
            AudioClip clip = type switch
            {
                BoatObstacleType.Reef => _reefHit,
                BoatObstacleType.Rock => _rockHit,
                _ => _debrisHit
            };
            Play(clip, 0.55f);
        }
    }
}
