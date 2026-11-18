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
        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (UnityEngine.Application.isPlaying) yield return new ExitPlayMode();
        }

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
            Object.FindFirstObjectByType<MainMenuController>().NewGame();
            Assert.IsTrue(app.IsLoading);
            Assert.IsFalse(app.NewGame(), "Повторная команда отклоняется во время загрузки");
            yield return WaitFor(GameScenes.PORT);
            var session = app.Session;
            Assert.NotNull(session);
            Object.FindFirstObjectByType<HomePortController>().Depart();
            yield return WaitFor(GameScenes.SEA);
            Assert.IsTrue(session.AtSea);
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(Object.FindFirstObjectByType<HomeShallowsController>().CollectFish());
            Assert.IsTrue(app.OpenMenu());
            yield return WaitFor(GameScenes.MENU);
            Object.FindFirstObjectByType<MainMenuController>().ContinueGame();
            yield return WaitFor(GameScenes.SEA);
            Object.FindFirstObjectByType<HomeShallowsController>().ReturnToPort();
            yield return WaitFor(GameScenes.PORT);
            Assert.AreSame(session, app.Session);
            Assert.IsFalse(session.AtSea); Assert.AreEqual(10, session.Cargo.Count);
            var port = Object.FindFirstObjectByType<HomePortController>();
            Assert.IsTrue(port.Sell());
            Assert.AreEqual(100, session.Coins);
            Assert.IsTrue(port.BuyUpgrade());
            Assert.IsFalse(port.BuyUpgrade());
            Assert.AreEqual(0, session.Coins);
            Assert.IsTrue(app.Depart()); yield return WaitFor(GameScenes.SEA);
            Assert.AreEqual(1.4f, session.Boat.SpeedMultiplier);
            Object.FindFirstObjectByType<HomeShallowsController>().Damage(100);
            yield return WaitFor(GameScenes.PORT);
            Assert.AreEqual(18, session.Boat.Hp);
            Assert.IsTrue(app.OpenMenu()); yield return WaitFor(GameScenes.MENU);
            Assert.IsTrue(app.NewGame()); yield return WaitFor(GameScenes.PORT);
            Assert.AreNotSame(session, app.Session); Assert.AreEqual(0, app.Session.Coins);
            Assert.AreEqual(1, Object.FindObjectsByType<GameApplication>(FindObjectsSortMode.None).Length);
            yield return new ExitPlayMode();
        }
        [Test]
        public void SoloPipelineHasNoPhotonOrCooperativeDependency()
        {
            foreach (var assembly in new[] { typeof(GameApplication).Assembly, typeof(SecondCatch.Solo.SoloGameSession).Assembly })
                foreach (var reference in assembly.GetReferencedAssemblies())
                    StringAssert.DoesNotContain("Photon", reference.Name);
            foreach (string path in System.IO.Directory.GetFiles("Assets/_Project/Scripts/Runtime/Application", "*.cs"))
            {
                string source = System.IO.File.ReadAllText(path);
                StringAssert.DoesNotContain("Photon", source);
                StringAssert.DoesNotContain("SecondCatch.Foundation", source);
            }
        }

        private static IEnumerator WaitFor(string scene)
        {
            float deadline = Time.realtimeSinceStartup + 20;
            while (GameApplication.Instance == null || GameApplication.Instance.IsLoading || SceneManager.GetActiveScene().name != scene)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Истекло время загрузки " + scene);
                yield return null;
            }
        }
    }
}
