using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.Application
{
    /// <summary>Входные сигналы морской сцены; переход при гибели повторяется при ошибке загрузки.</summary>
    public sealed class HomeShallowsController : MonoBehaviour
    {
        private bool Ready => GameApplication.Instance != null && !GameApplication.Instance.IsLoading && GameApplication.Instance.Session != null;
        public bool CollectFish() => Ready && GameApplication.Instance.Session.CatchFish(GameApplication.Instance.Session.Cargo.FishId);
        public void Damage(int amount)
        {
            if (Ready && GameApplication.Instance.Session.Damage(amount)) ReturnToPort();
        }
        public void ReturnToPort() { GameApplication.Instance?.ReturnToPort(); }
        private void Update()
        {
            if (Ready && !GameApplication.Instance.Session.AtSea) ReturnToPort();
        }
    }
}
