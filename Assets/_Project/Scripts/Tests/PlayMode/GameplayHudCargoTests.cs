using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            Assert.AreEqual("Улов 0/100", cargo.text);
            var school = Schools()[0];
            PlaceBoat(SceneComponent<BoatController>(), school.transform.position + Vector3.up);
            yield return WaitFor(() => cargo.text == "Улов 10/100", "The cargo indicator did not refresh");
            Assert.AreEqual(10, App.Session.Cargo.Count);
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
