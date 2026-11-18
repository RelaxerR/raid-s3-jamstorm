using System;
namespace SecondCatch.Solo
{
    /// <summary>Одна типизированная бочка серого прототипа; расширяется до набора бочек.</summary>
    public sealed class FishCargo
    {
        public string FishId { get; } = "fish.greybox";
        public int Capacity { get; } = 100;
        public int Count { get; private set; }
        public bool IsFull => Count == Capacity;
        internal bool Add(string fishId)
        {
            if (fishId != FishId || IsFull) return false;
            Count++;
            return true;
        }
        internal void Clear() { Count = 0; }
        // При нечётном количестве теряется округлённая вверх половина.
        internal void LoseHalf() { Count /= 2; }
    }
}
