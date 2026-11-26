using UnityEngine;

namespace SecondCatch.Fishing
{
    internal enum HarpoonOutcome { Fighting, Secured, Broken, Escaped }

    internal sealed class HarpoonLine
    {
        public const float MaximumLength = 20f;
        public const float WarningTension = 0.8f;
        private const float AlongsideDistance = 2.3f;
        private const float RunSeconds = 2.1f;
        private const float CycleSeconds = 5f;
        private const float ReelSpeed = 2.05f;
        private const float InitialSlack = 2f;
        internal const float EscapeSpeed = 4.5f;
        // Оставляем работу на второй отдых даже при попадании прямо у борта.
        internal const float MinimumFightDistance = AlongsideDistance + ReelSpeed * (CycleSeconds - RunSeconds) + InitialSlack;
        private readonly float _startDistance;
        private float _elapsed;

        public HarpoonLine(float horizontal, float height)
        {
            _startDistance = horizontal;
            Length = Mathf.Min(MaximumLength, ActualDistance(horizontal, height) + InitialSlack);
        }

        public float Tension { get; private set; } = 0.5f;
        public float Length { get; private set; }
        public bool Running => _elapsed % CycleSeconds < RunSeconds;
        public int RunDirection => (int)(_elapsed / CycleSeconds) % 2 == 0 ? 1 : -1;

        public HarpoonOutcome Advance(float seconds, float mouseAgainstRun, float reelInput, float height, ref Vector3 offset)
        {
            float distance = offset.magnitude;
            Vector3 outward = distance > 0.001f ? offset / distance : Vector3.forward;
            float range = distance;
            float sideSpeed = 0f;
            float tensionRate = -0.09f;
            if (reelInput < 0f)
                Length = Mathf.Min(MaximumLength, Length + 4.5f * seconds);

            if (Running)
            {
                bool resisting = mouseAgainstRun > 0f;
                sideSpeed = resisting ? 0.35f : 2.2f;
                float escapeSpeed = resisting ? 0f : mouseAgainstRun < 0f ? EscapeSpeed : 1.25f;
                range += Mathf.Min(escapeSpeed * seconds, Mathf.Max(0f, Length - ActualDistance(distance, height)));
                tensionRate = resisting ? 0.34f : mouseAgainstRun < 0f ? 0.04f : 0.19f;
            }
            else if (reelInput > 0f)
            {
                range = Mathf.Min(distance, Mathf.Max(AlongsideDistance, distance - ReelSpeed * seconds));
                Length = Mathf.Min(Length, ActualDistance(range, height) + 0.35f);
                tensionRate = 0.04f;
            }

            if (reelInput < 0f) tensionRate = -0.48f;
            Tension = Mathf.Clamp01(Tension + seconds * tensionRate);

            bool secured = !Running && reelInput > 0f && range <= AlongsideDistance;
            Vector3 sideways = Vector3.Cross(Vector3.up, outward);
            offset = (outward * Mathf.Max(distance, 0.001f) + sideways * (RunDirection * sideSpeed * seconds)).normalized * range;
            _elapsed += seconds;
            if (ActualDistance(range, height) >= MaximumLength - 0.01f) return HarpoonOutcome.Escaped;
            if (Tension >= 1f) return HarpoonOutcome.Broken;
            return secured ? HarpoonOutcome.Secured : HarpoonOutcome.Fighting;
        }

        public float Progress(float distance) =>
            Mathf.Clamp01((_startDistance - distance) / Mathf.Max(0.01f, _startDistance - AlongsideDistance));

        private static float ActualDistance(float horizontal, float height) =>
            Mathf.Sqrt(horizontal * horizontal + height * height);
    }
}
