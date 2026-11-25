using System;
using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Player;
using SecondCatch.Solo;
using SecondCatch.Stations;
using SecondCatch.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
                    var roots = scene.GetRootGameObjects();
                    foreach (var root in roots)
                        foreach (var component in root.GetComponentsInChildren<Transform>(true))
                            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject));
                    if (expected[i] == GameScenes.MENU || expected[i] == GameScenes.PORT)
                        Assert.IsTrue(Array.Exists(roots, root => root.TryGetComponent<UnityEngine.Camera>(out var camera)
                            && camera.isActiveAndEnabled), $"Для отрисовки IMGUI в Player нужна камера в {scene.name}");
                    if (expected[i] == GameScenes.SEA)
                    {
                        var ocean = FindInActiveScene<OceanSystem>();
                        using var serializedOcean = new SerializedObject(ocean);
                        var shader = serializedOcean.FindProperty("surfaceShader").objectReferenceValue;
                        Assert.NotNull(shader, "Прямая ссылка на шейдер воды нужна для включения в Player");
                        Assert.AreEqual("OceanPro/Depth Foam Water", shader.name);
                        using var hud = new SerializedObject(FindInActiveScene<GameplayHud>());
                        Assert.AreSame(FindInActiveScene<UnityEngine.Camera>(),
                            hud.FindProperty("_repairCamera").objectReferenceValue);
                    }
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [UnityTest]
        public IEnumerator BootMenuPortSeaReturnAndRestart()
        {
            EditorSceneManager.OpenScene(GameScenes.Path(GameScenes.BOOT));
            yield return new EnterPlayMode();
            yield return ExerciseVoyage();
            yield return new ExitPlayMode();
        }

        private static IEnumerator ExerciseVoyage()
        {
            yield return WaitFor(GameScenes.MENU);
            var app = GameApplication.Instance;
            Assert.NotNull(app); Assert.IsNull(app.Session);
            FindInActiveScene<MainMenuController>().NewGame();
            Assert.IsTrue(app.IsLoading);
            Assert.IsFalse(app.NewGame(), "Повторная команда отклоняется во время загрузки");
            yield return WaitFor(GameScenes.PORT);
            var session = app.Session;
            Assert.NotNull(session);
            FindInActiveScene<HomePortController>().Depart();
            yield return WaitFor(GameScenes.SEA);
            Assert.IsTrue(session.AtSea);
            var boat = FindInActiveScene<BoatController>();
            var zone = FindInActiveScene<PortZone>();
            var player = FindInActiveScene<PlayerController>();
            Assert.AreSame(boat, player.Boat);
            Assert.IsInstanceOf<HelmStation>(player.GetAvailableStation());
            Assert.AreEqual(60f, boat.HitPoints);
            Assert.AreEqual(60f, boat.MaxHitPoints);
            Assert.IsNull(boat.GetComponent<BoatRespawnController>());
            Assert.NotNull(FindInActiveScene<WaveController>().Ocean);
            Assert.NotNull(boat.GetComponent<BoatBuoyancy>());

            Vector3 departure = boat.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(zone.ReadyToDock, "Зона причала не возвращает лодку при спавне");
            Assert.AreEqual(GameScenes.SEA, SceneManager.GetActiveScene().name);

            boat.SetSailRaised(true);
            yield return WaitUntil(() => boat.SailDeployment > 0.99f && boat.LongitudinalSpeed > 0.5f);
            float initialThrust = boat.SailForwardThrust / boat.SailDeployment;
            Assert.Greater(initialThrust, 0f);
            yield return WaitUntil(() => zone.ReadyToDock);
            Assert.Greater(boat.transform.position.z, departure.z + 2f);
            Assert.Greater(boat.WaveDrift.sqrMagnitude, 0f);

            boat.SetSailRaised(false);
            yield return HitObstacle(boat, "Reef", new Vector3(-0.6f, 0f, 0.8f), BoatZone.FrontLeft, 45);
            yield return HitObstacle(boat, "WaterDebris", new Vector3(0.6f, 0f, 0.8f), BoatZone.FrontRight, 40);
            yield return HitObstacle(boat, "Rock", new Vector3(-0.6f, 0f, -0.8f), BoatZone.RearLeft, 15);
            for (int i = 0; i < 10; i++)
                Assert.IsTrue(FindInActiveScene<HomeShallowsController>().CollectFish());

            Assert.IsTrue(app.OpenMenu());
            yield return WaitFor(GameScenes.MENU);
            FindInActiveScene<MainMenuController>().ContinueGame();
            yield return WaitFor(GameScenes.SEA);
            boat = FindInActiveScene<BoatController>();
            zone = FindInActiveScene<PortZone>();
            Assert.AreEqual(15f, boat.HitPoints, "HP переживают выгрузку и продолжение рейса");
            PlaceBoat(boat, new Vector3(departure.x, departure.y, departure.z + 6f));
            yield return WaitUntil(() => zone.ReadyToDock);
            PlaceBoat(boat, departure);
            yield return WaitFor(GameScenes.PORT);
            Assert.AreSame(session, app.Session);
            Assert.IsFalse(session.AtSea); Assert.AreEqual(10, session.Cargo.Count);
            Assert.AreEqual(15, session.Boat.Hp);
            var port = FindInActiveScene<HomePortController>();
            Assert.IsTrue(port.Sell());
            Assert.AreEqual(100, session.Coins);
            Assert.IsTrue(port.BuyUpgrade());
            Assert.IsFalse(port.BuyUpgrade());
            Assert.AreEqual(0, session.Coins);
            Assert.IsTrue(app.Depart()); yield return WaitFor(GameScenes.SEA);
            boat = FindInActiveScene<BoatController>();
            PlaceBoat(boat, new Vector3(-14f, departure.y, departure.z));
            boat.SetSailRaised(true);
            yield return WaitUntil(() => boat.SailDeployment > 0.99f && boat.LongitudinalSpeed > 3f);
            Assert.That(boat.SailForwardThrust / boat.SailDeployment, Is.EqualTo(initialThrust * 1.4f).Within(0.05f));
            var voyage = FindInActiveScene<HomeShallowsController>();
            for (int i = 0; i < session.Cargo.First.Capacity; i++) Assert.IsTrue(voyage.CollectFish());
            Assert.IsFalse(voyage.CollectFish());

            int portLoads = 0;
            void CountPortLoads(Scene scene, LoadSceneMode mode) { if (scene.name == GameScenes.PORT) portLoads++; }
            SceneManager.sceneLoaded += CountPortLoads;
            long beforeDamage = session.Revision;
            try
            {
                Collider rock = GameObject.Find("Rock").GetComponent<Collider>();
                PlaceBoat(boat, rock.bounds.center);
                yield return WaitFor(GameScenes.PORT);
                yield return null;
            }
            finally { SceneManager.sceneLoaded -= CountPortLoads; }
            Assert.AreEqual(1, portLoads);
            Assert.AreEqual(beforeDamage + 1, session.Revision);
            Assert.AreEqual(18, session.Boat.Hp);
            Assert.AreEqual(50, session.Cargo.Count);
            Assert.IsFalse(app.ReturnToPort());

            yield return SceneManager.LoadSceneAsync(GameScenes.Path(GameScenes.BOOT), LoadSceneMode.Additive);
            yield return null;
            Assert.AreSame(app, GameApplication.Instance, "Повторная загрузка Boot сохраняет первый корень");
            Assert.AreEqual(1, Object.FindObjectsByType<GameApplication>().Length);
            Assert.IsTrue(app.OpenMenu()); yield return WaitFor(GameScenes.MENU);
            Assert.IsTrue(app.NewGame()); yield return WaitFor(GameScenes.PORT);
            Assert.AreNotSame(session, app.Session); Assert.AreEqual(0, app.Session.Coins);
            Assert.AreEqual(1, Object.FindObjectsByType<GameApplication>().Length);
        }

        private static T FindInActiveScene<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>();
                if (component != null) return component;
            }
            Assert.Fail($"{typeof(T).Name} missing from {scene.name}");
            return null;
        }

        private static IEnumerator HitObstacle(BoatController boat, string obstacleName, Vector3 impactDirection,
            BoatZone zone, int expectedHp)
        {
            var obstacle = GameObject.Find(obstacleName).GetComponent<Collider>();
            Assert.IsTrue(obstacle.isTrigger);
            var bounds = obstacle.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            int hpBefore = GameApplication.Instance.Session.Boat.Hp;
            boat.transform.rotation = Quaternion.identity;
            PlaceBoat(boat, bounds.center - impactDirection * (radius + 1.15f));
            yield return WaitUntil(() => boat.HitPoints <= expectedHp);
            Assert.AreEqual(expectedHp, boat.HitPoints);
            var state = GameApplication.Instance.Session.Boat;
            Assert.AreEqual(expectedHp, state.Hp);
            Assert.AreEqual(hpBefore - expectedHp, state.GetZoneDamage(zone));
        }

        private static void PlaceBoat(BoatController boat, Vector3 position)
        {
            boat.ResetMotion();
            boat.transform.position = position;
            Physics.SyncTransforms();
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline);
                yield return null;
            }
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
