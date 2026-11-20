using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.Fishing
{
    /// <summary>Источник одной рыбы по сигналу проверенного контакта лодки. Не уничтожается при полном трюме.</summary>
    public sealed class FishSchool : MonoBehaviour
    {
        [SerializeField] private HomeShallowsController _voyage;
        private bool _collected;
        public bool TryCollect()
        {
            if (_collected || _voyage == null || !_voyage.CollectFish()) return false;
            _collected = true; gameObject.SetActive(false); return true;
        }
    }
}
