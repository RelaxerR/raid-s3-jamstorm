using SecondCatch.Application;
using SecondCatch.Stations;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Fishing
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FishSchool : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [SerializeField] private NetStation _net;
        [SerializeField] private string _fishId = FishCargo.AzureFish;
        [SerializeField] private Material _markerMaterial;
        private bool _takenOnPass;

        [Tooltip("Unique positive ID among the schools in HomeShallows.")]
        [SerializeField, Min(1)] private int _schoolId = 1;
        [SerializeField, Min(1)] private int _portion = 10;

        private void Awake()
        {
            if (_markerMaterial == null) return;
            foreach (Renderer marker in GetComponentsInChildren<Renderer>())
                marker.sharedMaterial = _markerMaterial;
        }

        private void Start()
        {
            if (_voyage != null && _voyage.WasSchoolCollected(_schoolId))
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other) => TryCollect(other);

        private void OnTriggerStay(Collider other) => TryCollect(other);

        private void OnTriggerExit(Collider other)
        {
            if (_voyage != null && _voyage.IsHull(other)) _takenOnPass = false;
        }

        public bool TryCollect(Collider contact)
        {
            if (_takenOnPass || !isActiveAndEnabled || _voyage == null || !_voyage.IsHull(contact)
                || _net == null || !_net.TryCapture(_schoolId, _portion, _fishId))
                return false;

            _takenOnPass = true;
            if (_voyage.WasSchoolCollected(_schoolId)) gameObject.SetActive(false);
            return true;
        }
    }
}
