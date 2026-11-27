using SecondCatch.Solo;
using UnityEngine;

namespace SecondCatch.Application
{
    /// <summary>Port commands used by the scene UI and interactable objects.</summary>
    public sealed class HomePortController : MonoBehaviour
    {
        [SerializeField] private AudioSource _tradeAudio;
        [SerializeField] private AudioClip _sellSound;
        [SerializeField] private AudioClip _purchaseSound;

        public SoloGameSession Session
        {
            get
            {
                var app = GameApplication.Instance;
                return app != null && !app.IsLoading ? app.Session : null;
            }
        }

        public bool AcceptContract() => Session?.AcceptContract() == true;
        public bool DeliverContract() => ConfirmTrade(Session?.DeliverContract() == true, _sellSound);
        public void Depart() => GameApplication.Instance?.Depart();
        public void OpenMenu() => GameApplication.Instance?.OpenMenu();
        public bool Sell() => ConfirmTrade(Session?.Sell() == true, _sellSound);
        public bool SellFish(string fishId) => ConfirmTrade(Session?.SellFish(fishId) == true, _sellSound);
        public bool BuyUpgrade() => ConfirmTrade(Session?.BuySpeedUpgrade() == true, _purchaseSound);
        public bool BuyWood() => ConfirmTrade(Session?.BuyWood() == true, _purchaseSound);

        private bool ConfirmTrade(bool completed, AudioClip sound)
        {
            if (completed && _tradeAudio != null && sound != null) _tradeAudio.PlayOneShot(sound);
            return completed;
        }
    }
}
