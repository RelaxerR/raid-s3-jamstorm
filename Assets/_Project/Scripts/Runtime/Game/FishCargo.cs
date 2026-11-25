using System;

namespace SecondCatch.Solo
{
    public sealed class FishBarrel
    {
        public string FishId { get; private set; }
        public int Capacity => 100;
        public int Count { get; private set; }

        internal int Add(string fishId, int requested)
        {
            int accepted = Math.Min(requested, Capacity - Count);
            if (accepted == 0) return 0;
            FishId = fishId;
            Count += accepted;
            return accepted;
        }

        internal void Remove(int count)
        {
            Count -= count;
            if (Count == 0) FishId = null;
        }
    }

    public sealed class FishCargo
    {
        public const string AzureFish = "fish.greybox";
        public const string AmberFish = "fish.amber";
        public const int SpeciesCount = 2;

        public FishBarrel First { get; } = new();
        public FishBarrel Second { get; } = new();
        public int Capacity => First.Capacity + Second.Capacity;
        public int Count => First.Count + Second.Count;
        public bool IsFull => Count == Capacity;

        public FishBarrel GetBarrel(int index) => index switch { 0 => First, 1 => Second, _ => null };

        public static int SpeciesIndex(string fishId) => fishId switch
        {
            AzureFish => 0,
            AmberFish => 1,
            _ => -1
        };

        public static string SpeciesName(string fishId) => fishId switch
        {
            AzureFish => "Лазурная",
            AmberFish => "Янтарная",
            _ => "пусто"
        };

        public FishBarrel FindBarrel(string fishId) =>
            fishId == null ? null : First.FishId == fishId ? First : Second.FishId == fishId ? Second : null;

        internal int Add(string fishId, int requested)
        {
            if (SpeciesIndex(fishId) < 0 || requested <= 0) return 0;
            FishBarrel barrel = FindBarrel(fishId) ?? (First.Count == 0 ? First : Second);
            return barrel.Add(fishId, requested);
        }

        internal void Clear()
        {
            First.Remove(First.Count);
            Second.Remove(Second.Count);
        }

        internal void LoseHalf()
        {
            int firstLoss = (First.Count + 1) / 2;
            int totalLoss = (Count + 1) / 2;
            First.Remove(firstLoss);
            Second.Remove(totalLoss - firstLoss);
        }
    }
}
