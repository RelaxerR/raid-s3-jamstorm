using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class ScenePipelineTests
    {
        [Test]
        public void BuildOrderAndSceneReferencesAreValid()
        {
            var expected = new[] { GameScenes.BOOT, GameScenes.MENU, GameScenes.PORT, GameScenes.SEA };
            Assert.AreEqual(4, EditorBuildSettings.scenes.Length);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.IsTrue(EditorBuildSettings.scenes[i].enabled);
                    Assert.AreEqual(GameScenes.Path(expected[i]), EditorBuildSettings.scenes[i].path);
                    var scene = EditorSceneManager.OpenScene(GameScenes.Path(expected[i]));
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var component in root.GetComponentsInChildren<Transform>(true))
                            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject));
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [UnityTest]
        public IEnumerator BootMenuPortSeaReturnAndRestart()
        {
            EditorSceneManager.OpenScene(GameScenes.Path(GameScenes.BOOT));
            yield return new EnterPlayMode();
            yield return WaitFor(GameScenes.MENU);
            var app = GameApplication.Instance;
            Assert.NotNull(app); Assert.IsNull(app.Session);
            Assert.IsTrue(app.NewGame());
            Assert.IsFalse(app.NewGame(), "Repeated clicks are ignored during loading");
            yield return WaitFor(GameScenes.PORT);
            var session = app.Session;
            Assert.NotNull(session);
            Assert.IsTrue(app.Depart());
            yield return WaitFor(GameScenes.SEA);
            Assert.IsTrue(session.AtSea);
            Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
            Assert.IsTrue(app.OpenMenu());
            yield return WaitFor(GameScenes.MENU);
            Assert.IsTrue(app.ContinueGame());
            yield return WaitFor(GameScenes.SEA);
            Assert.IsTrue(app.ReturnToPort());
            yield return WaitFor(GameScenes.PORT);
            Assert.AreSame(session, app.Session);
            Assert.IsFalse(session.AtSea); Assert.AreEqual(1, session.Cargo.Count);
            Assert.IsTrue(session.Sell()); Assert.AreEqual(10, session.Coins);
            Assert.IsTrue(app.Depart()); yield return WaitFor(GameScenes.SEA);
            Object.FindFirstObjectByType<HomeShallowsController>().Damage(100);
            yield return WaitFor(GameScenes.PORT);
            Assert.AreEqual(18, session.Boat.Hp);
            Assert.IsTrue(app.OpenMenu()); yield return WaitFor(GameScenes.MENU);
            Assert.IsTrue(app.NewGame()); yield return WaitFor(GameScenes.PORT);
            Assert.AreNotSame(session, app.Session); Assert.AreEqual(0, app.Session.Coins);
            Assert.AreEqual(1, Object.FindObjectsByType<GameApplication>(FindObjectsSortMode.None).Length);
            yield return new ExitPlayMode();
        }
        private static IEnumerator WaitFor(string scene)
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (GameApplication.Instance == null || GameApplication.Instance.IsLoading || SceneManager.GetActiveScene().name != scene)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Timed out loading " + scene);
                yield return null;
            }
        }
    }
}
