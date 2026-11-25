using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Player;
using SecondCatch.Solo;
using SecondCatch.Stations;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondCatch.Tests
{
    public sealed class HarpoonVoyageTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator HarpoonCanBreakThenLandOneFishThatSurvivesDockingAndSellsOnce()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            Vector3 departure = boat.transform.position;
            PlaceOutsidePort(boat, departure);

            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var harpoon = SceneComponent<HarpoonStation>();
                var player = SceneComponent<PlayerController>();
                PlacePlayer(player, harpoon);
                Assert.AreSame(harpoon, player.GetAvailableStation());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.AreEqual(PlayerControlMode.Harpoon, player.ControlMode);
                yield return new WaitForSeconds(0.6f);

                var camera = SceneComponent<UnityEngine.Camera>();
                var target = GameObject.Find("CrimsonFish");
                var carcass = boat.transform.Find("CrimsonFishOnDeck").gameObject;
                Assert.IsFalse(carcass.activeSelf);
                InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
                yield return WaitFor(() => harpoon.Fighting, "The aimed shot did not reach the fish");
                yield return WaitFor(() => harpoon.Tension > 0.8f, "Continuously reeling should strain the line");
                Assert.IsTrue(TensionIsVisible());
                yield return WaitFor(() => !harpoon.Fighting, "The strained line did not break");
                Assert.That(harpoon.Status, Does.Contain("оборвался"));
                Assert.AreEqual(0, App.Session.UniqueFishCount);
                Assert.IsFalse(carcass.activeSelf);
                InputSystem.QueueStateEvent(mouse, MouseAt(camera, target));
                yield return new WaitForSeconds(0.48f);

                InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
                yield return WaitFor(() => harpoon.Fighting, "The fish should still be available after the break");
                bool easing = false;
                float deadline = Time.realtimeSinceStartup + 12f;
                while (harpoon.Fighting)
                {
                    Assert.Less(Time.realtimeSinceStartup, deadline, "Reeling and paying out did not land the fish");
                    easing = easing ? harpoon.Tension > 0.20f : harpoon.Tension > 0.66f;
                    InputSystem.QueueStateEvent(mouse,
                        new MouseState().WithButton(easing ? MouseButton.Right : MouseButton.Left));
                    yield return null;
                }

                Assert.IsTrue(App.Session.AtSea, "The boat drifted back into port during the fight");
                Assert.AreEqual(1, App.Session.UniqueFishCount, harpoon.Status);
                Assert.AreEqual(0, App.Session.Cargo.Count);
                Assert.IsFalse(target.activeSelf);
                Assert.IsTrue(carcass.activeSelf);
                yield return WaitFor(() => !TensionIsVisible(), "Tension did not hide after landing");
                StringAssert.Contains("Туша 1/1", CargoLabel().text);
                Assert.IsFalse(App.Session.TryStowUniqueFish());

                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "Port zone was not armed");
                PlaceBoat(boat, departure);
                yield return WaitForScene(GameScenes.PORT);
                Assert.AreEqual(1, App.Session.UniqueFishCount);

                Assert.IsTrue(App.Depart());
                yield return WaitForScene(GameScenes.SEA);
                harpoon = SceneComponent<HarpoonStation>();
                player = SceneComponent<PlayerController>();
                boat = player.Boat;
                departure = boat.transform.position;
                PlaceOutsidePort(boat, departure);
                PlacePlayer(player, harpoon);
                Assert.IsTrue(harpoon.TryEnter(player));
                yield return new WaitForSeconds(0.6f);
                target = GameObject.Find("CrimsonFish");
                camera = SceneComponent<UnityEngine.Camera>();
                InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
                yield return null;
                Assert.That(harpoon.Status, Does.Contain("занято"));
                Assert.IsFalse(harpoon.Fighting);
                Assert.AreEqual(1, App.Session.UniqueFishCount);

                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "Port zone was not armed again");
                PlaceBoat(boat, departure);
                yield return WaitForScene(GameScenes.PORT);
                var panel = SceneComponent<PortTradePanel>();
                Click(ButtonIn(panel, "OpenTrade"));
                yield return null;
                var sellUnique = ButtonIn(panel, "SellUnique");
                Assert.IsTrue(sellUnique.interactable);
                Assert.IsFalse(ButtonIn(panel, "SellFish").interactable);
                Click(sellUnique);
                Assert.AreEqual(SoloGameSession.UNIQUE_FISH_PRICE, App.Session.Coins);
                Assert.AreEqual(0, App.Session.UniqueFishCount);
                Assert.IsFalse(sellUnique.interactable);
                Assert.IsFalse(SceneComponent<HomePortController>().SellFish(SoloGameSession.UNIQUE_FISH_ID));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private static void PlaceOutsidePort(BoatController boat, Vector3 departure)
        {
            // Leave room for the natural shoreward drift while fighting with the sail down.
            PlaceBoat(boat, departure + new Vector3(4.6f, 0f, 4f));
        }

        private static MouseState MouseAt(UnityEngine.Camera camera, GameObject target)
        {
            Vector3 screen = camera.WorldToScreenPoint(target.transform.position);
            Assert.Greater(screen.z, 0f);
            Assert.IsTrue(camera.pixelRect.Contains(screen), $"Fish was outside the frame at {screen}");
            return new MouseState { position = screen };
        }

        private static bool TensionIsVisible() => SceneComponent<GameplayHud>()
            .GetComponentsInChildren<Text>().Any(label => label.text.StartsWith("Натяжение троса:"));
    }
}
