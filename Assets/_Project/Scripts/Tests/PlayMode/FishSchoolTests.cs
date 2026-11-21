using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class FishSchoolTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator HullCollectsOnceAndFishFollowTheBoatEvenAfterContinuingTheVoyage()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var session = App.Session;
            Assert.AreSame(session, SceneComponent<HomeShallowsController>().Session);
            var boat = SceneComponent<BoatController>();
            var first = Schools()[0];
            first.name = "Renamed School";
            var stranger = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Collider>();
            stranger.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            stranger.transform.position = first.transform.position;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(first.TryCollect(stranger));
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(0, session.Cargo.Count);
            Object.Destroy(stranger.gameObject);
            long revision = session.Revision;

            PlaceBoat(boat, first.transform.position + Vector3.up);
            yield return WaitFor(() => !first.gameObject.activeSelf, "The hull did not trigger the school");
            Assert.AreEqual(10, session.Cargo.Count);
            Assert.AreEqual(revision + 1, session.Revision);
            Assert.IsFalse(first.TryCollect(Hull(boat)));
            Assert.AreEqual(revision + 1, session.Revision);

            var hold = SceneComponent<CargoHold>();
            yield return WaitFor(() => hold.GetComponentsInChildren<CargoFish>().Length == 10,
                "The hold did not render the session catch");
            var fish = hold.GetComponentInChildren<CargoFish>();
            var body = fish.GetComponent<Rigidbody>();
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            Assert.IsFalse(body.detectCollisions);
            yield return new WaitForSeconds(0.5f);
            Vector3 slot = fish.transform.localPosition;
            Quaternion heading = fish.transform.localRotation;

            PlaceBoat(boat, new Vector3(-15f, 1.6f, 26f));
            boat.transform.rotation = Quaternion.Euler(0f, 83f, 0f);
            yield return new WaitForFixedUpdate();
            Assert.AreSame(hold.transform, fish.transform.parent);
            Assert.That(Vector3.Distance(slot, fish.transform.localPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(heading, fish.transform.localRotation), Is.LessThan(0.01f));

            Assert.IsTrue(App.OpenMenu());
            yield return WaitForScene(GameScenes.MENU);
            Assert.IsTrue(App.ContinueGame());
            yield return WaitForScene(GameScenes.SEA);
            Assert.IsFalse(Schools()[0].gameObject.activeSelf);
            Assert.AreEqual(10, session.Cargo.Count);
            Assert.AreEqual(10, SceneComponent<CargoHold>().GetComponentsInChildren<CargoFish>().Length);
        }

        [UnityTest]
        public IEnumerator SchoolFillsOnlyRemainingSpaceAndFullHoldLeavesTheNextSchoolAvailable()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var cargo = App.Session.Cargo;
            for (int i = 0; i < cargo.Capacity - 3; i++)
                Assert.IsTrue(App.Session.CatchFish(cargo.FishId));

            var boat = SceneComponent<BoatController>();
            var schools = Schools();
            PlaceBoat(boat, schools[0].transform.position + Vector3.up);
            yield return WaitFor(() => cargo.IsFull, "The first school did not fill the remaining cargo space");
            Assert.IsFalse(schools[0].gameObject.activeSelf);
            long revision = App.Session.Revision;
            PlaceBoat(boat, schools[1].transform.position + Vector3.up);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(schools[1].gameObject.activeSelf);
            Assert.IsFalse(schools[1].TryCollect(Hull(boat)));
            Assert.AreEqual(cargo.Capacity, cargo.Count);
            Assert.AreEqual(revision, App.Session.Revision);
        }

        private static Collider Hull(BoatController boat) => boat.GetComponentsInChildren<Collider>()
            .Single(SceneComponent<HomeShallowsController>().IsHull);
    }
}
