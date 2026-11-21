using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondCatch.Tests
{
    public abstract class VoyageTestFixture
    {
        protected static GameApplication App => GameApplication.Instance;

        [UnityTearDown]
        public IEnumerator UnloadVoyage()
        {
            if (App != null) Object.Destroy(App.gameObject);
            Cursor.lockState = CursorLockMode.None;
            var scene = SceneManager.CreateScene("VoyageTestCleanup");
            Scene oldScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            yield return SceneManager.UnloadSceneAsync(oldScene);
        }

        protected static IEnumerator StartNewGame()
        {
            if (App != null)
            {
                Object.Destroy(App.gameObject);
                yield return null;
            }
            yield return SceneManager.LoadSceneAsync(GameScenes.Path(GameScenes.BOOT));
            yield return WaitForScene(GameScenes.MENU);
            SceneComponent<MainMenuController>().NewGame();
            yield return WaitForScene(GameScenes.PORT);
        }

        protected static IEnumerator WaitForScene(string scene)
        {
            yield return WaitFor(() => App != null && !App.IsLoading && SceneManager.GetActiveScene().name == scene,
                $"Timed out loading {scene}");
            yield return null;
        }

        protected static IEnumerator WaitFor(Func<bool> condition, string message)
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, message);
                yield return null;
            }
        }

        protected static T SceneComponent<T>() where T : Component
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>();
                if (component != null) return component;
            }
            Assert.Fail($"No {typeof(T).Name} in {SceneManager.GetActiveScene().name}");
            return null;
        }

        protected static FishSchool[] Schools() => Object.FindObjectsByType<FishSchool>(FindObjectsInactive.Include)
            .OrderBy(school => school.transform.position.z).ToArray();

        protected static Text CargoLabel() => SceneComponent<GameplayHud>()
            .GetComponentsInChildren<Text>(true).Single(text => text.text.StartsWith("Улов "));

        protected static void PlaceBoat(BoatController boat, Vector3 position)
        {
            boat.ResetMotion();
            boat.transform.position = position;
            Physics.SyncTransforms();
        }
    }
}
