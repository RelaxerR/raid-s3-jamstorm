using SecondCatch.Application;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    public sealed class PortContractPanel : MonoBehaviour
    {
        [SerializeField] private Text _terms;
        [SerializeField] private Button _action;
        [SerializeField] private Text _actionLabel;
        private HomePortController _port;

        private void Awake() => _action.onClick.AddListener(UseContract);

        private void UseContract()
        {
            var session = _port != null ? _port.Session : null;
            if (session == null) return;
            if (session.Contract.State == FishContractState.Available)
                _port.AcceptContract();
            else
                _port.DeliverContract();
            Refresh(_port);
        }

        public void Refresh(HomePortController port)
        {
            _port = port;
            var session = port != null ? port.Session : null;
            _action.interactable = session != null && (session.CanAcceptContract || session.CanDeliverContract);
            _action.image.color = _action.interactable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.7f);
            if (session == null) return;

            string species = FishCargo.SpeciesName(FishContract.FishId);
            _terms.text = $"{species} рыба · {FishContract.Target} шт. · награда {FishContract.Reward} монет";
            switch (session.Contract.State)
            {
                case FishContractState.Available:
                    _actionLabel.text = "Принять контракт";
                    break;
                case FishContractState.Active:
                    _terms.text += $"\nДоставлено в бочках: {session.ContractProgress}/{FishContract.Target}";
                    _actionLabel.text = "Сдать улов";
                    break;
                case FishContractState.Closed:
                    _actionLabel.text = "Контракт закрыт";
                    break;
            }
        }
    }
}
