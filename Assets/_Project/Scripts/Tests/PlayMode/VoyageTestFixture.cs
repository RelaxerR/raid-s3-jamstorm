using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.Player;
using SecondCatch.Solo;
using SecondCatch.Stations;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondCatch.Tests
{
    public abstract class VoyageTestFixture
    {
        protected static GameApplication App => GameApplication.Instance;

        protected sealed class GameViewInputScope : IDisposable
        {
            private readonly InputSettings _settings = InputSystem.settings;
            private readonly InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
            private readonly InputSettings.EditorInputBehaviorInPlayMode _editorInput;
#endif

            public GameViewInputScope()
            {
                // Batch-mode PlayMode has no focused Game view to receive synthetic keys.
                _background = _settings.backgroundBehavior;
#if UNITY_EDITOR
                _editorInput = _settings.editorInputBehaviorInPlayMode;
                _settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            }

            public void Dispose()
            {
                _settings.backgroundBehavior = _background;
#if UNITY_EDITOR
                _settings.editorInputBehaviorInPlayMode = _editorInput;
#endif
            }
        }

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

        protected static void PlacePlayer(PlayerController player, StationInteraction station)
        {
            Vector3 local = player.Boat.transform.InverseTransformPoint(station.transform.position);
            local.y = player.transform.localPosition.y;
            player.transform.localPosition = local;
        }

        protected static IEnumerator SetNetState(NetStation net, NetState expected)
        {
            Assert.IsTrue(net.TryInteract());
            yield return WaitFor(() => net.State == expected, $"Net did not reach {expected}");
        }

        protected static IEnumerator LandSchoolAndStoreFish(BoatController boat, FishSchool school, int count)
        {
            var player = SceneComponent<PlayerController>();
            var net = SceneComponent<NetStation>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);
            PlaceBoat(boat, school.transform.position + Vector3.up);
            yield return WaitFor(() => net.Count == count, "The net did not catch the school");
            yield return SetNetState(net, NetState.Stowed);
            player.ExitStation();
            var deck = SceneComponent<DeckCatch>();
            yield return WaitFor(() => deck.GetComponentsInChildren<FishOnDeck>().Length == count, "The net did not land the fish");
            foreach (FishOnDeck fish in deck.GetComponentsInChildren<FishOnDeck>())
                Assert.AreEqual(DeckFishStoreResult.Stored, fish.TryStore(App.Session));
        }

        protected static void PlaceBoat(BoatController boat, Vector3 position)
        {
            boat.ResetMotion();
            boat.transform.position = position;
            Physics.SyncTransforms();
        }

        protected static Button ButtonIn(PortTradePanel panel, string name) =>
            panel.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

        protected static bool HasText(PortTradePanel panel, string fragment) =>
            panel.GetComponentsInChildren<Text>().Any(text => text.text.Contains(fragment));

        protected static void Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, button.name);
            Assert.IsTrue(button.interactable, button.name);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, $"{button.name} at {pointer.position} on {Screen.width}×{Screen.height}");
            Assert.AreSame(button.gameObject, hits[0].gameObject, button.name);
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
