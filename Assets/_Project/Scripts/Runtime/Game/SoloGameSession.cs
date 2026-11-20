namespace SecondCatch.Solo
{
    /// <summary>Единственный источник состояния текущей одиночной игры, без зависимости от сцен.</summary>
    public sealed class SoloGameSession
    {
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
            AtSea = true; Revision++; return true;
        }
        public bool Dock()
        {
            if (!AtSea) return false;
            AtSea = false; Revision++; return true;
        }
        public bool CatchFish(string fishId)
        {
            if (!AtSea || !Cargo.Add(fishId)) return false;
            Revision++; return true;
        }
        public bool Sell()
        {
            long income = (long)Cargo.Count * FISH_PRICE;
            if (AtSea || income == 0 || Coins > long.MaxValue - income) return false;
            Coins += income; Cargo.Clear(); Revision++; return true;
        }
        public bool BuySpeedUpgrade()
        {
            if (AtSea || Boat.SpeedUpgrade || Coins < UPGRADE_PRICE) return false;
            Coins -= UPGRADE_PRICE; Boat.SpeedUpgrade = true; Revision++; return true;
        }
        /// <summary>Возвращает true при гибели. Портовую сцену загружает контроллер рейса.</summary>
        public bool Damage(int amount)
        {
            if (!AtSea || amount <= 0) return false;
            bool sunk = Boat.Damage(amount);
            if (sunk) { Boat.Respawn(); Cargo.LoseHalf(); AtSea = false; }
            Revision++; return sunk;
        }
    }
}
