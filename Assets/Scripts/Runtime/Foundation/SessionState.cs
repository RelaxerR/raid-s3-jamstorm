using System;

namespace SecondCatch.Foundation
{
    /// <summary>Lifecycle of the shared boat voyage.</summary>
    public enum VoyagePhase { Port, Sailing, Hunting }
    /// <summary>Roles available in the RAID slice.</summary>
    public enum StationRole { Helm, Winch }
    /// <summary>Authoritative encounter phases, including terminal outcomes.</summary>
    public enum HuntPhase { Idle, Run, Tire, Enrage, Won, Failed }

    /// <summary>Read-only boat state. Only the session applies gameplay changes.</summary>
    public sealed class BoatState
    {
        #region State
        public int HitPoints { get; internal set; } = 100;
        public int UpgradeLevel { get; internal set; }
        public float Throttle { get; internal set; }
        public float Steering { get; internal set; }
        #endregion
    }

    /// <summary>Stable player identity mapped by the transport adapter to its authenticated sender.</summary>
    public sealed class PlayerState
    {
        #region State
        public string Id { get; }
        internal PlayerState(string id) { Id = id; }
        #endregion
    }

    /// <summary>Exclusive station occupancy, owned by the host session.</summary>
    public sealed class StationState
    {
        #region State
        public StationRole Role { get; }
        public string OperatorId { get; internal set; }
        internal StationState(StationRole role) { Role = role; }
        #endregion
    }

    /// <summary>Single-species cargo and its quantity limit for the initial integration slice.</summary>
    public sealed class CargoHold
    {
        #region State
        public int Count { get; internal set; }
        public int Capacity { get; }
        internal CargoHold(int capacity) { Capacity = capacity; }
        #endregion
    }

    /// <summary>Shared integer currency balance for the entire boat.</summary>
    public sealed class Wallet
    {
        #region State
        public long Balance { get; internal set; }
        #endregion
    }

    /// <summary>Encounter state exposed to presentation and future replication.</summary>
    public sealed class HuntEncounter
    {
        #region State
        public HuntPhase Phase { get; internal set; }
        public float Progress { get; internal set; }
        public float Tension { get; internal set; }
        public float PhaseTime { get; internal set; }
        internal bool EnrageTriggered { get; set; }
        #endregion
    }

    /// <summary>Immutable single-species economy settings copied from an asset at session creation.</summary>
    public sealed class FishDefinition
    {
        #region Definition
        public string Id { get; }
        public int Price { get; }
        public float WeightKg { get; }

        public FishDefinition(string id, int price, float weightKg)
        {
            if (string.IsNullOrWhiteSpace(id) || price < 0 || float.IsNaN(weightKg) ||
                float.IsInfinity(weightKg) || weightKg <= 0)
                throw new ArgumentException("Fish requires an ID, non-negative price and finite positive weight.");
            Id = id;
            Price = price;
            WeightKg = weightKg;
        }
        #endregion
    }
}
