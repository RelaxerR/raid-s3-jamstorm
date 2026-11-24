using System;

namespace SecondCatch.Solo
{
    public enum BoatZone { FrontLeft, FrontRight, RearLeft, RearRight, Mast }

    /// <summary>Прочность и купленное усиление одиночной лодки.</summary>
    public sealed class BoatState
    {
        private static readonly (int MaxHp, int DamageThreshold)[] ZoneSpecs =
        {
            (25, 5), // FrontLeft
            (25, 5), // FrontRight
            (25, 5), // RearLeft
            (25, 5), // RearRight
            (25, 5)  // Mast
        };
        private readonly int[] _zoneHp = Array.ConvertAll(ZoneSpecs, spec => spec.MaxHp);

        public int MaxHp { get; } = 60;
        public int Hp { get; private set; } = 60;
        public bool SpeedUpgrade { get; internal set; }
        public float SpeedMultiplier => SpeedUpgrade ? 1.4f : 1f;

        public int GetZoneMaxHp(BoatZone zone) => IsZone(zone) ? ZoneSpecs[(int)zone].MaxHp : 0;
        public int GetZoneHp(BoatZone zone) => IsZone(zone) ? _zoneHp[(int)zone] : 0;
        public int GetZoneDamage(BoatZone zone) => GetZoneMaxHp(zone) - GetZoneHp(zone);
        public bool IsZoneDamaged(BoatZone zone) =>
            IsZone(zone) && GetZoneDamage(zone) >= ZoneSpecs[(int)zone].DamageThreshold;

        internal static bool IsZone(BoatZone zone) => (uint)zone < ZoneSpecs.Length;

        internal int Damage(int amount, BoatZone? zone)
        {
            if (zone.HasValue) return DamageZone(amount, (int)zone.Value);

            // General hull damage keeps its full strength and starts at the mast.
            int applied = DamageZone(amount, (int)BoatZone.Mast);
            for (int i = 0; i < (int)BoatZone.Mast && applied < amount && Hp > 0; i++)
                applied += DamageZone(amount - applied, i);
            return applied;
        }

        private int DamageZone(int amount, int zoneIndex)
        {
            int applied = Math.Min(amount, Math.Min(Hp, _zoneHp[zoneIndex]));
            _zoneHp[zoneIndex] -= applied;
            Hp -= applied;
            return applied;
        }

        internal void Repair(BoatZone zone)
        {
            Hp += GetZoneDamage(zone);
            _zoneHp[(int)zone] = GetZoneMaxHp(zone);
        }

        internal void Respawn()
        {
            Hp = (int)Math.Ceiling(MaxHp * 0.3);
            int missing = MaxHp - Hp;
            int count = _zoneHp.Length;
            for (int i = 0; i < count; i++)
                _zoneHp[i] = ZoneSpecs[i].MaxHp - missing / count - (i < missing % count ? 1 : 0);
        }
    }
}
