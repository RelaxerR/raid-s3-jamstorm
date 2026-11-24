using SecondCatch.Boat;
using SecondCatch.Player;
using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Application
{
    /// <summary>Связывает физическую лодку и сигналы рейса с одиночной сессией.</summary>
    public sealed class HomeShallowsController : MonoBehaviour
    {
        [SerializeField] private BoatController _boat;
        [SerializeField] private Collider _hull;
        [SerializeField] private BoatRepairController _repair;

        private PlayerController _player;
        private GameApplication _application;
        private SoloGameSession _session;
        public SoloGameSession Session => _session;
        public BoatRepairController Repair => _repair;

        public bool Voyaging => _session != null && _session.AtSea && !_application.IsLoading;

        private void Awake()
        {
            _application = GameApplication.Instance;
            _session = _application != null ? _application.Session : null;
            if (_session == null || _boat == null) return;
            // Сессия уже существует при загрузке моря. До завершения загрузки её команды закрыты.
            _player = _boat.GetComponentInChildren<PlayerController>();
            _boat.Health.BindVoyage(_session.Boat, ApplyDamage);
            _boat.PropulsionMultiplier = _session.Boat.SpeedMultiplier;
        }

        /// <summary>Принимает одну рыбу от источника улова и сообщает, поместилась ли она в бочку.</summary>
        public bool CollectFish() => Voyaging && _session.CatchFish(_session.Cargo.FishId);

        public bool IsHull(Collider contact) => contact != null && contact == _hull;

        public bool WasSchoolCollected(int schoolId) => _session != null && _session.WasSchoolCollected(schoolId);

        public void ReturnToPort()
        {
            if (Voyaging && _application.ReturnToPort()) _player?.ExitStation();
        }

        internal bool TryRepair(BoatZone zone)
        {
            if (!Voyaging || !_session.TryRepairZone(zone)) return false;
            _boat.Health.NotifyVoyageRepair();
            return true;
        }

        private int ApplyDamage(int amount, Vector3? worldImpact)
        {
            if (!Voyaging) return 0;
            int before = _session.Boat.Hp;
            BoatZone? zone = worldImpact.HasValue && _repair != null ? _repair.ZoneAt(worldImpact.Value) : null;
            if (!_session.Damage(amount, zone)) return before - _session.Boat.Hp;

            _boat.IsRespawning = true;
            _player?.ExitStation();
            _boat.ResetMotion();
            _application.ReturnToPort();
            return before;
        }
    }
}
