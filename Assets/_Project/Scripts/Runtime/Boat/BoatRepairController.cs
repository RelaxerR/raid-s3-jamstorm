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
        [SerializeField] private Zone[] _zones;
        [SerializeField] private Mesh _markMesh;
        [SerializeField] private Material _markMaterial;
        [SerializeField, Min(0.1f)] private float _radius = 0.9f;
        [SerializeField, Min(0.1f)] private float _holdSeconds = 3f;
        [SerializeField, Min(0f)] private float _promptHeight = 0.7f;

        private Zone _selected;
        private Zone _repairing;
        private float _elapsed;

        public BoatZone? SelectedZone => _selected?.Id;
        public bool HasWood => _voyage.Session?.Wood > 0;
        public bool IsRepairing => _repairing != null;
        public float Progress => Mathf.Clamp01(_elapsed / _holdSeconds);
        public Vector3 PromptPosition => transform.TransformPoint(_selected.LocalPoint) + Vector3.up * _promptHeight;

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
            _selected = FindAvailableZone();
            UpdateHold();
            UpdateMarks();
        }

        private Zone FindAvailableZone()
        {
            if (!_voyage.Voyaging || !_player.isActiveAndEnabled || !_player.Boat.isActiveAndEnabled
                || _player.Boat.IsRespawning || _player.ControlMode != PlayerControlMode.Walking
                || _player.GetAvailableStation() != null)
                return null;

            return FindNearest(_player.transform.position, true, _radius * _radius);
        }

        private Zone FindNearest(Vector3 worldPoint, bool damagedOnly, float bestDistance)
        {
            Vector3 point = transform.InverseTransformPoint(worldPoint);
            Zone nearest = null;
            foreach (var zone in _zones)
            {
                if (damagedOnly && _voyage.Session.Boat.GetZoneDamage(zone.Id) == 0) continue;
                Vector3 offset = zone.LocalPoint - point;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = zone;
            }
            return nearest;
        }

        private void UpdateHold()
        {
            if (_selected == null || !HasWood || !GameplayInput.InteractHeld || GameplayInput.CancelPressed)
            {
                ResetHold();
                return;
            }

            if (GameplayInput.InteractPressed)
            {
                _repairing = _selected;
                _elapsed = 0f;
                return;
            }

            if (_repairing != _selected)
            {
                ResetHold();
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < _holdSeconds) return;
            _voyage.TryRepair(_repairing.Id);
            _selected = FindAvailableZone();
            ResetHold();
        }

        private void UpdateMarks()
        {
            var boat = _voyage.Session?.Boat;
            if (boat == null) return;
            foreach (var zone in _zones)
            {
                if (zone.DamageMark == null) continue;
                bool damaged = boat.GetZoneDamage(zone.Id) > 0;
                if (zone.DamageMark.activeSelf != damaged) zone.DamageMark.SetActive(damaged);
            }
        }

        private void ResetHold()
        {
            _repairing = null;
            _elapsed = 0f;
        }

        private void OnDisable()
        {
            _selected = null;
            ResetHold();
        }
    }
}
