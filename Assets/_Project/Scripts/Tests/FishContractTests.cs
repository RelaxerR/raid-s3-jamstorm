using NUnit.Framework;
using SecondCatch.Solo;

namespace SecondCatch.Tests
{
    public sealed class FishContractTests
    {
        [Test]
        public void DeliveryUsesOnlyTheTargetSpeciesAndPaysOnce()
        {
            var session = new SoloGameSession();
            Assert.IsFalse(session.DeliverContract());
            Assert.IsTrue(session.AcceptContract());
            Assert.IsFalse(session.AcceptContract());
            session.Depart();
            session.CatchFish(FishCargo.AmberFish);
            for (int i = 0; i < FishContract.Target + 1; i++)
                session.CatchFish(FishContract.FishId);
            Assert.AreEqual(FishContract.Target, session.ContractProgress);
            Assert.IsFalse(session.DeliverContract());
            session.Dock();

            long revision = session.Revision;
            Assert.IsTrue(session.DeliverContract());
            Assert.IsFalse(session.AcceptContract());
            Assert.IsFalse(session.DeliverContract());
            Assert.AreEqual(revision + 1, session.Revision);
            Assert.AreEqual(1, session.Cargo.FindBarrel(FishContract.FishId).Count);
            Assert.AreEqual(1, session.Cargo.FindBarrel(FishCargo.AmberFish).Count);
            Assert.AreEqual(FishContract.Reward, session.Coins);
            Assert.AreEqual(FishContractState.Closed, session.Contract.State);
            Assert.AreEqual(FishContractState.Available, new SoloGameSession().Contract.State);
        }

        [TestCase(0)]
        [TestCase(FishContract.Target - 1)]
        public void WrongSpeciesAndInsufficientCatchLeaveEverythingUntouched(int targetFish)
        {
            var session = new SoloGameSession();
            session.Depart();
            Assert.IsFalse(session.AcceptContract());
            for (int i = 0; i < FishContract.Target; i++)
                session.CatchFish(FishCargo.AmberFish);
            session.TryStowUniqueFish();
            for (int i = 0; i < targetFish; i++)
                session.CatchFish(FishContract.FishId);
            session.Dock();
            Assert.IsTrue(session.AcceptContract());
            long revision = session.Revision;
            Assert.AreEqual(targetFish, session.ContractProgress);
            Assert.IsFalse(session.DeliverContract());
            Assert.AreEqual(revision, session.Revision);
            Assert.AreEqual(targetFish + FishContract.Target, session.Cargo.Count);
            Assert.AreEqual(1, session.UniqueFishCount);
            Assert.AreEqual(0, session.Coins);
            Assert.AreEqual(FishContractState.Active, session.Contract.State);
        }

        [Test]
        public void SellingRequiredFishRemovesProgress()
        {
            var session = new SoloGameSession();
            session.AcceptContract();
            session.Depart();
            for (int i = 0; i < FishContract.Target; i++)
                session.CatchFish(FishContract.FishId);
            session.Dock();
            Assert.IsTrue(session.CanDeliverContract);
            Assert.IsTrue(session.SellFish(FishContract.FishId));
            Assert.AreEqual(0, session.ContractProgress);
            Assert.IsFalse(session.DeliverContract());
        }
    }
}
