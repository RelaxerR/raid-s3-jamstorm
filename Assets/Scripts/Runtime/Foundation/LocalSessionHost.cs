using UnityEngine;

namespace SecondCatch.Foundation
{
    /// <summary>
    /// Explicit local integration entry point. Add to an isolated test scene.
    /// Replace this driver with a Fusion StateAuthority adapter for network sessions.
    /// </summary>
    public sealed class LocalSessionHost : MonoBehaviour
    {
        #region Configuration
        [SerializeField, Min(1)] private int _capacity = 20;
        [SerializeField, Min(1)] private int _fishPrice = 10;
        [SerializeField, Min(1)] private int _upgradePrice = 100;
        public GameSession Session { get; private set; }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            Session = new GameSession(new FishDefinition("fish.demo", Mathf.Max(1, _fishPrice), 5),
                Mathf.Max(1, _capacity), Mathf.Max(1, _upgradePrice));
        }

        /// <summary>Called by the local station adapter with validated operator input.</summary>
        public void Simulate(float deltaTime, bool winchHeld)
        {
            Session?.Tick(deltaTime, winchHeld);
        }
        #endregion
    }
}
