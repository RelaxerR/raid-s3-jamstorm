using System;
namespace SecondCatch.Solo
{
    /// <summary>Прочность и купленное усиление одиночной лодки.</summary>
    public sealed class BoatState
    {
        public int MaxHp { get; } = 60;
        public int Hp { get; private set; } = 60;
        public bool SpeedUpgrade { get; internal set; }
        public float SpeedMultiplier => SpeedUpgrade ? 1.4f : 1f;
        internal bool Damage(int amount)
        {
            if (amount <= 0) return false;
            Hp = Math.Max(0, Hp - amount);
            return Hp == 0;
        }
        internal void Respawn() { Hp = (int)Math.Ceiling(MaxHp * 0.3); }
    }
}
