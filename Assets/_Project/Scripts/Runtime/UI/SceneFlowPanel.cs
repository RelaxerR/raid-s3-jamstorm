using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.UI
{
    /// <summary>Меню, портовые действия и подсказка возврата из рейса.</summary>
    public sealed class SceneFlowPanel : MonoBehaviour
    {
        private void OnGUI()
        {
            var app = GameApplication.Instance;
            var sea = GetComponent<HomeShallowsController>();
            GUILayout.BeginArea(sea != null && app != null
                ? new Rect(20, Screen.height - 76, Screen.width - 40, 76)
                : new Rect(30, 120, 600, 600));
            if (app == null)
            {
                GUILayout.Label("Start from Boot to initialize the application.");
                if (GUILayout.Button("Open Boot")) UnityEngine.SceneManagement.SceneManager.LoadScene(GameScenes.Path(GameScenes.BOOT));
                GUILayout.EndArea(); return;
            }
            GUI.enabled = !app.IsLoading;
            var menu = GetComponent<MainMenuController>();
            var port = GetComponent<HomePortController>();
            if (menu != null)
            {
                GUILayout.Label("Second Catch");
                if (GUILayout.Button("New game")) menu.NewGame();
                if (app.Session != null && GUILayout.Button("Continue (memory only)")) menu.ContinueGame();
            }
            else if (sea != null && app.Session != null)
            {
                GUILayout.Label($"Улов {app.Session.Cargo.Count}/{app.Session.Cargo.Capacity} · Для возврата выйдите за створ причала и войдите обратно между тумбами.");
                if (GUILayout.Button("Главное меню", GUILayout.Width(150))) app.OpenMenu();
            }
            else if (app.Session != null)
            {
                var session = app.Session;
                GUILayout.Label($"HP {session.Boat.Hp}/{session.Boat.MaxHp} | Fish {session.Cargo.Count}/{session.Cargo.Capacity} | Coins {session.Coins} | Speed x{session.Boat.SpeedMultiplier}");
                if (port != null)
                {
                    if (GUILayout.Button("Depart")) port.Depart();
                    if (GUILayout.Button("Sell catch")) port.Sell();
                    if (GUILayout.Button("Buy speed upgrade (100)")) port.BuyUpgrade();
                }
                if (GUILayout.Button("Main menu")) app.OpenMenu();
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
