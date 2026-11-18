using NUnit.Framework;
using SecondCatch.Solo;

namespace SecondCatch.Tests
{
    public sealed class SoloGameSessionTests
    {
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
