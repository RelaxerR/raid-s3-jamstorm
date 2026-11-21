using SecondCatch.Application;
using UnityEngine;

namespace SecondCatch.Fishing
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FishSchool : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        [Tooltip("Unique positive ID among the schools in HomeShallows.")]
        [SerializeField, Min(1)] private int _schoolId = 1;
        [SerializeField, Min(1)] private int _portion = 10;

        private void Start()
        {
            if (_voyage != null && _voyage.WasSchoolCollected(_schoolId))
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other) => TryCollect(other);

        public bool TryCollect(Collider contact)
        {
            if (!isActiveAndEnabled || _voyage == null || !_voyage.IsHull(contact)
                || _voyage.CollectSchool(_schoolId, _portion) == 0)
                return false;

            gameObject.SetActive(false);
            return true;
        }
    }
}
