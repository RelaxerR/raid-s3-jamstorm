using System.Collections.Generic;

namespace SecondCatch.Solo
{
    public enum DeckFishStoreResult { Unavailable, Stored, Full, Incompatible }

    /// <summary>Единственный источник состояния текущей одиночной игры, без зависимости от сцен.</summary>
    public sealed class SoloGameSession
    {
        private readonly HashSet<int> _collectedSchools = new();
        private readonly Dictionary<int, int> _schoolCatch = new();

        public int NetFishCount { get; private set; }
        public int DeckFishCount { get; private set; }

        public BoatState Boat { get; } = new BoatState();
        public FishCargo Cargo { get; } = new FishCargo();
        public bool AtSea { get; private set; }
        public long Coins { get; private set; }
        public long Revision { get; private set; }
        public const int FISH_PRICE = 10;
        public const int UPGRADE_PRICE = 100;
        public bool Depart()
        {
            if (AtSea) return false;
            _collectedSchools.Clear();
            _schoolCatch.Clear();
            AtSea = true; Revision++; return true;
        }
        public bool Dock()
        {
            if (!AtSea) return false;
            ClearLooseFish();
            AtSea = false; Revision++; return true;
        }
        public bool CatchFish(string fishId) => CatchFish(fishId, 1) == 1;

        private int CatchFish(string fishId, int requested)
        {
            if (!AtSea) return 0;
            int accepted = Cargo.Add(fishId, requested);
            if (accepted > 0) Revision++;
            return accepted;
        }

        public bool WasSchoolCollected(int schoolId) => _collectedSchools.Contains(schoolId);

        public int CatchSchool(int schoolId, int requested)
        {
            if (schoolId <= 0 || WasSchoolCollected(schoolId)) return 0;
            int accepted = CatchFish(Cargo.FishId, requested);
            if (accepted > 0) _collectedSchools.Add(schoolId);
            return accepted;
        }

        public int TakeSchoolIntoNet(int schoolId, int portion, int netCapacity)
        {
            if (!AtSea || schoolId <= 0 || WasSchoolCollected(schoolId)) return 0;
            _schoolCatch.TryGetValue(schoolId, out int taken);
            int accepted = System.Math.Min(portion - taken, netCapacity - NetFishCount);
            if (accepted <= 0) return 0;

            taken += accepted;
            _schoolCatch[schoolId] = taken;
            if (taken == portion) _collectedSchools.Add(schoolId);
            NetFishCount += accepted;
            Revision++;
            return accepted;
        }

        public void SpillNetFish(int count)
        {
            int lost = System.Math.Min(count, NetFishCount);
            if (!AtSea || lost <= 0) return;
            NetFishCount -= lost;
            Revision++;
        }

        public void LandNetCatch()
        {
            if (!AtSea || NetFishCount == 0) return;
            DeckFishCount += NetFishCount;
            NetFishCount = 0;
            Revision++;
        }

        public DeckFishStoreResult StoreDeckFish(string fishId)
        {
            if (!AtSea || DeckFishCount == 0) return DeckFishStoreResult.Unavailable;
            if (fishId != Cargo.FishId) return DeckFishStoreResult.Incompatible;
            if (!CatchFish(fishId)) return DeckFishStoreResult.Full;
            DeckFishCount--;
            return DeckFishStoreResult.Stored;
        }

        private void ClearLooseFish()
        {
            NetFishCount = 0;
            DeckFishCount = 0;
        }

        public bool CanSell => !AtSea && Cargo.Count > 0 && Coins <= long.MaxValue - SaleValue;
        public long SaleValue => (long)Cargo.Count * FISH_PRICE;
        public bool CanBuySpeedUpgrade => !AtSea && !Boat.SpeedUpgrade && Coins >= UPGRADE_PRICE;

        public bool Sell()
        {
            if (!CanSell) return false;
            Coins += SaleValue;
            Cargo.Clear();
            Revision++;
            return true;
        }

        public bool SellFish(string fishId) => fishId == Cargo.FishId && Sell();

        public bool BuySpeedUpgrade()
        {
            if (!CanBuySpeedUpgrade) return false;
            Coins -= UPGRADE_PRICE; Boat.SpeedUpgrade = true; Revision++; return true;
        }
        /// <summary>Возвращает true при гибели. Портовую сцену загружает контроллер рейса.</summary>
        public bool Damage(int amount)
        {
            if (!AtSea || amount <= 0) return false;
            bool sunk = Boat.Damage(amount);
            if (sunk) { Boat.Respawn(); Cargo.LoseHalf(); ClearLooseFish(); AtSea = false; }
            Revision++; return sunk;
        }
    }
}
