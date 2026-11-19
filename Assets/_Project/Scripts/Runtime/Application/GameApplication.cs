using System;
using System.Collections;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace SecondCatch.Application
{
    /// <summary>Корень приложения из Boot; владеет сессией и последовательной загрузкой сцен.</summary>
    public sealed class GameApplication : MonoBehaviour
    {
        public static GameApplication Instance { get; private set; }
        public SoloGameSession Session { get; private set; }
        public bool IsLoading { get; private set; }
        public string Error { get; private set; }
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start() { if (Instance == this) OpenMenu(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public bool OpenMenu() => Navigate(GameScenes.MENU, null);
        public bool NewGame() => Navigate(GameScenes.PORT, () => Session = new SoloGameSession());
        public bool ContinueGame() => Session != null && Navigate(Session.AtSea ? GameScenes.SEA : GameScenes.PORT, null);
        public bool Depart() => Session != null && !Session.AtSea && Navigate(GameScenes.SEA, () => Session.Depart());
        public bool ReturnToPort() => Session != null && SceneManager.GetActiveScene().path == GameScenes.Path(GameScenes.SEA)
            && Navigate(GameScenes.PORT, () => Session.Dock());
        private bool Navigate(string scene, Action commit)
        {
            if (IsLoading) return false;
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(GameScenes.Path(scene)))
            {
                Error = "Scene unavailable: " + scene;
                Debug.LogError(Error); return false;
            }
            IsLoading = true; Error = null;
            StartCoroutine(Load(scene, commit)); return true;
        }
        private IEnumerator Load(string scene, Action commit)
        {
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(GameScenes.Path(scene), LoadSceneMode.Single); }
            catch (Exception exception) { Error = exception.Message; Debug.LogException(exception); }
            if (operation == null) { IsLoading = false; yield break; }
            yield return operation;
            // Модель меняется только после успешной загрузки; сцены читают её после IsLoading=false.
            commit?.Invoke();
            IsLoading = false;
        }
        private void OnGUI()
        {
            if (IsLoading) GUI.Label(new Rect(20, 10, 600, 30), "Loading...");
            if (Error == null) return;
            GUI.Label(new Rect(20, 40, 900, 30), Error);
            if (GUI.Button(new Rect(20, 80, 200, 35), "Retry menu")) OpenMenu();
        }
    }
}
