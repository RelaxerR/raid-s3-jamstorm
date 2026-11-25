using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.Player;
using SecondCatch.Solo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class GameplayHudCargoTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator HudShowsInitialCapacityAndRefreshesAfterCollection()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var cargo = CargoLabel();
            Assert.IsTrue(cargo.gameObject.activeInHierarchy);
            Assert.AreEqual("Улов · Бочка 1 0/100 · пусто\nБочка 2 0/100 · пусто\nТуша 0/1", cargo.text);
            var school = Schools()[0];
            yield return LandSchoolAndStoreFish(SceneComponent<BoatController>(), school, 10);
            yield return WaitFor(() => cargo.text.Contains("Бочка 1 10/100 · Лазурная"), "The cargo indicator did not refresh");
            Assert.AreEqual(10, App.Session.Cargo.Count);
        }

        [UnityTest]
        public IEnumerator HoldingENearBarrelReleasesOnlyThatBarrel()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var session = App.Session;
            Assert.IsTrue(session.CatchFish(FishCargo.AzureFish));
            Assert.IsTrue(session.CatchFish(FishCargo.AmberFish));
            var player = SceneComponent<PlayerController>();
            var first = player.Boat.transform.Find("Barrel1").GetComponent<CargoHold>();
            var position = first.transform.localPosition;
            position.y = player.transform.localPosition.y;
            player.transform.localPosition = position;
            Assert.AreSame(first, player.GetAvailableBarrel());

            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Assert.AreEqual(1, session.Cargo.First.Count, "A tap should not release the barrel");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                yield return WaitFor(() => session.Cargo.First.Count == 0, "Hold E did not empty the first barrel");
                Assert.IsNull(session.Cargo.First.FishId);
                Assert.AreEqual(1, session.Cargo.Second.Count);
                yield return WaitFor(() => CargoLabel().text.Contains("Бочка 2 1/100 · Янтарная"),
                    "HUD did not retain the second barrel after release");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator SeaSceneOpensWithoutSessionAndHidesCargo()
        {
            if (App != null)
            {
                Object.Destroy(App.gameObject);
                yield return null;
            }
            yield return SceneManager.LoadSceneAsync(GameScenes.Path(GameScenes.SEA));
            yield return null;
            Assert.IsNull(App);
            Assert.IsNull(SceneComponent<HomeShallowsController>().Session);
            Assert.IsFalse(CargoLabel().gameObject.activeInHierarchy);
        }
    }
}
