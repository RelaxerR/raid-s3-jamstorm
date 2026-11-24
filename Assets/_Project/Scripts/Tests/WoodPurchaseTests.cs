using NUnit.Framework;
using SecondCatch.Solo;

namespace SecondCatch.Tests
{
    public sealed class WoodPurchaseTests
    {
        [Test]
        public void PurchaseChargesTenCoinsOnceAndAddsOneWood()
        {
            var session = SessionWithFishSale(1);
            long revision = session.Revision;

            Assert.That(session.WoodPrice, Is.EqualTo(10));
            Assert.IsTrue(session.CanBuyWood);
            Assert.IsTrue(session.BuyWood());
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.That(session.Coins, Is.Zero);
            Assert.That(session.Wood, Is.EqualTo(1));
            Assert.IsFalse(session.BuyWood());
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void RejectsInsufficientCoinsAndUsesConfiguredPrice()
        {
            var session = SessionWithFishSale(1, 15);
            long revision = session.Revision;
            Assert.IsFalse(session.CanBuyWood);
            Assert.IsFalse(session.BuyWood());
            Assert.That(session.Coins, Is.EqualTo(10));
            Assert.That(session.Wood, Is.Zero);
            Assert.That(session.Revision, Is.EqualTo(revision));

            session = SessionWithFishSale(2, 15);
            Assert.IsTrue(session.BuyWood());
            Assert.That(session.Coins, Is.EqualTo(5));
        }

        [Test]
        public void CannotBuyAtSeaEvenWithEnoughCoins()
        {
            var session = SessionWithFishSale(2);
            Assert.IsTrue(session.Depart());
            long revision = session.Revision;

            Assert.IsFalse(session.CanBuyWood);
            Assert.IsFalse(session.BuyWood());
            Assert.That(session.Coins, Is.EqualTo(20));
            Assert.That(session.Wood, Is.Zero);
            Assert.That(session.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void StockSurvivesVoyageAndSpendingStopsAtZero()
        {
            var session = SessionWithFishSale(1);
            Assert.IsTrue(session.BuyWood());
            Assert.IsTrue(session.Depart());
            long revision = session.Revision;

            Assert.IsTrue(session.TrySpendWood());
            Assert.IsFalse(session.TrySpendWood());
            Assert.That(session.Wood, Is.Zero);
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.IsTrue(session.Dock());
            Assert.That(session.Wood, Is.Zero);
        }

        private static SoloGameSession SessionWithFishSale(int count, int price = SoloGameSession.DEFAULT_WOOD_PRICE)
        {
            var session = new SoloGameSession(price);
            Assert.IsTrue(session.Depart());
            Assert.That(session.CatchSchool(1, count), Is.EqualTo(count));
            Assert.IsTrue(session.Dock());
            Assert.IsTrue(session.Sell());
            return session;
        }
    }
}
