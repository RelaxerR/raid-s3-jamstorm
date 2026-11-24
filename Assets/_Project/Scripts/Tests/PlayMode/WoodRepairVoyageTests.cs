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
    public sealed class WoodRepairVoyageTests : VoyageTestFixture
    {
        private Keyboard _keyboard;
        private GameViewInputScope _input;

        [TearDown]
        public void RemoveKeyboard()
        {
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            _input?.Dispose();
        }

        [UnityTest]
        public IEnumerator BootToPurchaseAndRepairUsesOneStockAndRealDeckInput()
        {
            yield return StartNewGame();

            var session = App.Session;
            var panel = SceneComponent<PortTradePanel>();
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Assert.IsTrue(HasText(panel, "Древесина: 0"));
            Assert.IsFalse(ButtonIn(panel, "BuyWood").interactable);
            Click(ButtonIn(panel, "Depart"));

            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            Vector3 departure = boat.transform.position;
            yield return LandSchoolAndStoreFish(boat, Schools()[0], 10);
            yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "The boat has not cleared the port");
            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);

            panel = SceneComponent<PortTradePanel>();
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Click(ButtonIn(panel, "SellFish"));
            Assert.That(session.Coins, Is.EqualTo(100));
            long revision = session.Revision;
            Click(ButtonIn(panel, "BuyWood"));
            Assert.That(session.Coins, Is.EqualTo(90));
            Assert.That(session.Wood, Is.EqualTo(1));
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.IsTrue(HasText(panel, "Монеты: 90"));
            Assert.IsTrue(HasText(panel, "Древесина: 1"));
            Click(ButtonIn(panel, "Depart"));

            yield return WaitForScene(GameScenes.SEA);
            var voyage = SceneComponent<HomeShallowsController>();
            Assert.AreSame(session, voyage.Session);
            Assert.IsFalse(session.BuyWood());
            Assert.That(session.Coins, Is.EqualTo(90));
            var hud = SceneComponent<GameplayHud>();
            Text wood = hud.GetComponentsInChildren<Text>(true).Single(label => label.text == "Древесина: 1");
            Assert.IsTrue(wood.gameObject.activeInHierarchy);

            boat = SceneComponent<BoatController>();
            var debris = Object.FindObjectsByType<BoatDamageSource>(FindObjectsInactive.Exclude)
                .Single(source => source.name == "WaterDebris");
            Vector3 debrisCenter = debris.GetComponent<Collider>().bounds.center;
            PlaceBoat(boat, debrisCenter - boat.transform.TransformVector(new Vector3(-1.4f, 0f, 1.8f)));
            yield return WaitFor(() => session.Boat.Hp < 60, "The debris did not hit the hull");
            PlaceBoat(boat, new Vector3(-14f, 1.65f, 15f));
            Assert.That(session.Boat.GetZoneDamage(BoatZone.FrontLeft), Is.EqualTo(5));
            Assert.That(boat.Health.Current, Is.EqualTo(55));

            var repair = voyage.Repair;
            var player = SceneComponent<PlayerController>();
            var prompt = hud.GetComponentInChildren<BoatRepairView>(true);
            var track = (RectTransform)prompt.transform.Find("ProgressTrack");
            var fill = (RectTransform)track.Find("ProgressFill");
            var mark = boat.transform.Find("FrontLeftDamage").gameObject;

            _input = new GameViewInputScope();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            // Damage overlaps the helm and net interaction areas. Both still own E at their stations.
            boat.Health.TakeDamage(5f, boat.transform.TransformPoint(new Vector3(-0.72f, 0f, -1.18f)));
            var helm = SceneComponent<HelmStation>();
            SetDeckPosition(player, 0f, -1.78f);
            Assert.AreSame(helm, player.GetAvailableStation());
            yield return Keys(Key.E);
            Assert.AreSame(helm, player.ActiveStation);
            yield return AssertInterrupted(repair, track, boat, 50);
            yield return Keys();
            yield return Keys(Key.E);
            Assert.IsNull(player.ActiveStation);
            yield return Keys();

            var net = SceneComponent<NetStation>();
            PlacePlayer(player, net);
            yield return Keys(Key.E);
            Assert.AreSame(net, player.ActiveStation);
            yield return AssertInterrupted(repair, track, boat, 50);
            yield return Keys();
            yield return Keys(Key.E);
            yield return WaitFor(() => net.State == NetState.Deployed, "E did not lower the net");
            Assert.AreSame(net, player.ActiveStation);
            yield return Keys();
            yield return Keys(Key.Escape);
            Assert.IsNull(player.ActiveStation);
            yield return Keys();

            SetDeckPosition(player, -0.8f, 0.79f);
            yield return WaitFor(() => repair.SelectedZone == BoatZone.FrontLeft && prompt.gameObject.activeInHierarchy,
                "No prompt over the damaged bow");
            Assert.IsTrue(mark.activeInHierarchy);
            Assert.IsFalse(track.gameObject.activeInHierarchy);

            yield return Keys(Key.E);
            yield return WaitFor(() => fill.anchorMax.x > 0.1f, "The repair bar did not fill");
            Assert.IsTrue(track.gameObject.activeInHierarchy);
            Assert.That(boat.Health.Current, Is.EqualTo(50));
            Assert.That(session.Wood, Is.EqualTo(1));
            yield return Keys();
            yield return AssertInterrupted(repair, track, boat, 50);

            yield return Keys(Key.E);
            yield return WaitFor(() => repair.Progress > 0.1f, "Repair did not restart after releasing E");
            yield return Keys(Key.E, Key.S);
            yield return WaitFor(() => !repair.IsRepairing, "Leaving the zone did not cancel repair");
            yield return AssertInterrupted(repair, track, boat, 50);

            yield return Keys(Key.E);
            SetDeckPosition(player, -0.8f, 0.79f);
            yield return new WaitForSeconds(0.2f);
            yield return AssertInterrupted(repair, track, boat, 50);
            yield return Keys();
            revision = session.Revision;
            float holdStarted = Time.time;
            yield return Keys(Key.E);
            yield return WaitFor(() => repair.Progress > 0.75f, "The repair did not approach completion");
            Assert.That(session.Wood, Is.EqualTo(1));
            Assert.That(boat.Health.Current, Is.EqualTo(50));
            yield return WaitFor(() => session.Wood == 0, "Three seconds of holding E did not repair the zone");
            Assert.That(Time.time - holdStarted, Is.GreaterThanOrEqualTo(3f));
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.That(boat.Health.Current, Is.EqualTo(55));
            Assert.That(session.Boat.GetZoneDamage(BoatZone.FrontLeft), Is.Zero);
            Assert.That(session.Boat.GetZoneDamage(BoatZone.RearLeft), Is.EqualTo(5));
            yield return WaitFor(() => wood.text == "Древесина: 0", "The sea HUD did not reflect the repair");
            Assert.IsFalse(mark.activeSelf);
            Assert.IsFalse(track.gameObject.activeInHierarchy);
            Assert.IsTrue(hud.GetComponentsInChildren<Text>().Any(label => label.text.Contains("HP 55/60")));
            yield return Keys();

            SetDeckPosition(player, -0.8f, -1.1f);
            yield return WaitFor(() => repair.SelectedZone == BoatZone.RearLeft && prompt.gameObject.activeInHierarchy
                && prompt.GetComponentInChildren<Text>().text == "Нужна древесина",
                "The damaged stern did not show the missing wood prompt");
            revision = session.Revision;
            yield return Keys(Key.E);
            yield return new WaitForSeconds(3.1f);
            Assert.That(session.Revision, Is.EqualTo(revision));
            Assert.That(session.Wood, Is.Zero);
            Assert.That(boat.Health.Current, Is.EqualTo(55));
            Assert.That(repair.Progress, Is.Zero);
            Assert.IsFalse(track.gameObject.activeInHierarchy);
            yield return Keys();

            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);
            panel = SceneComponent<PortTradePanel>();
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Assert.IsTrue(HasText(panel, "Древесина: 0"));
            Assert.That(session.Boat.Hp, Is.EqualTo(55));
        }

        private IEnumerator AssertInterrupted(BoatRepairController repair, RectTransform track, BoatController boat, int hp)
        {
            yield return null;
            Assert.That(repair.Progress, Is.Zero);
            Assert.That(boat.Health.Current, Is.EqualTo(hp));
            Assert.That(App.Session.Wood, Is.EqualTo(1));
            Assert.IsFalse(track.gameObject.activeInHierarchy);
        }

        private IEnumerator Keys(params Key[] keys)
        {
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            yield return null;
            yield return null;
        }

        private static void SetDeckPosition(PlayerController player, float x, float z)
        {
            player.transform.localPosition = new Vector3(x, player.transform.localPosition.y, z);
        }
    }
}
