using SecondCatch.Application;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.UI;

namespace SecondCatch.UI
{
    /// <summary>Портовая карточка первого матроса, доступная также для хаба.</summary>
    public sealed class PortCrewPanel : MonoBehaviour
    {
        [SerializeField] private Button _action;
        [SerializeField] private Text _label;
        private HomePortController _port;
        private SoloGameSession _renderedSession;
        private long _renderedRevision = long.MinValue;

        private void Awake()
        {
            _port = GetComponentInParent<HomePortController>();
            _action.onClick.AddListener(Select);
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        private void Select()
        {
            if (_port?.Session == null) return;
            if (_port.Session.HasHelmsman) _port.AssignHelmsman();
            else _port.HireHelmsman();
            Refresh();
        }

        private void Refresh()
        {
            var session = _port != null ? _port.Session : null;
            long revision = session?.Revision ?? -1;
            if (_renderedSession == session && revision == _renderedRevision) return;
            _renderedSession = session;
            _renderedRevision = revision;
            _action.interactable = session != null && (session.CanHireHelmsman || session.CanAssignHelmsman);
            _action.image.color = _action.interactable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.7f);
            _label.text = session?.HasHelmsman != true ? $"Нанять · {SoloGameSession.HELMSMAN_PRICE} монет"
                : session.HelmsmanAssigned ? "Иван на руле" : "Назначить на руль";
        }
    }
}
