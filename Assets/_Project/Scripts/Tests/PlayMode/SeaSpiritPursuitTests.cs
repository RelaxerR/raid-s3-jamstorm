using System.Collections;
using System.Linq;
using System.Reflection;
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
            Assert.IsTrue(App.Session.CatchFish(FishCargo.AzureFish));
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
            var director = SceneComponent<SeaSpiritDirector>();
            var deck = SceneComponent<DeckCatch>();
            // The loss band, species accounting and simultaneous click have dedicated tests.
            typeof(DeckCatch).GetField("_lossChance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(deck, 0f);
            var cargo = App.Session.Cargo;
            var schools = Schools();
            foreach (int firstSchool in new[] { 0, 4 })
            {
                PlacePlayer(player, net);
                if (net.Operator == null) Assert.IsTrue(net.TryEnter(player));
                yield return SetNetState(net, NetState.Deployed);
                int expectedCatch = 0;
                for (int i = firstSchool; i < firstSchool + 3; i++)
                {
                    expectedCatch = Mathf.Min(100, expectedCatch + (i == 0 ? 10 : 45));
                    PlaceBoat(boat, schools[i].transform.position + Vector3.up);
                    yield return WaitFor(() => net.Count == expectedCatch, "Net did not catch the school");
                }
                Assert.AreEqual(100, net.Count);
                yield return SetNetState(net, NetState.Stowed);
                player.ExitStation();

                Assert.AreEqual(100, App.Session.DeckFishCount);
                Assert.IsFalse(director.PursuitRequested);
                while (App.Session.DeckFishCount > 0)
                {
                    yield return WaitFor(() => PickableFish(deck).Any(), "Missing deck fish");
                    foreach (FishOnDeck fish in PickableFish(deck))
                        Assert.AreEqual(DeckFishStoreResult.Stored, fish.TryStore(App.Session));
                }
            }
            Assert.AreEqual(FishCargo.AzureFish, cargo.First.FishId);
            Assert.AreEqual(FishCargo.AmberFish, cargo.Second.FishId);
            Assert.IsTrue(cargo.IsFull);
            yield return WaitFor(() => director.IsPursuing, "The full barrels did not start a spirit pursuit");
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
            Assert.AreEqual(2000, App.Session.Coins);
            Assert.IsFalse(SceneComponent<HomePortController>().Sell());
        }

        private static System.Collections.Generic.IEnumerable<FishOnDeck> PickableFish(DeckCatch deck) =>
            deck.GetComponentsInChildren<FishOnDeck>().Where(fish => fish.GetComponent<Collider>().enabled);

        [UnityTest]
        public IEnumerator SpiritAtHullCenterKeepsAttackingUntilTheBoatSinks()
        {
            yield return StartNewGame();
            var session = App.Session;
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);

            var boat = SceneComponent<BoatController>();
            var director = SceneComponent<SeaSpiritDirector>();
            var openWater = new Vector3(-20f, boat.transform.position.y, 20f);
            PlaceBoat(boat, openWater);
            for (int i = 0; i < session.Cargo.First.Capacity; i++)
            {
                Assert.IsTrue(session.CatchFish(FishCargo.AzureFish));
                Assert.IsTrue(session.CatchFish(FishCargo.AmberFish));
            }

            yield return WaitFor(() => director.IsPursuing, "A full hold did not attract the spirit");
            var spirit = director.ActiveSpirit;
            spirit.transform.position = boat.transform.position + Vector3.up * 1.5f;

            int startingHp = session.Boat.MaxHp;
            for (int hit = 1; hit <= 11; hit++)
            {
                int expectedHp = startingHp - 5 * hit;
                yield return WaitFor(() => session.Boat.Hp <= expectedHp,
                    $"Spirit attack {hit} did not lower the hull to {expectedHp} HP");
                Assert.That(session.Boat.Hp, Is.EqualTo(expectedHp));
                if (hit == 5 || hit == 6)
                    Assert.That(session.Boat.GetZoneHp(BoatZone.Mast), Is.Zero);
            }

            yield return WaitForScene(GameScenes.PORT);
            Assert.AreSame(session, App.Session);
            Assert.IsFalse(session.AtSea);
            Assert.That(session.Boat.Hp, Is.EqualTo(18));
            Assert.That(session.Cargo.Count, Is.EqualTo(100));
            Assert.IsTrue(spirit == null, "The spirit must unload when the damaged boat returns to port");
        }
    }
}
