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
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondCatch.Tests
{
    public sealed class FishSchoolTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator BootVoyageUsesNetAndMouseOnTheMovingBoatAndReturnsToPort()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var session = App.Session;
            var boat = SceneComponent<BoatController>();
            Vector3 departure = boat.transform.position;
            var portZone = SceneComponent<PortZone>();
            var player = SceneComponent<PlayerController>();
            var sail = SceneComponent<SailStation>();
            var net = SceneComponent<NetStation>();
            var school = Schools()[0];
            var deck = SceneComponent<DeckCatch>();
            var hold = SceneComponent<CargoHold>();
            var feedbackAudio = deck.GetComponent<AudioSource>();
            Assert.NotNull(feedbackAudio);
            var camera = SceneComponent<UnityEngine.Camera>();

            using var input = new GameViewInputScope();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                PlacePlayer(player, net);
                Assert.AreSame(net, player.GetAvailableStation());
                yield return Press(keyboard, Key.E);
                Assert.AreSame(net, player.ActiveStation);
                Assert.AreEqual(NetState.Stowed, net.State);
                StringAssert.Contains("0/100", Hint());

                yield return Press(keyboard, Key.E);
                Assert.AreSame(net, player.ActiveStation);
                Assert.AreEqual(NetState.Lowering, net.State);
                yield return WaitFor(() => net.State == NetState.Deployed, "Net did not lower");
                yield return Press(keyboard, Key.Escape);
                Assert.AreEqual(PlayerControlMode.Walking, player.ControlMode);
                StringAssert.DoesNotContain("Сеть:", Hint());
                Assert.AreEqual(NetState.Deployed, net.State);

                player.transform.localPosition = new Vector3(0f, player.transform.localPosition.y, 0f);
                Assert.IsTrue(sail.TryEnter(player));
                // Держим курс западнее рифа, оставаясь в зоне первого косяка.
                boat.transform.rotation = Quaternion.Euler(0f, -10f, 0f);
                boat.SetSailRaised(true);
                player.ExitStation();
                PlacePlayer(player, net);
                yield return Press(keyboard, Key.E);
                Assert.AreSame(net, player.ActiveStation);

                yield return WaitFor(() => !school.gameObject.activeSelf, "Sailing through the first school did not yield fish");
                Assert.Greater(boat.LongitudinalSpeed, 0.5f);
                AssertInFrame(camera, school.transform.position);
                AssertInFrame(camera, net.transform.position);
                Assert.AreEqual(10, net.Count);
                Assert.AreEqual(0, session.Cargo.Count);
                Assert.AreEqual(0, session.DeckFishCount);
                yield return Press(keyboard, Key.E);
                Assert.AreEqual(NetState.Raising, net.State);
                yield return Press(keyboard, Key.E);
                Assert.AreEqual(NetState.Raising, net.State);
                yield return Press(keyboard, Key.Escape);
                Assert.AreEqual(PlayerControlMode.Walking, player.ControlMode);

                yield return WaitFor(() => deck.GetComponentsInChildren<FishOnDeck>().Length == 10, "Raise did not spawn deck fish");
                Assert.AreEqual(NetState.Stowed, net.State);
                Assert.AreEqual(0, net.Count);
                Assert.AreEqual(0, session.Cargo.Count);
                Assert.AreEqual("Улов 0/100", CargoLabel().text);
                StringAssert.Contains("ЛКМ", Hint());

                foreach (float x in new[] { -1f, 1f })
                    foreach (float z in new[] { -2f, 2f })
                        AssertInFrame(camera, boat.transform.TransformPoint(new Vector3(x, 0f, z)));

                FishOnDeck fish = PickableFish(deck, camera);
                Assert.AreSame(boat.transform, fish.transform.parent.parent);
                Vector2 target = camera.WorldToScreenPoint(fish.transform.position);
                Vector3 start = hold.transform.InverseTransformPoint(fish.transform.position);
                float clickedAt = Time.time;
                yield return Click(mouse, target);
                Assert.AreEqual(1, session.Cargo.Count);
                Assert.AreEqual(9, session.DeckFishCount);
                Assert.AreSame(hold.transform, fish.transform.parent);
                Assert.IsTrue(fish.gameObject.activeInHierarchy);
                Assert.IsTrue(fish.GetComponent<MeshRenderer>().enabled);
                Assert.IsFalse(fish.GetComponent<Collider>().enabled);
                Assert.AreEqual(DeckFishStoreResult.Unavailable, fish.TryStore(session));
                Assert.AreEqual(1, session.Cargo.Count);
                Assert.IsEmpty(hold.GetComponentsInChildren<CargoFish>());
                Assert.AreEqual("sfx_fish_pickup", feedbackAudio.clip.name);
                yield return WaitFor(() => fish != null && Vector3.Distance(start, fish.transform.localPosition) > 0.08f,
                    "The selected fish did not travel to the hold");
                yield return WaitFor(() => fish == null, "The transfer did not finish");
                Assert.That(Time.time - clickedAt, Is.InRange(0.19f, 0.4f));
                Assert.AreEqual(1, hold.GetComponentsInChildren<CargoFish>().Length);
                yield return WaitFor(() => CargoLabel().text == "Улов 1/100", "HUD did not show the stored fish");

                Vector2 plainDeck = camera.WorldToScreenPoint(boat.transform.TransformPoint(new Vector3(0.65f, 0.06f, 1.65f)));
                Assert.IsTrue(camera.pixelRect.Contains(plainDeck));
                yield return Click(mouse, plainDeck);
                Assert.AreEqual(1, session.Cargo.Count);

                FishOnDeck incompatible = PickableFish(deck, camera);
                FieldInfo fishId = typeof(FishOnDeck).GetField("_fishId", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(fishId);
                fishId.SetValue(incompatible, "fish.other");
                yield return Click(mouse, camera.WorldToScreenPoint(incompatible.transform.position));
                yield return WaitFor(() => Hint().Contains("Этот вид рыбы"), "HUD did not explain the incompatible fish");
                Assert.AreEqual("sfx_ui_error", feedbackAudio.clip.name);
                Assert.IsTrue(incompatible.GetComponent<Collider>().enabled);
                Assert.AreSame(deck.transform, incompatible.transform.parent);
                Assert.AreEqual(1, session.Cargo.Count);
                fishId.SetValue(incompatible, session.Cargo.FishId);

                while (!session.Cargo.IsFull)
                    Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
                FishOnDeck rejected = PickableFish(deck, camera);
                yield return Click(mouse, camera.WorldToScreenPoint(rejected.transform.position));
                yield return WaitFor(() => Hint().Contains("Бочка заполнена"), "HUD did not explain the rejection");
                Assert.AreEqual("sfx_barrel_full", feedbackAudio.clip.name);
                Assert.IsTrue(rejected.gameObject.activeSelf);
                Assert.IsTrue(rejected.GetComponent<Collider>().enabled);
                Assert.AreEqual(9, session.DeckFishCount);

                Assert.IsTrue(portZone.ReadyToDock);
                Vector3 homeward = Vector3.ProjectOnPlane(departure - boat.transform.position, Vector3.up).normalized;
                boat.ResetMotion();
                boat.transform.rotation = Quaternion.LookRotation(homeward, Vector3.up);
                boat.Wind.SetWind(homeward, boat.Wind.MaxSpeed, 1f);
                yield return WaitForScene(GameScenes.PORT);
                Assert.IsFalse(session.AtSea);
                Assert.AreEqual(0, session.DeckFishCount);
                Assert.AreEqual(session.Cargo.Capacity, session.Cargo.Count, "Docking must preserve the barrel");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator NetRaisesUseDifferentFreeDeckPoints()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var deck = SceneComponent<DeckCatch>();
            var net = SceneComponent<NetStation>();
            var player = SceneComponent<PlayerController>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            Vector2Int[] firstPoints = null;
            var randomState = Random.state;
            try
            {
                for (int haul = 0; haul < 3; haul++)
                {
                    yield return SetNetState(net, NetState.Deployed);
                    int catchSize = haul < 2 ? 10 : 15;
                    Assert.AreEqual(catchSize, App.Session.TakeSchoolIntoNet(90 + haul, catchSize, 100));
                    Random.InitState(124 + haul);
                    yield return SetNetState(net, NetState.Stowed);
                    int visible = haul < 2 ? 10 : 20;
                    yield return WaitFor(() => deck.GetComponentsInChildren<FishOnDeck>().Length == visible,
                        "The raise did not fill the available deck points");
                    var fish = deck.GetComponentsInChildren<FishOnDeck>();
                    Vector2Int[] occupied = fish.Select(DeckPoint).ToArray();
                    Assert.AreEqual(visible, occupied.Distinct().Count());
                    Assert.IsTrue(occupied.All(point => point.x >= 0 && point.x < 4 && point.y >= 0 && point.y < 5));

                    if (haul == 0)
                    {
                        firstPoints = occupied;
                        foreach (FishOnDeck selected in fish)
                            Assert.AreEqual(DeckFishStoreResult.Stored, selected.TryStore(App.Session));
                    }
                    else if (haul == 1)
                    {
                        Assert.IsFalse(firstPoints.ToHashSet().SetEquals(occupied), "Different seeds chose the same deck points");
                    }
                }
                Assert.AreEqual(25, App.Session.DeckFishCount);
            }
            finally { Random.state = randomState; }
        }

        private static Vector2Int DeckPoint(FishOnDeck fish)
        {
            Vector3 position = fish.transform.localPosition;
            return new Vector2Int(Mathf.RoundToInt(position.x / 0.38f + 1.5f), Mathf.RoundToInt(position.z / 0.33f + 2f));
        }

        [UnityTest]
        public IEnumerator StationaryHullInsideSchoolCatchesOnlyAfterStartingToMove()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var session = App.Session;
            Assert.AreSame(session, SceneComponent<HomeShallowsController>().Session);
            var boat = SceneComponent<BoatController>();
            var first = Schools()[0];
            var player = SceneComponent<PlayerController>();
            var net = SceneComponent<NetStation>();
            SceneComponent<WaveController>().enabled = false;
            PlaceBoat(boat, first.transform.position + Vector3.up);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.AreEqual(0, net.Count);

            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, boat.CurrentSpeed, 0.001f);
            Assert.AreEqual(0, net.Count, "Lowering the net at rest must not take fish");
            Assert.IsTrue(first.gameObject.activeSelf);

            boat.SetSailRaised(true);
            yield return WaitFor(() => net.Count == 10, "The net missed the school when the hull began to move");
            Assert.GreaterOrEqual(boat.CurrentSpeed, 0.05f);
            Assert.AreEqual(0, session.Cargo.Count);
            Collider hull = boat.GetComponentsInChildren<Collider>().Single(SceneComponent<HomeShallowsController>().IsHull);
            Assert.IsFalse(first.TryCollect(hull));
        }

        [UnityTest]
        public IEnumerator NetAndDeckCatchSurviveContinue()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var session = App.Session;
            var net = SceneComponent<NetStation>();
            var player = SceneComponent<PlayerController>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);
            var school = Schools()[0];
            PlaceBoat(SceneComponent<BoatController>(), school.transform.position + Vector3.up);
            yield return WaitFor(() => net.Count == 10, "The drifting hull did not catch the school");

            Assert.IsTrue(App.OpenMenu());
            yield return WaitForScene(GameScenes.MENU);
            Assert.IsTrue(App.ContinueGame());
            yield return WaitForScene(GameScenes.SEA);
            net = SceneComponent<NetStation>();
            player = SceneComponent<PlayerController>();
            Assert.AreEqual(NetState.Deployed, net.State);
            Assert.AreEqual(10, net.Count);
            Assert.IsFalse(Schools()[0].gameObject.activeSelf);
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Stowed);
            player.ExitStation();

            var deck = SceneComponent<DeckCatch>();
            yield return WaitFor(() => deck.GetComponentsInChildren<FishOnDeck>().Length == 10, "Loose fish did not appear");
            var fish = deck.GetComponentInChildren<FishOnDeck>();
            Assert.AreEqual(DeckFishStoreResult.Stored, fish.TryStore(session));
            Assert.AreEqual(DeckFishStoreResult.Unavailable, fish.TryStore(session));
            yield return WaitFor(() => SceneComponent<CargoHold>().GetComponentsInChildren<CargoFish>().Length == 1,
                "The hold did not render the stored fish");

            Assert.IsTrue(App.OpenMenu());
            yield return WaitForScene(GameScenes.MENU);
            Assert.IsTrue(App.ContinueGame());
            yield return WaitForScene(GameScenes.SEA);
            Assert.AreEqual(1, session.Cargo.Count);
            Assert.AreEqual(9, SceneComponent<DeckCatch>().GetComponentsInChildren<FishOnDeck>().Length);
            Assert.IsFalse(Schools()[0].gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator FullNetLeavesSchoolAvailableAndLosesPartOfCatchUnderSail()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var player = SceneComponent<PlayerController>();
            var net = SceneComponent<NetStation>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);

            var schools = Schools();
            Assert.AreEqual(100, App.Session.TakeSchoolIntoNet(99, 100, 100));
            PlaceBoat(boat, schools[0].transform.position + Vector3.up);
            yield return WaitFor(() => boat.CurrentSpeed >= 0.05f, "Hull did not start drifting");
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(100, net.Count);
            Assert.IsTrue(schools[0].gameObject.activeSelf);
            Assert.AreEqual(0, App.Session.Cargo.Count);

            PlaceBoat(boat, new Vector3(-15f, boat.transform.position.y, 26f));
            boat.SetSailRaised(true);
            yield return CheckSpill(net, 100, 90, 81);
            Assert.Greater(boat.CurrentSpeed, 0.2f);
            yield return SetNetState(net, NetState.Stowed);
            Assert.AreEqual(81, App.Session.DeckFishCount);
            Assert.AreEqual(0, net.Count);
            Assert.AreEqual(0, App.Session.Cargo.Count);
        }

        [UnityTest]
        public IEnumerator SmallCatchLosesTenPercentEachSecondRoundedUp()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var player = SceneComponent<PlayerController>();
            var net = SceneComponent<NetStation>();
            PlacePlayer(player, net);
            Assert.IsTrue(net.TryEnter(player));
            yield return SetNetState(net, NetState.Deployed);
            PlaceBoat(boat, new Vector3(-15f, boat.transform.position.y, 26f));
            Assert.AreEqual(10, App.Session.TakeSchoolIntoNet(99, 10, 100));
            yield return new WaitForSeconds(4.1f);
            Assert.AreEqual(10, net.Count, "No fish should be lost while the boat is below the spill speed");

            boat.SetSailRaised(true);
            yield return CheckSpill(net, 10, 9, 8);
        }

        private static IEnumerator CheckSpill(NetStation net, int initial, int first, int second)
        {
            yield return new WaitForSeconds(2.9f);
            Assert.AreEqual(initial, net.Count, "Loss started before the grace period ended");
            yield return WaitFor(() => net.Count < initial, "First second of loss did not finish");
            Assert.AreEqual(first, net.Count);
            yield return WaitFor(() => net.Count < first, "Second second of loss did not finish");
            Assert.AreEqual(second, net.Count);
        }

        private static FishOnDeck PickableFish(DeckCatch deck, UnityEngine.Camera camera)
        {
            Physics.SyncTransforms();
            return deck.GetComponentsInChildren<FishOnDeck>().First(candidate =>
            {
                Vector3 point = camera.WorldToScreenPoint(candidate.transform.position);
                return camera.pixelRect.Contains(point)
                    && Physics.Raycast(camera.ScreenPointToRay(point), out var hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    && hit.collider.gameObject == candidate.gameObject;
            });
        }

        private static void AssertInFrame(UnityEngine.Camera camera, Vector3 worldPoint)
        {
            Vector3 point = camera.WorldToViewportPoint(worldPoint);
            Assert.That(point.x, Is.InRange(0f, 1f));
            Assert.That(point.y, Is.InRange(0f, 1f));
            Assert.Greater(point.z, 0f);
        }

        private static string Hint() => SceneComponent<InteractionHintView>().GetComponentInChildren<Text>().text;

        private static IEnumerator Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator Click(Mouse mouse, Vector2 position)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
        }
    }
}
