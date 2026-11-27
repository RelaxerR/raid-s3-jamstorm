using SecondCatch.Application;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    [DisallowMultipleComponent]
    public sealed class PortTradePanel : MonoBehaviour
    {
        [SerializeField] private HomePortController _port;
        [SerializeField] private GameObject _tradeBody;
        [SerializeField] private PortContractPanel _contract;
        [SerializeField] private Text _cargo;
        [SerializeField] private Text _coins;
        [SerializeField] private Text _wood;
        [SerializeField] private Text _buyWoodLabel;
        [SerializeField] private Text _buyUpgradeLabel;
        [SerializeField] private Text _speedUpgrade;
        [SerializeField] private Button _open;
        [SerializeField] private Button _close;
        [SerializeField] private Button _sellFish;
        [SerializeField] private Button _sellAmber;
        [SerializeField] private Button _sellUnique;
        [SerializeField] private Button _sellAll;
        [SerializeField] private Button _buyUpgrade;
        [SerializeField] private Button _buyWood;
        [SerializeField] private Button _depart;
        [SerializeField] private Button _menu;

        private SoloGameSession _renderedSession;
        private long _renderedRevision = long.MinValue;

        private void Awake()
        {
            _open.onClick.AddListener(Open);
            _close.onClick.AddListener(Close);
            _sellFish.onClick.AddListener(() => SellFish(FishCargo.AzureFish));
            _sellAmber.onClick.AddListener(() => SellFish(FishCargo.AmberFish));
            _sellUnique.onClick.AddListener(() => SellFish(SoloGameSession.UNIQUE_FISH_ID));
            _sellAll.onClick.AddListener(SellAll);
            _buyUpgrade.onClick.AddListener(BuyUpgrade);
            _buyWood.onClick.AddListener(BuyWood);
            _depart.onClick.AddListener(Depart);
            _menu.onClick.AddListener(OpenMenu);
            Close();
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Open()
        {
            if (_port == null || _port.Session == null) return;
            _contract.gameObject.SetActive(false);
            _tradeBody.SetActive(true);
            _open.gameObject.SetActive(false);
            Refresh();
        }

        public void Close()
        {
            _tradeBody.SetActive(false);
            _contract.gameObject.SetActive(true);
            _open.gameObject.SetActive(true);
        }

        private void SellFish(string fishId)
        {
            _port?.SellFish(fishId);
            Refresh();
        }

        private void SellAll()
        {
            _port?.Sell();
            Refresh();
        }

        private void BuyUpgrade()
        {
            _port?.BuyUpgrade();
            Refresh();
        }

        private void BuyWood()
        {
            _port?.BuyWood();
            Refresh();
        }

        private void Depart()
        {
            _port?.Depart();
            Refresh();
        }

        private void OpenMenu()
        {
            _port?.OpenMenu();
            Refresh();
        }

        private static void SetAvailable(Button button, bool available)
        {
            button.interactable = available;
            button.image.color = available ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.7f);
        }

        private void Refresh()
        {
            var session = _port != null ? _port.Session : null;
            long revision = session?.Revision ?? -1;
            if (ReferenceEquals(session, _renderedSession) && revision == _renderedRevision) return;
            _renderedSession = session;
            _renderedRevision = revision;

            _contract.Refresh(_port);
            bool ready = session != null && !session.AtSea;
            SetAvailable(_open, ready);
            SetAvailable(_depart, ready);
            SetAvailable(_menu, ready);
            SetAvailable(_sellAll, session?.CanSell == true);
            SetAvailable(_sellFish, session?.CanSellFish(FishCargo.AzureFish) == true);
            SetAvailable(_sellAmber, session?.CanSellFish(FishCargo.AmberFish) == true);
            SetAvailable(_sellUnique, session?.CanSellFish(SoloGameSession.UNIQUE_FISH_ID) == true);
            SetAvailable(_buyUpgrade, session?.CanBuySpeedUpgrade == true);
            SetAvailable(_buyWood, session?.CanBuyWood == true);
            if (!ready) return;

            _cargo.text = $"Улов {session.Cargo.Count}/{session.Cargo.Capacity} · {SoloGameSession.FISH_PRICE} монет за рыбу\n"
                + $"1: {FutureGameplayIndicators.DescribeBarrel(session.Cargo.First)}     2: {FutureGameplayIndicators.DescribeBarrel(session.Cargo.Second)}\n"
                + $"Багряная рыба {session.UniqueFishCount}/{session.UniqueFishCapacity} · {SoloGameSession.UNIQUE_FISH_PRICE} монет";
            _coins.text = $"Монеты: {session.Coins}";
            _wood.text = $"Древесина: {session.Wood}";
            _buyWoodLabel.text = $"Купить древесину · {session.WoodPrice} монет";
            _speedUpgrade.text = $"Улучшение скорости · тяга ×{BoatState.UPGRADED_SPEED_MULTIPLIER:0.0}";
            _buyUpgradeLabel.text = session.Boat.SpeedUpgrade
                ? "Куплено"
                : $"Купить · {SoloGameSession.UPGRADE_PRICE} монет";
        }
    }
}
