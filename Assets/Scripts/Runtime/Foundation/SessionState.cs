using System;

namespace SecondCatch.Foundation
{
    /// <summary>Этапы общего рейса лодки.</summary>
    public enum VoyagePhase { Port, Sailing, Hunting }
    /// <summary>Роли на станциях в RAID-срезе.</summary>
    public enum StationRole { Helm, Winch }
    /// <summary>Фазы охоты и её исходы в состоянии хоста.</summary>
    public enum HuntPhase { Idle, Run, Tire, Enrage, Won, Failed }

    /// <summary>Состояние лодки. Игровые изменения применяет сессия.</summary>
    public sealed class BoatState
    {
        #region Состояние
        public int HitPoints { get; internal set; } = 100;
        public int UpgradeLevel { get; internal set; }
        public float Throttle { get; internal set; }
        public float Steering { get; internal set; }
        #endregion
    }

    /// <summary>Идентификатор игрока, который сетевой адаптер сопоставляет с проверенным отправителем.</summary>
    public sealed class PlayerState
    {
        #region Состояние
        public string Id { get; }
        internal PlayerState(string id) { Id = id; }
        #endregion
    }

    /// <summary>Состояние станции с одним оператором под управлением хоста.</summary>
    public sealed class StationState
    {
        #region Состояние
        public StationRole Role { get; }
        public string OperatorId { get; internal set; }
        internal StationState(StationRole role) { Role = role; }
        #endregion
    }

    /// <summary>Трюм для одного вида рыбы с ограничением по количеству.</summary>
    public sealed class CargoHold
    {
        #region Состояние
        public int Count { get; internal set; }
        public int Capacity { get; }
        internal CargoHold(int capacity) { Capacity = capacity; }
        #endregion
    }

    /// <summary>Общий баланс лодки в целых монетах.</summary>
    public sealed class Wallet
    {
        #region Состояние
        public long Balance { get; internal set; }
        #endregion
    }

    /// <summary>Состояние охоты для интерфейса и будущей сетевой репликации.</summary>
    public sealed class HuntEncounter
    {
        #region Состояние
        public HuntPhase Phase { get; internal set; }
        public float Progress { get; internal set; }
        public float Tension { get; internal set; }
        public float PhaseTime { get; internal set; }
        internal bool EnrageTriggered { get; set; }
        #endregion
    }

    /// <summary>Неизменяемые параметры одного вида рыбы для экономики сессии.</summary>
    public sealed class FishDefinition
    {
        #region Параметры вида
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
