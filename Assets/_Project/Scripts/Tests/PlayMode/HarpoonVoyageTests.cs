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
        public IEnumerator RunInputChangesOutcomeAndTheLineCanBreakWithoutAnOperator()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            PlaceOutsidePort(boat);

            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var harpoon = SceneComponent<HarpoonStation>();
                var player = SceneComponent<PlayerController>();
                yield return EnterHarpoon(player, harpoon, keyboard);
                var camera = SceneComponent<UnityEngine.Camera>();
                var target = GameObject.Find("CrimsonFish");
                yield return HookFish(mouse, camera, target, harpoon);
                Assert.That(harpoon.Tension, Is.EqualTo(0.5f).Within(0.04f));
                Assert.IsTrue(harpoon.Running);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                float startDistance = HorizontalDistance(harpoon, target);
                float startTension = harpoon.Tension;
                yield return MoveMouse(mouse, harpoon, against: false, 0.2f);
                Assert.Greater(HorizontalDistance(harpoon, target), startDistance + 0.3f);
                float followTension = harpoon.Tension - startTension;
                startDistance = HorizontalDistance(harpoon, target);
                startTension = harpoon.Tension;
                yield return MoveMouse(mouse, harpoon, against: true, 0.2f);
                Assert.That(HorizontalDistance(harpoon, target), Is.EqualTo(startDistance).Within(0.18f));
                Assert.Greater(harpoon.Tension - startTension, followTension + 0.025f);
                Assert.IsTrue(TensionIsVisible());

                var line = harpoon.GetComponent<LineRenderer>();
                while (harpoon.Running && harpoon.Tension < 0.82f)
                    yield return MoveMouse(mouse, harpoon, against: true, Time.deltaTime);
                Assert.GreaterOrEqual(harpoon.Tension, 0.8f);
                Assert.AreEqual(Color.red, LineColor(line));
                float oldLength = harpoon.Length;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return MoveMouse(mouse, harpoon, against: true, 0.21f);
                Assert.Greater(harpoon.Length, oldLength);
                Assert.Less(harpoon.Tension, 0.8f);
                Vector4 originalColor = line.sharedMaterial.GetColor("_BaseColor");
                Vector4 restoredColor = LineColor(line);
                Assert.That((originalColor - restoredColor).magnitude, Is.LessThan(0.001f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                while (harpoon.Running && harpoon.Tension < 0.91f)
                    yield return MoveMouse(mouse, harpoon, against: true, Time.deltaTime);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                Assert.AreEqual(PlayerControlMode.Walking, player.ControlMode);
                Assert.IsTrue(harpoon.Fighting);
                Assert.IsTrue(line.enabled);
                yield return WaitFor(() => !harpoon.Fighting, "Unattended fight did not resolve");
                Assert.That(harpoon.RecentLoss, Does.Contain("оборвался"));
                Assert.AreEqual(0, App.Session.UniqueFishCount);
                Assert.IsFalse(line.enabled);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator ALongShotMissesAndPayingOutAtTheLimitLosesTheFish()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            PlaceOutsidePort(boat);
            Vector3 fishingPosition = boat.transform.position;
            var target = GameObject.Find("CrimsonFish");
            var harpoon = SceneComponent<HarpoonStation>();
            var player = SceneComponent<PlayerController>();
            var camera = SceneComponent<UnityEngine.Camera>();
            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                PlaceAtFishDistance(boat, target, 23f);
                yield return EnterHarpoon(player, harpoon, keyboard);
                InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
                var trace = harpoon.GetComponent<LineRenderer>();
                yield return WaitFor(() => trace.enabled, "The out-of-range shot did not show a trace");
                yield return WaitFor(() => !trace.enabled, "The missed shot did not disappear");
                Assert.IsFalse(harpoon.Fighting);
                StringAssert.Contains("Промах", harpoon.Status);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                PlaceBoat(boat, fishingPosition);
                yield return new WaitForSeconds(0.6f);

                yield return HookFish(mouse, camera, target, harpoon);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                float deadline = Time.realtimeSinceStartup + 15f;
                while (harpoon.Fighting)
                {
                    Assert.Less(Time.realtimeSinceStartup, deadline, "Paying out should let the fish escape");
                    yield return MoveMouse(mouse, harpoon, against: false, Time.deltaTime);
                }
                StringAssert.Contains("пределе лески", harpoon.RecentLoss);
                Assert.AreEqual(0, App.Session.UniqueFishCount);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator CatchCardReleasesKeepsAndReplacesOneCarcassAcrossVoyages()
        {
            yield return StartNewGame();
            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                for (int voyage = 0; voyage < 2; voyage++)
                {
                    Assert.IsTrue(App.Depart());
                    yield return WaitForScene(GameScenes.SEA);
                    var boat = SceneComponent<BoatController>();
                    Vector3 departure = boat.transform.position;
                    PlaceOutsidePort(boat);
                    Vector3 fishingPosition = boat.transform.position;
                    var harpoon = SceneComponent<HarpoonStation>();
                    var player = SceneComponent<PlayerController>();
                    var camera = SceneComponent<UnityEngine.Camera>();
                    var target = GameObject.Find("CrimsonFish");
                    var carcass = boat.transform.Find("CrimsonFishOnDeck").gameObject;
                    yield return EnterHarpoon(player, harpoon, keyboard);
                    yield return LandFish(mouse, keyboard, camera, target, harpoon);
                    Assert.AreEqual(voyage, App.Session.UniqueFishCount, "Securing does not stow a fish");
                    Assert.IsFalse(harpoon.CardOpen);
                    Assert.IsFalse(TensionIsVisible());
                    yield return OpenCard(mouse, camera, target, harpoon);
                    var details = GameObject.Find("CatchDetails").GetComponent<Text>();
                    StringAssert.Contains("Редкая", details.text);
                    if (voyage == 1)
                        StringAssert.Contains("Место занято.\nЗаменить тушу на палубе?", details.text);
                    Canvas.ForceUpdateCanvases();
                    Assert.That(details.preferredHeight, Is.LessThanOrEqualTo(details.rectTransform.rect.height));

                    if (voyage == 0)
                    {
                        Click(GameObject.Find("ReleaseFish").GetComponent<Button>());
                        Assert.AreEqual(0, App.Session.UniqueFishCount);
                        Assert.IsFalse(carcass.activeSelf);
                        Assert.IsFalse(harpoon.Secured);
                        PlaceBoat(boat, fishingPosition);
                        yield return new WaitForSeconds(0.6f);
                        yield return LandFish(mouse, keyboard, camera, target, harpoon);
                        yield return OpenCard(mouse, camera, target, harpoon);
                    }

                    var keep = GameObject.Find("KeepFish").GetComponent<Button>();
                    Assert.AreEqual(voyage == 0 ? "Забрать" : "Заменить", keep.GetComponentInChildren<Text>().text);
                    Click(keep);
                    Assert.AreEqual(1, App.Session.UniqueFishCount);
                    Assert.IsTrue(App.Session.UniqueFishCaughtThisVoyage);
                    Assert.AreEqual(0, App.Session.Cargo.Count);
                    Assert.IsFalse(target.activeSelf);
                    Assert.IsTrue(carcass.activeSelf);
                    yield return null;
                    StringAssert.Contains("Туша 1/1", CargoLabel().text);
                    Assert.IsFalse(App.Session.TryStowUniqueFish(replace: true));

                    yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "Port zone was not armed");
                    PlaceBoat(boat, departure);
                    yield return WaitForScene(GameScenes.PORT);
                    Assert.AreEqual(1, App.Session.UniqueFishCount);
                }

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

        private static IEnumerator EnterHarpoon(PlayerController player, HarpoonStation harpoon, Keyboard keyboard)
        {
            PlacePlayer(player, harpoon);
            Assert.AreSame(harpoon, player.GetAvailableStation());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(PlayerControlMode.Harpoon, player.ControlMode);
        }

        private static IEnumerator HookFish(Mouse mouse, UnityEngine.Camera camera, GameObject target, HarpoonStation harpoon)
        {
            InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
            yield return WaitFor(() => harpoon.Fighting, $"The aimed shot did not reach the fish: {harpoon.Status}");
            InputSystem.QueueStateEvent(mouse, new MouseState());
        }

        private static IEnumerator LandFish(Mouse mouse, Keyboard keyboard, UnityEngine.Camera camera, GameObject target,
            HarpoonStation harpoon)
        {
            yield return HookFish(mouse, camera, target, harpoon);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (harpoon.Fighting)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Reeling and easing did not secure the fish");
                InputSystem.QueueStateEvent(keyboard, harpoon.Running && harpoon.Tension > 0.66f
                    ? new KeyboardState(Key.S) : new KeyboardState(Key.W));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = Vector2.right * -harpoon.RunDirection * 4f });
                yield return null;
            }
            Assert.IsTrue(harpoon.Secured, harpoon.Status);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator OpenCard(Mouse mouse, UnityEngine.Camera camera, GameObject target, HarpoonStation harpoon)
        {
            InputSystem.QueueStateEvent(mouse, MouseAt(camera, target).WithButton(MouseButton.Left));
            yield return WaitFor(() => harpoon.CardOpen, "Clicking the secured fish did not open its card");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return null;
        }

        private static IEnumerator MoveMouse(Mouse mouse, HarpoonStation harpoon, bool against, float seconds)
        {
            float end = Time.time + seconds;
            do
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = Vector2.right * harpoon.RunDirection * (against ? -4f : 4f) });
                yield return null;
            }
            while (Time.time < end);
        }

        private static void PlaceOutsidePort(BoatController boat)
        {
            PlaceBoat(boat, boat.transform.position + new Vector3(4.6f, 0f, 4f));
            boat.Wind.SetWind(Vector3.forward, 0f, 1f);
        }

        private static void PlaceAtFishDistance(BoatController boat, GameObject target, float distance)
        {
            Vector3 position = target.transform.position - boat.transform.forward * (distance + 2f);
            position.y = boat.transform.position.y;
            PlaceBoat(boat, position);
        }

        private static float HorizontalDistance(HarpoonStation harpoon, GameObject target) =>
            Vector3.ProjectOnPlane(target.transform.position - harpoon.transform.position, Vector3.up).magnitude;

        private static MouseState MouseAt(UnityEngine.Camera camera, GameObject target)
        {
            Vector3 screen = camera.WorldToScreenPoint(target.transform.position);
            Assert.Greater(screen.z, 0f);
            Assert.IsTrue(camera.pixelRect.Contains(screen), $"Fish was outside the frame at {screen}");
            return new MouseState { position = screen };
        }

        private static Color LineColor(LineRenderer line)
        {
            var properties = new MaterialPropertyBlock();
            line.GetPropertyBlock(properties);
            return properties.GetColor("_BaseColor");
        }

        private static bool TensionIsVisible() => SceneComponent<GameplayHud>()
            .GetComponentsInChildren<Text>().Any(label => label.text.StartsWith("Натяжение троса:"));
    }
}
