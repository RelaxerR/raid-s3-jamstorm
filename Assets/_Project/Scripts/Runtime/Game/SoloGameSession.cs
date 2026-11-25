using System;
using System.Collections.Generic;

namespace SecondCatch.Solo
{
    public enum DeckFishStoreResult { Unavailable, Stored, Full, Incompatible }

    /// <summary>Единственный источник состояния текущей одиночной игры, без зависимости от сцен.</summary>
    public sealed class SoloGameSession
    {
        private readonly HashSet<int> _collectedSchools = new();
        private readonly Dictionary<int, int> _schoolCatch = new();

        private readonly int[] _netFish = new int[FishCargo.SpeciesCount];
        private readonly int[] _deckFish = new int[FishCargo.SpeciesCount];

        public int NetFishCount => _netFish[0] + _netFish[1];
        public int DeckFishCount => _deckFish[0] + _deckFish[1];

        public int DeckCount(string fishId)
        {
            int species = FishCargo.SpeciesIndex(fishId);
            return species < 0 ? 0 : _deckFish[species];
        }

        public BoatState Boat { get; } = new BoatState();
        public FishCargo Cargo { get; } = new FishCargo();
        public int UniqueFishCount { get; private set; }
        public int UniqueFishCapacity => Boat.UniqueFishCapacity;
        public bool UniqueFishFull => UniqueFishCount >= UniqueFishCapacity;
        public bool UniqueFishCaughtThisVoyage { get; private set; }
        public bool AtSea { get; private set; }
        public long Coins { get; private set; }
        public int Wood { get; private set; }
        public int WoodPrice { get; }
        public long Revision { get; private set; }
        public const int FISH_PRICE = 10;
        public const string UNIQUE_FISH_ID = "fish.crimson";
        public const int UNIQUE_FISH_PRICE = 50;
        public const int UPGRADE_PRICE = 100;
        public const int DEFAULT_WOOD_PRICE = 10;

        public SoloGameSession(int woodPrice = DEFAULT_WOOD_PRICE)
        {
            WoodPrice = Math.Max(1, woodPrice);
        }

        public bool Depart()
        {
            if (AtSea) return false;
            _collectedSchools.Clear();
            _schoolCatch.Clear();
            UniqueFishCaughtThisVoyage = false;
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
            int accepted = CatchFish(FishCargo.AzureFish, requested);
            if (accepted > 0) _collectedSchools.Add(schoolId);
            return accepted;
        }

        public int TakeSchoolIntoNet(int schoolId, int portion, int netCapacity, string fishId = FishCargo.AzureFish)
        {
            int species = FishCargo.SpeciesIndex(fishId);
            if (!AtSea || species < 0 || schoolId <= 0 || WasSchoolCollected(schoolId)) return 0;
            _schoolCatch.TryGetValue(schoolId, out int taken);
            int accepted = Math.Min(portion - taken, netCapacity - NetFishCount);
            if (accepted <= 0) return 0;

            taken += accepted;
            _schoolCatch[schoolId] = taken;
            if (taken == portion) _collectedSchools.Add(schoolId);
            _netFish[species] += accepted;
            Revision++;
            return accepted;
        }

        public void SpillNetFish(int count)
        {
            int lost = Math.Min(count, NetFishCount);
            if (!AtSea || lost <= 0) return;
            // Keep the same total spill and distribute it according to the catch in the net.
            int azureLost = (int)(((long)lost * _netFish[0] + NetFishCount / 2) / NetFishCount);
            _netFish[0] -= azureLost;
            _netFish[1] -= lost - azureLost;
            Revision++;
        }

        public void LandNetCatch()
        {
            if (!AtSea || NetFishCount == 0) return;
            for (int species = 0; species < _netFish.Length; species++)
            {
                _deckFish[species] += _netFish[species];
                _netFish[species] = 0;
            }
            Revision++;
        }

        public DeckFishStoreResult StoreDeckFish(string fishId)
        {
            if (!AtSea || DeckFishCount == 0) return DeckFishStoreResult.Unavailable;
            int species = FishCargo.SpeciesIndex(fishId);
            if (species < 0) return DeckFishStoreResult.Incompatible;
            if (_deckFish[species] == 0) return DeckFishStoreResult.Unavailable;
            if (!CatchFish(fishId)) return DeckFishStoreResult.Full;
            _deckFish[species]--;
            return DeckFishStoreResult.Stored;
        }

        public bool ReleaseBarrel(int index)
        {
            FishBarrel barrel = Cargo.GetBarrel(index);
            if (!AtSea || barrel == null || barrel.Count == 0) return false;
            barrel.Remove(barrel.Count);
            Revision++;
            return true;
        }

        private void ClearLooseFish()
        {
            Array.Clear(_netFish, 0, _netFish.Length);
            Array.Clear(_deckFish, 0, _deckFish.Length);
        }

        public bool TryStowUniqueFish()
        {
            if (!AtSea || UniqueFishFull || UniqueFishCaughtThisVoyage) return false;
            UniqueFishCount++;
            UniqueFishCaughtThisVoyage = true;
            Revision++;
            return true;
        }

        public bool CanSell => !AtSea && SaleValue > 0 && Coins <= long.MaxValue - SaleValue;
        public long SaleValue => (long)Cargo.Count * FISH_PRICE + UniqueSaleValue;
        private long UniqueSaleValue => (long)UniqueFishCount * UNIQUE_FISH_PRICE;
        public bool CanBuySpeedUpgrade => !AtSea && !Boat.SpeedUpgrade && Coins >= UPGRADE_PRICE;
        public bool CanBuyWood => !AtSea && Coins >= WoodPrice && Wood < int.MaxValue;

        public bool Sell()
        {
            if (!CanSell) return false;
            Coins += SaleValue;
            Cargo.Clear();
            UniqueFishCount = 0;
            Revision++;
            return true;
        }

        public bool CanSellFish(string fishId) => fishId == UNIQUE_FISH_ID
            ? CanSellUnique
            : CanSellBarrel(Cargo.FindBarrel(fishId));

        private bool CanSellUnique => !AtSea && UniqueFishCount > 0 && Coins <= long.MaxValue - UniqueSaleValue;

        public bool SellFish(string fishId)
        {
            if (fishId == UNIQUE_FISH_ID)
            {
                if (!CanSellUnique) return false;
                Coins += UniqueSaleValue;
                UniqueFishCount = 0;
                Revision++;
                return true;
            }

            FishBarrel barrel = Cargo.FindBarrel(fishId);
            if (!CanSellBarrel(barrel)) return false;
            Coins += (long)barrel.Count * FISH_PRICE;
            barrel.Remove(barrel.Count);
            Revision++;
            return true;
        }

        private bool CanSellBarrel(FishBarrel barrel) =>
            !AtSea && barrel != null && Coins <= long.MaxValue - (long)barrel.Count * FISH_PRICE;

        public bool BuySpeedUpgrade()
        {
            if (!CanBuySpeedUpgrade) return false;
            Coins -= UPGRADE_PRICE; Boat.SpeedUpgrade = true; Revision++; return true;
        }

        public bool BuyWood()
        {
            if (!CanBuyWood) return false;
            Coins -= WoodPrice;
            Wood++;
            Revision++;
            return true;
        }

        public bool TrySpendWood()
        {
            if (Wood == 0) return false;
            Wood--;
            Revision++;
            return true;
        }

        /// <summary>Возвращает true при гибели. Портовую сцену загружает контроллер рейса.</summary>
        public bool Damage(int amount, BoatZone? zone = null)
        {
            if (!AtSea || amount <= 0 || (zone.HasValue && !BoatState.IsZone(zone.Value))) return false;
            if (Boat.Damage(amount, zone) == 0) return false;
            bool sunk = Boat.Hp == 0;
            if (sunk)
            {
                Boat.Respawn();
                Cargo.LoseHalf();
                UniqueFishCount /= 2;
                ClearLooseFish();
                AtSea = false;
            }
            Revision++; return sunk;
        }

        public bool TryRepairZone(BoatZone zone)
        {
            if (!AtSea || !Boat.IsZoneDamaged(zone) || !TrySpendWood()) return false;
            Boat.Repair(zone);
            return true;
        }
    }
}
