using UnityEngine;
namespace SecondCatch.Application
{
    /// <summary>Команды меню; продолжение доступно только для сессии в памяти.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public void NewGame() { GameApplication.Instance?.NewGame(); }
        public void ContinueGame() { GameApplication.Instance?.ContinueGame(); }
    }
}
