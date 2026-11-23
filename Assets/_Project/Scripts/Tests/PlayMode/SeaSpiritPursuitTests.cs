using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Fishing;
using SecondCatch.Player;
using SecondCatch.Solo;
using SecondCatch.Stations;
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
            yield return WaitForScene(GameScenes.SEA);
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
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            Vector3 departure = boat.transform.position;
            var player = SceneComponent<PlayerController>();
            var net = SceneComponent<NetStation>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);

            int expectedCatch = 0;
            var schools = Schools();
            foreach (FishSchool school in schools)
            {
                expectedCatch += school == schools[0] ? 10 : 45;
                PlaceBoat(boat, school.transform.position + Vector3.up);
                yield return WaitFor(() => net.Count == expectedCatch, "Net did not catch the school");
            }
            Assert.AreEqual(100, net.Count);
            yield return SetNetState(net, NetState.Stowed);
            player.ExitStation();
            Assert.AreEqual(100, App.Session.DeckFishCount);

            var director = SceneComponent<SeaSpiritDirector>();
            Assert.IsFalse(director.PursuitRequested);
            var deck = SceneComponent<DeckCatch>();
            var cargo = App.Session.Cargo;
            while (cargo.Count < cargo.Capacity - 1)
            {
                yield return WaitFor(() => deck.GetComponentInChildren<FishOnDeck>() != null, "Missing deck fish");
                foreach (FishOnDeck fish in deck.GetComponentsInChildren<FishOnDeck>())
                {
                    if (cargo.Count == cargo.Capacity - 1) break;
                    Assert.AreEqual(DeckFishStoreResult.Stored, fish.TryStore(App.Session));
                }
            }
            Assert.IsFalse(director.PursuitRequested);
            Assert.AreEqual(DeckFishStoreResult.Stored, deck.GetComponentInChildren<FishOnDeck>().TryStore(App.Session));
            Assert.AreEqual(0, App.Session.DeckFishCount);
            yield return WaitFor(() => director.IsPursuing, "The full barrel did not start a spirit pursuit");
            Assert.IsTrue(director.PursuitRequested);
            Assert.AreEqual(1, director.SpawnCount);
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(1, director.SpawnCount);

            director.ActiveSpirit.transform.position = boat.transform.position + Vector3.up * 1.5f;
            float hp = boat.Health.Current;
            yield return new WaitForSeconds(0.1f);
            Assert.That(boat.Health.Current, Is.EqualTo(hp - 5f).Within(0.01f));
            hp = boat.Health.Current;
            yield return new WaitForSeconds(0.5f);
            Assert.That(boat.Health.Current, Is.EqualTo(hp).Within(0.01f));

            var zone = SceneComponent<PortZone>();
            yield return WaitFor(() => zone.ReadyToDock, "Port zone was not armed");
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);
            Assert.IsFalse(App.Session.AtSea);
            Assert.IsTrue(player == null, "The voyage camera and its operator left with the sea scene");
            Assert.IsTrue(SceneComponent<HomePortController>().Sell());
            Assert.AreEqual(1000, App.Session.Coins);
        }
    }
}
