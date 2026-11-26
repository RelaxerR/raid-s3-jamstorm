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
        private BoatHealth _health;
        private SailPosition _previousSail;
        private bool _helmMoving;
        private float _nextHelm;
        private float _nextWave;
        private float _nextCreak;

        private void Awake()
        {
            _oceanSource = CreateLoop(_ocean);
            _shoreSource = CreateLoop(_shore);
            _winds = System.Array.ConvertAll(_windLoops, CreateLoop);
            _hull = System.Array.ConvertAll(_hullLoops, CreateLoop);
            _health = _boat.Health;
        }

        private void OnEnable()
        {
            if (_health != null) _health.ObstacleHit += OnObstacleHit;
            _previousSail = _boat.SailPosition;
            _nextWave = Time.time + 2f;
            _nextCreak = Time.time + 5f;
        }

        private void OnDisable()
        {
            if (_health != null) _health.ObstacleHit -= OnObstacleHit;
            Silence(_oceanSource);
            Silence(_shoreSource);
            foreach (AudioSource source in _winds) Silence(source);
            foreach (AudioSource source in _hull) Silence(source);
            _effects.Stop();
            _helmMoving = false;
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
            Fade(_oceanSource, underway ? Mathf.Lerp(0.07f, 0.14f, offshore) : 0f);
            Fade(_shoreSource, underway ? 0.15f * (1f - offshore) : 0f);

            int windIndex = wind != null ? (int)wind.Type : (int)WindType.Calm;
            float sailGain = underway ? Mathf.Lerp(0.025f, 0.14f, _boat.SailDeployment) : 0f;
            for (int i = 0; i < _winds.Length; i++)
                Fade(_winds[i], i == windIndex ? sailGain : 0f);

            float moving = Mathf.InverseLerp(0.05f, 0.5f, speed);
            float hullBlend = Mathf.InverseLerp(0.7f, 1.8f, speed);
            Fade(_hull[0], 0.14f * moving * (1f - hullBlend));
            Fade(_hull[1], 0.14f * moving * hullBlend);

            if (underway) PlayBoatMotion(speed, windIndex);
            _previousSail = _boat.SailPosition;
        }

        private void PlayBoatMotion(float speed, int windIndex)
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

            if (Time.time >= _nextWave)
            {
                AudioClip[] waves = windIndex == (int)WindType.Storm ? _strongWaves
                    : speed >= 1.2f ? _mediumWaves : _slowWaves;
                PlayVariation(waves, 0.23f);
                _nextWave = Time.time + RandomInterval(2.8f, 5f);
            }

            if (Time.time >= _nextCreak)
            {
                if (turning || speed >= 0.5f || windIndex == (int)WindType.Storm)
                    PlayVariation(_woodCreaks, 0.20f);
                _nextCreak = Time.time + RandomInterval(5f, 9f);
            }
        }

        internal void Play(AudioClip clip, float volume)
        {
            if (isActiveAndEnabled && clip != null) _effects.PlayOneShot(clip, volume);
        }

        internal void PlayVariation(AudioClip[] clips, float volume)
        {
            if (clips != null && clips.Length > 0) Play(clips[_random.Next(clips.Length)], volume);
        }

        private float RandomInterval(float minimum, float maximum) =>
            Mathf.Lerp(minimum, maximum, (float)_random.NextDouble());

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
