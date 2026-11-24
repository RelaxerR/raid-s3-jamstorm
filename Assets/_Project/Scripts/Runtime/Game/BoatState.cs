using System;

namespace SecondCatch.Solo
{
    public enum BoatZone { FrontLeft, FrontRight, RearLeft, RearRight, Mast }

    /// <summary>Прочность и купленное усиление одиночной лодки.</summary>
    public sealed class BoatState
    {
        private static readonly int ZoneCount = Enum.GetValues(typeof(BoatZone)).Length;
        private readonly int[] _zoneDamage = new int[ZoneCount];

        public int MaxHp { get; } = 60;
        public int Hp { get; private set; } = 60;
        public bool SpeedUpgrade { get; internal set; }
        public float SpeedMultiplier => SpeedUpgrade ? 1.4f : 1f;

        public int GetZoneDamage(BoatZone zone) => IsZone(zone) ? _zoneDamage[(int)zone] : 0;

        internal static bool IsZone(BoatZone zone) => (uint)zone < ZoneCount;

        internal bool Damage(int amount, BoatZone zone)
        {
            int applied = Math.Min(Hp, amount);
            _zoneDamage[(int)zone] += applied;
            Hp -= applied;
            return Hp == 0;
        }

        internal void Repair(BoatZone zone)
        {
            Hp += _zoneDamage[(int)zone];
            _zoneDamage[(int)zone] = 0;
        }

        internal void Respawn()
        {
            Hp = (int)Math.Ceiling(MaxHp * 0.3);
            int missing = MaxHp - Hp;
            for (int i = 0; i < ZoneCount; i++)
                _zoneDamage[i] = missing / ZoneCount + (i < missing % ZoneCount ? 1 : 0);
        }
    }
}
