using System.Collections;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.Solo;
using SecondCatch.UI;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class PortContractPanelTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator PortMasterAcceptsStoredNetCatchAfterReturningToPort()
        {
            yield return StartNewGame();
            var session = App.Session;
            var panel = SceneComponent<PortTradePanel>();
            var action = ButtonIn(panel, "ContractAction");
            Assert.IsTrue(HasText(panel, $"{FishContract.Reward} монет"));
            Click(action);
            Assert.IsTrue(HasText(panel, $"0/{FishContract.Target}"));
            Assert.IsFalse(action.interactable);
            Click(ButtonIn(panel, "OpenTrade"));
            yield return null;
            Assert.IsFalse(action.gameObject.activeInHierarchy);
            Click(ButtonIn(panel, "CloseTrade"));
            yield return null;
            Assert.IsTrue(action.gameObject.activeInHierarchy);

            Click(ButtonIn(panel, "Depart"));
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var departure = boat.transform.position;
            yield return LandSchoolAndStoreFish(boat, Schools()[0], 10);
            yield return WaitFor(() => SceneComponent<PortZone>().ReadyToDock, "Boat must clear the port");
            PlaceBoat(boat, departure);
            yield return WaitForScene(GameScenes.PORT);

            panel = SceneComponent<PortTradePanel>();
            Assert.IsTrue(HasText(panel, $"{FishContract.Target}/{FishContract.Target}"));
            action = ButtonIn(panel, "ContractAction");
            Click(action);
            Assert.IsTrue(HasText(panel, "Контракт закрыт"));
            Assert.IsFalse(action.interactable);
            Assert.AreEqual(10 - FishContract.Target, session.Cargo.Count);
            Assert.AreEqual(FishContract.Reward, session.Coins);
        }
    }
}
