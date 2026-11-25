using System;

namespace SecondCatch.Fishing
{
    internal enum HarpoonOutcome { Fighting, Landed, Broken }

    internal sealed class HarpoonLine
    {
        private const float BreakTension = 1f;
        private const float SurgeSeconds = 2.1f;
        private const float CycleSeconds = 5f;
        private float _elapsed;

        public float Tension { get; private set; } = 0.28f;
        public float Progress { get; private set; }
        public bool Surging => _elapsed % CycleSeconds < SurgeSeconds;

        public HarpoonOutcome Advance(float seconds, bool reel, bool slack)
        {
            float tensionRate;
            if (slack)
            {
                tensionRate = -0.55f;
                Progress = Math.Max(0f, Progress - seconds * 0.075f);
            }
            else if (reel)
            {
                tensionRate = Surging ? 0.42f : 0.12f;
                Progress = Math.Min(1f, Progress + seconds * 0.23f);
            }
            else
            {
                tensionRate = Surging ? 0.075f : -0.1f;
            }

            Tension = Math.Max(0f, Tension + seconds * tensionRate);
            _elapsed += seconds;
            if (Tension > BreakTension) return HarpoonOutcome.Broken;
            return Progress >= 1f ? HarpoonOutcome.Landed : HarpoonOutcome.Fighting;
        }
    }
}
