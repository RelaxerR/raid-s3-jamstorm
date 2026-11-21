using NUnit.Framework;
using SecondCatch.Solo;

namespace SecondCatch.Tests
{
    public sealed class SoloGameSessionTests
    {
        [Test]
        public void NewSessionStartsInPortAndRejectedCommandsPreserveRevision()
        {
            var session = new SoloGameSession();
            Assert.IsFalse(session.AtSea);
            Assert.AreEqual(60, session.Boat.Hp);
            Assert.AreEqual(60, session.Boat.MaxHp);
            Assert.AreEqual(1f, session.Boat.SpeedMultiplier);
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.AreEqual(0, session.Coins);
            Assert.AreEqual(0, session.Revision);
            Assert.IsFalse(session.Dock());
            Assert.IsFalse(session.Sell());
            Assert.IsFalse(session.BuySpeedUpgrade());
            Assert.AreEqual(0, session.Revision);
        }

        [Test]
        public void DockAndDepartPreserveCargoHealthAndModelIdentity()
        {
            var session = new SoloGameSession();
            var boat = session.Boat;
            var cargo = session.Cargo;
            session.Depart();
            session.CatchFish(cargo.FishId);
            Assert.IsFalse(session.Damage(7));
            Assert.AreEqual(53, boat.Hp);
            long revision = session.Revision;
            Assert.IsTrue(session.Dock());
            Assert.IsTrue(session.Depart());
            Assert.AreEqual(revision + 2, session.Revision);
            Assert.AreSame(boat, session.Boat);
            Assert.AreSame(cargo, session.Cargo);
            Assert.AreEqual(53, session.Boat.Hp);
            Assert.AreEqual(1, session.Cargo.Count);
        }

        [Test]
        public void TwoVoyagesPreserveUpgradeAndPreventDuplicatePurchases()
        {
            var session = new SoloGameSession();
            Assert.IsFalse(session.CatchFish(session.Cargo.FishId));
            Assert.IsTrue(session.Depart());
            Assert.IsFalse(session.Depart());
            for (int i = 0; i < 12; i++) Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
            Assert.IsFalse(session.Sell());
            Assert.IsTrue(session.Dock());
            Assert.IsTrue(session.Sell());
            Assert.IsFalse(session.Sell());
            Assert.AreEqual(120, session.Coins);
            Assert.IsTrue(session.BuySpeedUpgrade());
            Assert.IsFalse(session.BuySpeedUpgrade());
            Assert.AreEqual(20, session.Coins);
            Assert.IsTrue(session.Depart());
            Assert.AreEqual(1.4f, session.Boat.SpeedMultiplier);
            Assert.AreEqual(0, session.Cargo.Count);
        }
        [Test]
        public void BarrelRejectsOtherSpeciesAndOverflow()
        {
            var session = new SoloGameSession(); session.Depart();
            Assert.IsFalse(session.CatchFish("other"));
            for (int i = 0; i < 100; i++) Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
            Assert.IsTrue(session.Cargo.IsFull);
            Assert.IsFalse(session.CatchFish(session.Cargo.FishId));
            Assert.AreEqual(100, session.Cargo.Count);
        }
        [Test]
        public void SaleByTypeRejectsWrongTypeAndUpdatesCargoCoinsAndRevisionTogether()
        {
            var session = new SoloGameSession();
            var fishId = session.Cargo.FishId;
            Assert.IsTrue(session.Depart());
            Assert.AreEqual(10, session.CatchSchool(1, 10));
            Assert.IsFalse(session.SellFish(fishId));
            Assert.IsTrue(session.Dock());
            long revision = session.Revision;

            Assert.IsFalse(session.SellFish("fish.unknown"));
            Assert.AreEqual(revision, session.Revision);
            Assert.AreEqual(10, session.Cargo.Count);
            Assert.AreEqual(0, session.Coins);
            Assert.IsTrue(session.SellFish(fishId));
            Assert.AreEqual(revision + 1, session.Revision);
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.AreEqual(100, session.Coins);
            Assert.IsFalse(session.SellFish(fishId));
            Assert.IsFalse(session.Sell());
            Assert.AreEqual(revision + 1, session.Revision);
        }

        [Test]
        public void SinkingLosesHalfOnceAndReturnsAtThirtyPercentHp()
        {
            var session = new SoloGameSession(); session.Depart();
            for (int i = 0; i < 5; i++) session.CatchFish(session.Cargo.FishId);
            Assert.IsFalse(session.Damage(-1));
            Assert.IsTrue(session.Damage(100));
            Assert.IsFalse(session.AtSea);
            Assert.AreEqual(18, session.Boat.Hp);
            Assert.AreEqual(2, session.Cargo.Count);
            Assert.IsFalse(session.Damage(100));
            Assert.AreEqual(2, session.Cargo.Count);
            session.Depart(); session.Dock();
            Assert.AreEqual(18, session.Boat.Hp);
        }
    }
}
