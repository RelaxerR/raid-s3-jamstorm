using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            Assert.IsTrue(HasText(panel, "Улов 0/100"));
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
            Assert.IsTrue(HasText(panel, "Улов 10/100"));
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
            Assert.AreEqual("Улов 0/100", CargoLabel().text);

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

        private static Button ButtonIn(PortTradePanel panel, string name) =>
            panel.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

        private static bool HasText(PortTradePanel panel, string fragment) =>
            panel.GetComponentsInChildren<Text>().Any(text => text.text.Contains(fragment));

        private static void Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, button.name);
            Assert.IsTrue(button.interactable, button.name);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, $"{button.name} at {pointer.position} on {Screen.width}×{Screen.height}");
            Assert.AreSame(button.gameObject, hits[0].gameObject, button.name);
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
