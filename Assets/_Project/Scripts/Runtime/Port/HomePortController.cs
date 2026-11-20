using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.Application
{
    /// <summary>Портовые команды, доступные из кликабельных объектов и UI.</summary>
    public sealed class HomePortController : MonoBehaviour
    {
        public void Depart() { GameApplication.Instance?.Depart(); }
        public bool Sell() => Ready && GameApplication.Instance.Session.Sell();
        public bool BuyUpgrade() => Ready && GameApplication.Instance.Session.BuySpeedUpgrade();
        private bool Ready => GameApplication.Instance != null && !GameApplication.Instance.IsLoading && GameApplication.Instance.Session != null;
    }
}
