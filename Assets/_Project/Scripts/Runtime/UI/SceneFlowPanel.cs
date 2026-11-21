using SecondCatch.Application;
using UnityEngine;
namespace SecondCatch.UI
{
    /// <summary>Главное меню, запуск Boot и подсказка возврата из рейса.</summary>
    public sealed class SceneFlowPanel : MonoBehaviour
    {
        private void OnGUI()
        {
            var app = GameApplication.Instance;
            var sea = GetComponent<HomeShallowsController>();
            if (app != null && GetComponent<HomePortController>() != null) return;
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
            if (menu != null)
            {
                GUILayout.Label("Second Catch");
                if (GUILayout.Button("New game")) menu.NewGame();
                if (app.Session != null && GUILayout.Button("Continue (memory only)")) menu.ContinueGame();
            }
            else if (sea != null && app.Session != null)
            {
                GUILayout.Label("Для возврата выйдите за створ причала и войдите обратно между тумбами.");
                if (GUILayout.Button("Главное меню", GUILayout.Width(150))) app.OpenMenu();
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }
}
