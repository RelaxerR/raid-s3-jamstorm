using System;
using System.Collections.Generic;

namespace SecondCatch.Foundation
{
    /// <summary>
    /// Shared host-owned gameplay model. Call commands serially from the Unity main thread.
    /// A network adapter must validate sender identity and state authority before calling this model.
    /// Clients consume replicated state and never instantiate an authoritative command receiver.
    /// </summary>
    public sealed class GameSession
    {
        #region State
        private readonly Dictionary<string, PlayerState> _players = new Dictionary<string, PlayerState>();
        private readonly int _upgradePrice;
        private readonly int _huntReward;
        public BoatState Boat { get; } = new BoatState();
        public CargoHold Cargo { get; }
        public Wallet Wallet { get; } = new Wallet();
        public HuntEncounter Hunt { get; } = new HuntEncounter();
        public StationState Helm { get; } = new StationState(StationRole.Helm);
        public StationState Winch { get; } = new StationState(StationRole.Winch);
        public VoyagePhase Voyage { get; private set; }
        public FishDefinition Fish { get; }
        public long Revision { get; private set; }

        /// <summary>Creates a fresh session in port using explicitly supplied balance values.</summary>
        public GameSession(FishDefinition fish, int capacity = 20, int upgradePrice = 100, int huntReward = 50)
        {
            Fish = fish ?? throw new ArgumentNullException(nameof(fish));
            if (capacity <= 0 || upgradePrice <= 0 || huntReward < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Cargo = new CargoHold(capacity);
            _upgradePrice = upgradePrice;
            _huntReward = huntReward;
        }
        #endregion

        #region Players and stations
        /// <summary>Registers a player before departure. Repeated registration has no effect.</summary>
        public bool Join(string playerId)
        {
            if (Voyage != VoyagePhase.Port || string.IsNullOrWhiteSpace(playerId) || _players.ContainsKey(playerId)) return false;
            _players.Add(playerId, new PlayerState(playerId));
            Revision++;
            return true;
        }

        /// <summary>Releases occupied stations on disconnect.</summary>
        public void Leave(string playerId)
        {
            if (playerId == null || !_players.Remove(playerId)) return;
            Release(playerId);
            Revision++;
        }

        /// <summary>Atomically moves a registered player to a free station.</summary>
        public bool Occupy(string playerId, StationRole role)
        {
            if (playerId == null || !_players.ContainsKey(playerId)) return false;
            if (role != StationRole.Helm && role != StationRole.Winch) return false;
            StationState station = role == StationRole.Helm ? Helm : Winch;
            if (station.OperatorId == playerId) return true;
            if (station.OperatorId != null) return false;
            Release(playerId);
            station.OperatorId = playerId;
            Revision++;
            return true;
        }

        /// <summary>Releases all stations occupied by this player and clears stale helm input.</summary>
        public void Release(string playerId)
        {
            if (playerId == null) return;
            if (Helm.OperatorId == playerId)
            {
                Helm.OperatorId = null;
                Boat.Throttle = 0;
                Boat.Steering = 0;
                Revision++;
            }
            if (Winch.OperatorId == playerId) { Winch.OperatorId = null; Revision++; }
        }

        /// <summary>Accepts finite normalized input from the current helm operator.</summary>
        public bool Steer(string playerId, float throttle, float steering)
        {
            if (playerId == null || Helm.OperatorId != playerId || Voyage == VoyagePhase.Port ||
                !Finite(throttle) || !Finite(steering)) return false;
            Boat.Throttle = Math.Max(-1, Math.Min(1, throttle));
            Boat.Steering = Math.Max(-1, Math.Min(1, steering));
            Revision++;
            return true;
        }
        #endregion

        #region Voyage and economy
        /// <summary>Begins a voyage from port when a player occupies the helm.</summary>
        public bool Depart()
        {
            if (Voyage != VoyagePhase.Port || Helm.OperatorId == null) return false;
            Voyage = VoyagePhase.Sailing;
            Revision++;
            return true;
        }

        /// <summary>Host port trigger ends a voyage. Active hunts cannot dock.</summary>
        public bool Dock()
        {
            if (Voyage != VoyagePhase.Sailing) return false;
            ReturnToPort();
            return true;
        }

        /// <summary>Host catch source inserts one fish only when sailing with room available.</summary>
        public bool CatchFish()
        {
            if (Voyage != VoyagePhase.Sailing || Cargo.Count >= Cargo.Capacity) return false;
            Cargo.Count++;
            Revision++;
            return true;
        }

        /// <summary>Sells the current cargo once. Repeated requests against empty cargo do nothing.</summary>
        public bool Sell()
        {
            if (Voyage != VoyagePhase.Port || Cargo.Count == 0) return false;
            long income = (long)Cargo.Count * Fish.Price;
            if (Wallet.Balance > long.MaxValue - income) return false;
            Wallet.Balance += income;
            Cargo.Count = 0;
            Revision++;
            return true;
        }

        /// <summary>Purchases the slice's single boat upgrade once.</summary>
        public bool BuyUpgrade()
        {
            if (Voyage != VoyagePhase.Port || Boat.UpgradeLevel != 0 || Wallet.Balance < _upgradePrice) return false;
            Wallet.Balance -= _upgradePrice;
            Boat.UpgradeLevel = 1;
            Revision++;
            return true;
        }
        #endregion

        #region Encounter
        /// <summary>Starts a fresh encounter. The host supplies collision and station actions.</summary>
        public bool StartHunt()
        {
            if (Voyage != VoyagePhase.Sailing) return false;
            Voyage = VoyagePhase.Hunting;
            Hunt.Phase = HuntPhase.Run;
            Hunt.Progress = Hunt.Tension = Hunt.PhaseTime = 0;
            Hunt.EnrageTriggered = false;
            Revision++;
            return true;
        }

        /// <summary>Advances one host simulation step; large catch-up intervals must be subdivided.</summary>
        public void Tick(float deltaTime, bool winchHeld)
        {
            if (Voyage != VoyagePhase.Hunting || !Finite(deltaTime) || deltaTime <= 0 || deltaTime > 0.25f) return;
            Hunt.PhaseTime += deltaTime;
            if (Hunt.Phase == HuntPhase.Tire && winchHeld && Winch.OperatorId != null)
                Hunt.Progress = Math.Min(100, Hunt.Progress + 10 * deltaTime);
            if (Hunt.Progress >= 100) { FinishHunt(true); return; }
            if (!Hunt.EnrageTriggered && Hunt.Progress >= 50)
            {
                Hunt.EnrageTriggered = true;
                Hunt.Phase = HuntPhase.Enrage;
                Hunt.PhaseTime = 0;
            }
            else if (Hunt.PhaseTime >= 4)
            {
                Hunt.Phase = Hunt.Phase == HuntPhase.Run ? HuntPhase.Tire : HuntPhase.Run;
                Hunt.PhaseTime = 0;
            }
            Revision++;
        }

        /// <summary>Applies host-calculated tension; overload ends the encounter.</summary>
        public void SetTension(float tension)
        {
            if (Voyage != VoyagePhase.Hunting || !Finite(tension) || tension < 0) return;
            Hunt.Tension = tension;
            Revision++;
            if (tension > 100) FinishHunt(false);
        }

        /// <summary>Applies host collision damage and returns a destroyed boat to port.</summary>
        public void Damage(int amount)
        {
            if (Voyage == VoyagePhase.Port || amount <= 0) return;
            Boat.HitPoints = Math.Max(0, Boat.HitPoints - amount);
            Revision++;
            if (Boat.HitPoints != 0) return;
            if (Voyage == VoyagePhase.Hunting) FinishHunt(false);
            else ReturnToPort();
        }

        private void FinishHunt(bool won)
        {
            Hunt.Phase = won ? HuntPhase.Won : HuntPhase.Failed;
            if (won) Wallet.Balance += Math.Min(_huntReward, long.MaxValue - Wallet.Balance);
            ReturnToPort();
        }

        private void ReturnToPort()
        {
            Voyage = VoyagePhase.Port;
            Boat.HitPoints = 100;
            Boat.Throttle = Boat.Steering = 0;
            Revision++;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        #endregion
    }
}
