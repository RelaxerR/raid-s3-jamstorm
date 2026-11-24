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
        [SerializeField] private Text _cargo;
        [SerializeField] private Text _coins;
        [SerializeField] private Text _wood;
        [SerializeField] private Text _buyWoodLabel;
        [SerializeField] private Text _speedUpgrade;
        [SerializeField] private Button _open;
        [SerializeField] private Button _close;
        [SerializeField] private Button _sellFish;
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
            _sellFish.onClick.AddListener(SellFish);
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
            _tradeBody.SetActive(true);
            _open.gameObject.SetActive(false);
            Refresh();
        }

        public void Close()
        {
            _tradeBody.SetActive(false);
            _open.gameObject.SetActive(true);
        }

        private void SellFish()
        {
            var session = _port != null ? _port.Session : null;
            if (session != null) _port.SellFish(session.Cargo.FishId);
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

        private void Refresh()
        {
            var session = _port != null ? _port.Session : null;
            long revision = session?.Revision ?? -1;
            if (ReferenceEquals(session, _renderedSession) && revision == _renderedRevision) return;
            _renderedSession = session;
            _renderedRevision = revision;

            bool ready = session != null && !session.AtSea;
            _open.interactable = ready;
            _depart.interactable = ready;
            _menu.interactable = ready;
            _sellAll.interactable = _sellFish.interactable = session?.CanSell == true;
            _buyUpgrade.interactable = session?.CanBuySpeedUpgrade == true;
            _buyWood.interactable = session?.CanBuyWood == true;
            if (!ready) return;

            _cargo.text = $"Улов {session.Cargo.Count}/{session.Cargo.Capacity} · {SoloGameSession.FISH_PRICE} монет за рыбу";
            _coins.text = $"Монеты: {session.Coins}";
            _wood.text = $"Древесина: {session.Wood}";
            _buyWoodLabel.text = $"Купить древесину · {session.WoodPrice} монет";
            _speedUpgrade.text = session.Boat.SpeedUpgrade
                ? $"Куплено · скорость ×{session.Boat.SpeedMultiplier:0.0}"
                : $"Улучшение скорости · {SoloGameSession.UPGRADE_PRICE} монет";
        }
    }
}
