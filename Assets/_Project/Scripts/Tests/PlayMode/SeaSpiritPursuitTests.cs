using System.Collections;
using NUnit.Framework;
using SecondCatch.Boat;
using SecondCatch.Spirit;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class SeaSpiritPursuitTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator PartialCargoDoesNotSpawnSpirit()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(SecondCatch.Application.GameScenes.SEA);
            Assert.IsTrue(App.Session.CatchFish(App.Session.Cargo.FishId));
            yield return new WaitForSeconds(0.25f);

            var director = SceneComponent<SeaSpiritDirector>();
            Assert.IsFalse(director.PursuitRequested);
            Assert.IsFalse(director.IsPursuing);
            Assert.AreEqual(0, director.SpawnCount);
        }

        [UnityTest]
        public IEnumerator FullCargoSpawnsOnceAndAttacksThroughBoatHealth()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(SecondCatch.Application.GameScenes.SEA);
            while (!App.Session.Cargo.IsFull)
                App.Session.CatchFish(App.Session.Cargo.FishId);

            var director = SceneComponent<SeaSpiritDirector>();
            yield return WaitFor(() => director.IsPursuing, "The full barrel did not start a spirit pursuit");
            Assert.IsTrue(director.PursuitRequested);
            Assert.AreEqual(1, director.SpawnCount);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, director.SpawnCount);

            var boat = SceneComponent<BoatController>();
            director.ActiveSpirit.transform.position = boat.transform.position + Vector3.up * 1.5f;
            float hp = boat.Health.Current;
            yield return new WaitForSeconds(0.1f);
            Assert.That(boat.Health.Current, Is.EqualTo(hp - 5f).Within(0.01f));
            hp = boat.Health.Current;
            yield return new WaitForSeconds(0.5f);
            Assert.That(boat.Health.Current, Is.EqualTo(hp).Within(0.01f));

            Assert.IsTrue(App.ReturnToPort());
            yield return WaitForScene(SecondCatch.Application.GameScenes.PORT);
            Assert.IsFalse(App.Session.AtSea);
        }
    }
}
