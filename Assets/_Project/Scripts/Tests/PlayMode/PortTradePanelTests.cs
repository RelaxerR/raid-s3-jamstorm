using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class PortTradePanelTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator PortButtonsReflectSessionAcrossSalePurchaseAndNewVoyage()
        {
            yield return StartNewGame();
            var session = App.Session;
            var port = SceneComponent<HomePortController>();
            Assert.AreSame(session, port.Session);
            var eventSystem = SceneComponent<InputSystemUIInputModule>();
            Assert.IsTrue(eventSystem.enabled);
            Assert.NotNull(eventSystem.leftClick.action);
            Assert.NotNull(eventSystem.point.action);

            var panel = SceneComponent<PortTradePanel>();
            var open = ButtonIn(panel, "OpenTrade");
            var close = ButtonIn(panel, "CloseTrade");
            open.name = "Renamed Open Button";
            close.name = "Renamed Close Button";
            Assert.IsFalse(close.gameObject.activeInHierarchy);
            Click(open);
            yield return null;
            Assert.IsTrue(close.gameObject.activeInHierarchy);
            Assert.IsFalse(ButtonIn(panel, "SellFish").interactable);
            Assert.IsFalse(ButtonIn(panel, "SellAll").interactable);
            Assert.IsFalse(ButtonIn(panel, "BuyUpgrade").interactable);
            Assert.IsTrue(HasText(panel, "Улов 0/200"));
            Assert.IsTrue(HasText(panel, "Монеты: 0"));
            Assert.IsTrue(HasText(panel, "100 монет"));
            Click(close);
            yield return null;
            Assert.IsFalse(close.gameObject.activeInHierarchy);
            Click(open);
            yield return null;
            Assert.IsTrue(close.gameObject.activeInHierarchy);
            Click(ButtonIn(panel, "Depart"));

            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var departure = boat.transform.position;
            var school = Schools()[0];
            yield return LandSchoolAndStoreFish(boat, school, 10);
            Assert.AreEqual(10, session.Cargo.Count);
            yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "The boat has not cleared the port");
            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);

            panel = SceneComponent<PortTradePanel>();
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Assert.IsTrue(HasText(panel, "Улов 10/200"));
            Assert.IsTrue(ButtonIn(panel, "SellFish").interactable);
            Assert.IsFalse(ButtonIn(panel, "BuyUpgrade").interactable);
            long revision = session.Revision;
            Click(ButtonIn(panel, "SellFish"));
            Assert.AreEqual(revision + 1, session.Revision);
            Assert.AreEqual(100, session.Coins);
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.IsTrue(HasText(panel, "Монеты: 100"));
            Assert.IsFalse(ButtonIn(panel, "SellAll").interactable);
            Assert.IsFalse(ButtonIn(panel, "SellFish").interactable);
            Click(ButtonIn(panel, "BuyUpgrade"));
            Assert.AreEqual(0, session.Coins);
            Assert.IsTrue(session.Boat.SpeedUpgrade);
            Assert.IsTrue(HasText(panel, "Куплено"));
            Assert.IsFalse(ButtonIn(panel, "BuyUpgrade").interactable);
            Assert.IsFalse(SceneComponent<HomePortController>().BuyUpgrade());
            Click(ButtonIn(panel, "Depart"));

            yield return WaitForScene(GameScenes.SEA);
            Assert.AreSame(session, App.Session);
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.That(session.Boat.SpeedMultiplier, Is.EqualTo(1.4f));
            Assert.IsTrue(Schools()[0].gameObject.activeSelf);
            Assert.IsTrue(CargoLabel().gameObject.activeInHierarchy);
            Assert.AreEqual("Улов · Бочка 1 0/100 · пусто\nБочка 2 0/100 · пусто\nТуша 0/1", CargoLabel().text);

            boat = SceneComponent<BoatController>();
            departure = boat.transform.position;
            school = Schools()[0];
            yield return LandSchoolAndStoreFish(boat, school, 10);
            Assert.AreEqual(10, session.Cargo.Count);
            yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "The boat has not cleared the port");
            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);

            panel = SceneComponent<PortTradePanel>();
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Click(ButtonIn(panel, "SellAll"));
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.AreEqual(100, session.Coins);
            Assert.IsTrue(HasText(panel, "Куплено"));
            Assert.IsFalse(ButtonIn(panel, "BuyUpgrade").interactable);
            Assert.IsFalse(ButtonIn(panel, "SellAll").interactable);
        }
    }
}
