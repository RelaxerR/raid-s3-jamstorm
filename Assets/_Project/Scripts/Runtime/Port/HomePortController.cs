using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Application
{
    /// <summary>Port commands used by the scene UI and interactable objects.</summary>
    public sealed class HomePortController : MonoBehaviour
    {
        public SoloGameSession Session
        {
            get
            {
                var app = GameApplication.Instance;
                return app != null && !app.IsLoading ? app.Session : null;
            }
        }

        public void Depart() => GameApplication.Instance?.Depart();
        public void OpenMenu() => GameApplication.Instance?.OpenMenu();
        public bool Sell() => Session?.Sell() == true;
        public bool SellFish(string fishId) => Session?.SellFish(fishId) == true;
        public bool BuyUpgrade() => Session?.BuySpeedUpgrade() == true;
    }
}
