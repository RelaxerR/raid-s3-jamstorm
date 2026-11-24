using System;
using SecondCatch.Application;
using SecondCatch.Input;
using SecondCatch.Player;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.Rendering;

namespace SecondCatch.Boat
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class BoatRepairController : MonoBehaviour
    {
        [Serializable]
        private sealed class Zone
        {
            public BoatZone Id;
            public Vector3 LocalPoint;
            public Vector3 MarkSize;
            [NonSerialized] public GameObject DamageMark;
        }

        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private PlayerController _player;
        [SerializeField] private Transform _repairVisual;
        [SerializeField] private Zone[] _zones;
        [SerializeField] private Mesh _markMesh;
        [SerializeField] private Material _markMaterial;
        [SerializeField, Min(0.1f)] private float _radius = 0.9f;
        [SerializeField, Min(0.1f)] private float _holdSeconds = 3f;
        [SerializeField, Min(0f)] private float _promptHeight = 0.7f;

        private Zone _selected;
        private Zone _repairing;
        private float _elapsed;
        private Vector3 _visualRestPosition;
        private Quaternion _visualRestRotation;

        public BoatZone? SelectedZone => _selected?.Id;
        public bool HasWood => _voyage.Session?.Wood > 0;
        public bool IsRepairing => _repairing != null;
        public float Progress => Mathf.Clamp01(_elapsed / _holdSeconds);
        public Vector3 PromptPosition => transform.TransformPoint(_selected.LocalPoint) + Vector3.up * _promptHeight;

        private void Awake()
        {
            if (_repairVisual == null) return;
            _visualRestPosition = _repairVisual.localPosition;
            _visualRestRotation = _repairVisual.localRotation;
        }

        private void Start()
        {
            if (_voyage.Session == null) return;

            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(0.4f, 0.045f, 0.035f));
            foreach (var zone in _zones)
            {
                var mark = new GameObject(zone.Id + "Damage", typeof(MeshFilter), typeof(MeshRenderer));
                mark.transform.SetParent(transform, false);
                mark.transform.localPosition = zone.LocalPoint;
                mark.transform.localScale = zone.MarkSize;
                mark.GetComponent<MeshFilter>().sharedMesh = _markMesh;
                var renderer = mark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _markMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.SetPropertyBlock(properties);
                mark.SetActive(false);
                zone.DamageMark = mark;
            }
        }

        internal BoatZone ZoneAt(Vector3 worldPoint) =>
            FindNearest(worldPoint, false, float.PositiveInfinity)?.Id ?? BoatZone.Mast;

        private void Update()
        {
            string unavailable = DeckUnavailableReason();
            _selected = unavailable == null ? FindAvailableZone() : null;
            UpdateHold(unavailable);
            UpdateMarks();
        }

        private string DeckUnavailableReason()
        {
            if (!_voyage.Voyaging) return "voyage_unavailable";
            if (!_player.isActiveAndEnabled || !_player.Boat.isActiveAndEnabled || _player.Boat.IsRespawning)
                return "deck_unavailable";
            if (_player.ControlMode != PlayerControlMode.Walking || _player.GetAvailableStation() != null)
                return "station";
            return null;
        }

        private Zone FindAvailableZone() => FindNearest(_player.transform.position, true, _radius * _radius);

        private Zone FindNearest(Vector3 worldPoint, bool damagedOnly, float bestDistance)
        {
            Vector3 point = transform.InverseTransformPoint(worldPoint);
            Zone nearest = null;
            foreach (var zone in _zones)
            {
                if (damagedOnly && !_voyage.Session.Boat.IsZoneDamaged(zone.Id)) continue;
                Vector3 offset = zone.LocalPoint - point;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = zone;
            }
            return nearest;
        }

        private void UpdateHold(string unavailable)
        {
            if (_repairing == null)
            {
                if (_selected == null || !GameplayInput.InteractPressed || !GameplayInput.InteractHeld
                    || GameplayInput.CancelPressed) return;
                if (!HasWood)
                {
                    LogTransition(_selected.Id, "rejected", "no_wood", 0);
                    return;
                }
                _repairing = _selected;
                AnimateRepair();
                LogTransition(_repairing.Id, "started", null, 0);
                return;
            }

            string interrupted = unavailable ?? InterruptionReason();
            if (interrupted != null)
            {
                LogTransition(_repairing.Id, "interrupted", interrupted, 0);
                ResetHold();
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < _holdSeconds)
            {
                AnimateRepair();
                return;
            }

            BoatZone zone = _repairing.Id;
            int woodBefore = _voyage.Session.Wood;
            bool completed = _voyage.TryRepair(zone);
            ResetHold();
            _selected = FindAvailableZone();
            LogTransition(zone, completed ? "completed" : "rejected",
                completed ? null : "command_rejected", woodBefore - _voyage.Session.Wood);
        }

        private string InterruptionReason()
        {
            if (GameplayInput.CancelPressed) return "cancel";
            if (!GameplayInput.InteractHeld) return "released";
            if (!HasWood) return "no_wood";
            if (!_voyage.Session.Boat.IsZoneDamaged(_repairing.Id)) return "zone_restored";
            if (_selected == null) return "out_of_range";
            return _selected != _repairing ? "zone_changed" : null;
        }

        private void AnimateRepair()
        {
            if (_repairVisual == null) return;
            Vector3 target = transform.TransformPoint(_repairing.LocalPoint);
            Vector3 direction = _repairVisual.parent.InverseTransformPoint(target) - _visualRestPosition;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            float stroke = 0.5f + 0.5f * Mathf.Sin(_elapsed * (2f * Mathf.PI / 0.42f));
            _repairVisual.localPosition = _visualRestPosition + direction * (0.12f + 0.12f * stroke)
                + Vector3.down * (0.12f + 0.24f * stroke);
            _repairVisual.localRotation = Quaternion.AngleAxis(12f + 16f * stroke,
                Vector3.Cross(Vector3.up, direction)) * _visualRestRotation;
        }

        private void UpdateMarks()
        {
            var boat = _voyage.Session?.Boat;
            if (boat == null) return;
            foreach (var zone in _zones)
            {
                if (zone.DamageMark == null) continue;
                bool damaged = boat.IsZoneDamaged(zone.Id);
                if (zone.DamageMark.activeSelf != damaged) zone.DamageMark.SetActive(damaged);
            }
        }

        private void ResetHold()
        {
            _repairing = null;
            _elapsed = 0f;
            if (_repairVisual == null) return;
            _repairVisual.SetLocalPositionAndRotation(_visualRestPosition, _visualRestRotation);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogTransition(BoatZone zone, string state, string reason, int woodSpent) =>
            Debug.Log($"[BoatRepair] zone={zone} state={state} reason={reason ?? "-"} woodSpent={woodSpent} wood={_voyage.Session?.Wood}", this);

        private void OnDisable()
        {
            _selected = null;
            if (_repairing == null) return;
            LogTransition(_repairing.Id, "interrupted", "disabled", 0);
            ResetHold();
        }
    }
}
