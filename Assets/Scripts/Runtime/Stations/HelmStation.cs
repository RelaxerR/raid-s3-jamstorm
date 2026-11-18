using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Управляет греблей и плавной коррекцией курса только во время занятия руля.</summary>
    public sealed class HelmStation : StationInteraction
    {
        public override PlayerControlMode ControlMode => PlayerControlMode.Helm;

        protected override void OnEntered() => Boat.SetHelmActive(true);

        public override void HandleInput(Vector2 input)
        {
            Boat.SetSteering(input.x);
            if (input.y > 0f) Boat.SetRowing(true);
            else if (input.y < 0f) Boat.SetRowing(false);
        }

        protected override void OnExited()
        {
            if (Boat != null) Boat.SetHelmActive(false);
        }
    }
}
