using SecondCatch.Boat;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Application
{
    /// <summary>Связывает физическую лодку и сигналы рейса с одиночной сессией.</summary>
    public sealed class HomeShallowsController : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        private GameApplication _application;
        private SoloGameSession _session;
        private bool Voyaging => _session != null && _session.AtSea && !_application.IsLoading;

        private void Awake()
        {
            _application = GameApplication.Instance;
            _session = _application != null ? _application.Session : null;
            if (_session == null) return;
            // Сессия уже существует при загрузке моря. До завершения загрузки её команды закрыты.
            _boat.Health.BindVoyage(_session.Boat, ApplyDamage);
            _boat.PropulsionMultiplier = _session.Boat.SpeedMultiplier;
        }

        /// <summary>Принимает одну рыбу от источника улова и сообщает, поместилась ли она в бочку.</summary>
        public bool CollectFish() => Voyaging && _session.CatchFish(_session.Cargo.FishId);

        public void ReturnToPort()
        {
            if (Voyaging) _application.ReturnToPort();
        }

        private int ApplyDamage(int amount)
        {
            if (!Voyaging) return 0;
            int before = _session.Boat.Hp;
            if (!_session.Damage(amount)) return before - _session.Boat.Hp;

            _boat.IsRespawning = true;
            _boat.ResetMotion();
            _application.ReturnToPort();
            return before;
        }
    }
}
