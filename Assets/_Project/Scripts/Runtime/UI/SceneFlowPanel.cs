using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.UI
{
    /// <summary>Временная панель проверки пайплайна; будущий UI Toolkit вызывает те же команды.</summary>
    public sealed class SceneFlowPanel : MonoBehaviour
    {
        private void OnGUI()
        {
            var app = GameApplication.Instance;
            GUILayout.BeginArea(new Rect(30, 120, 600, 600));
            GUILayout.Label("Second Catch / pipeline scaffold");
            if (app == null)
            {
                GUILayout.Label("Start from Boot to initialize the application.");
                if (GUILayout.Button("Open Boot")) UnityEngine.SceneManagement.SceneManager.LoadScene(GameScenes.Path(GameScenes.BOOT));
                GUILayout.EndArea(); return;
            }
            GUI.enabled = !app.IsLoading;
            var menu = GetComponent<MainMenuController>();
            var port = GetComponent<HomePortController>();
            var sea = GetComponent<HomeShallowsController>();
            if (menu != null)
            {
                if (GUILayout.Button("New game")) menu.NewGame();
                if (app.Session != null && GUILayout.Button("Continue (memory only)")) menu.ContinueGame();
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
                if (sea != null)
                {
                    GUILayout.Label("Technical controls below simulate signals, not finished gameplay.");
                    if (GUILayout.Button("Debug: collect one fish")) sea.CollectFish();
                    if (GUILayout.Button("Debug: reef damage (15)")) sea.Damage(15);
                    if (GUILayout.Button("Return to port")) sea.ReturnToPort();
                }
                if (GUILayout.Button("Main menu")) app.OpenMenu();
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
