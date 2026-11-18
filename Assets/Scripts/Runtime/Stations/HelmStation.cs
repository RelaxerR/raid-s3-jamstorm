using SecondCatch.Player;
using UnityEngine;

namespace SecondCatch.Stations
{
    /// <summary>Передаёт рулю ввод A/D; W начинает движение, S останавливает лодку.</summary>
    public sealed class HelmStation : StationInteraction
    {
        public override PlayerControlMode ControlMode => PlayerControlMode.Helm;

        protected override void OnEntered() => Boat.SetSailing(true);

        public override void HandleInput(Vector2 input)
        {
            Boat.SetSteering(input.x);
            if (input.y > 0f) Boat.SetSailing(true);
            else if (input.y < 0f) Boat.SetSailing(false);
        }

        protected override void OnExited()
        {
            if (Boat != null) Boat.SetSteering(0f);
        }
    }
}
