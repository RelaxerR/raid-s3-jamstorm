using System;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Solo;

namespace SecondCatch.Tests
{
    public sealed class BoatZoneRepairTests
    {
        [TestCase(BoatZone.FrontLeft)]
        [TestCase(BoatZone.FrontRight)]
        [TestCase(BoatZone.RearLeft)]
        [TestCase(BoatZone.RearRight)]
        [TestCase(BoatZone.Mast)]
        public void HitBelongsToOneZoneAndRepairRestoresExactlyItsDamage(BoatZone zone)
        {
            var session = VoyageWithWood();
            var otherZone = zone == BoatZone.Mast ? BoatZone.FrontLeft : BoatZone.Mast;

            var boat = session.Boat;
            Assert.That(boat.GetZoneHp(zone), Is.EqualTo(25));
            Assert.IsFalse(session.Damage(1, zone));
            Assert.That(boat.GetZoneHp(zone), Is.EqualTo(24));
            Assert.That(boat.Hp, Is.EqualTo(59));
            Assert.IsFalse(boat.IsZoneDamaged(zone));
            Assert.IsFalse(session.TryRepairZone(zone));
            Assert.That(session.Wood, Is.EqualTo(1));

            Assert.IsFalse(session.Damage(4, zone));
            Assert.That(boat.GetZoneHp(zone), Is.EqualTo(20));
            Assert.IsTrue(boat.IsZoneDamaged(zone));
            Assert.IsFalse(session.Damage(25, zone));
            Assert.That(boat.GetZoneHp(zone), Is.Zero);
            Assert.That(boat.Hp, Is.EqualTo(35));
            foreach (BoatZone entry in Enum.GetValues(typeof(BoatZone)))
                Assert.That(boat.GetZoneDamage(entry), Is.EqualTo(entry == zone ? 25 : 0));

            long revision = session.Revision;
            Assert.IsFalse(session.Damage(25, zone));
            Assert.That(session.Revision, Is.EqualTo(revision));
            Assert.That(boat.Hp, Is.EqualTo(35));
            Assert.IsFalse(session.Damage(5, otherZone));
            revision = session.Revision;
            Assert.IsTrue(session.TryRepairZone(zone));
            Assert.That(session.Wood, Is.Zero);
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.That(boat.GetZoneHp(zone), Is.EqualTo(boat.GetZoneMaxHp(zone)));
            Assert.IsFalse(boat.IsZoneDamaged(zone));
            Assert.That(boat.GetZoneDamage(otherZone), Is.EqualTo(5));
            Assert.That(boat.Hp, Is.EqualTo(55));
            Assert.IsFalse(session.Damage(5, zone));
            Assert.IsTrue(boat.IsZoneDamaged(zone), "The same area can be damaged again after repair");
            Assert.That(boat.Hp, Is.EqualTo(50));
        }

        [Test]
        public void InvalidOrUndamagedZoneAndPortRepairLeaveStateUnchanged()
        {
            var session = VoyageWithWood();
            long revision = session.Revision;
            Assert.IsFalse(session.Damage(20, (BoatZone)99));
            Assert.IsFalse(session.TryRepairZone((BoatZone)99));
            Assert.IsFalse(session.TryRepairZone(BoatZone.RearRight));
            Assert.That(session.Revision, Is.EqualTo(revision));

            Assert.IsFalse(session.Damage(5, BoatZone.RearRight));
            Assert.IsTrue(session.Dock());
            revision = session.Revision;
            Assert.IsFalse(session.TryRepairZone(BoatZone.RearRight));
            Assert.That(session.Wood, Is.EqualTo(1));
            Assert.That(session.Boat.Hp, Is.EqualTo(55));
            Assert.That(session.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void CannotRepairWithEmptyStock()
        {
            var session = new SoloGameSession();
            Assert.IsTrue(session.Depart());
            Assert.IsFalse(session.Damage(15, BoatZone.FrontLeft));

            long revision = session.Revision;
            Assert.IsFalse(session.TryRepairZone(BoatZone.FrontLeft));
            Assert.That(session.Revision, Is.EqualTo(revision));
            Assert.That(session.Wood, Is.Zero);
            Assert.That(session.Boat.Hp, Is.EqualTo(45));
            Assert.That(session.Boat.GetZoneDamage(BoatZone.FrontLeft), Is.EqualTo(15));
        }

        [Test]
        public void RespawnRetainsThirtyPercentAndTheMissingHpCanBeRepaired()
        {
            var session = VoyageWithWood();
            Assert.IsFalse(session.Damage(25, BoatZone.FrontLeft));
            Assert.IsFalse(session.Damage(25, BoatZone.FrontRight));
            Assert.IsTrue(session.Damage(25, BoatZone.RearRight));

            Assert.That(session.Boat.Hp, Is.EqualTo(18));
            var zones = (BoatZone[])Enum.GetValues(typeof(BoatZone));
            Assert.That(zones.Sum(session.Boat.GetZoneDamage), Is.EqualTo(42));
            Assert.That(session.Wood, Is.EqualTo(1));
            Assert.IsTrue(session.Depart());
            int damage = session.Boat.GetZoneDamage(BoatZone.RearRight);
            Assert.IsTrue(session.TryRepairZone(BoatZone.RearRight));
            Assert.That(session.Boat.Hp, Is.EqualTo(18 + damage));
            Assert.That(session.Boat.GetZoneDamage(BoatZone.RearRight), Is.Zero);
        }

        private static SoloGameSession VoyageWithWood()
        {
            var session = new SoloGameSession();
            Assert.IsTrue(session.Depart());
            Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
            Assert.IsTrue(session.Dock());
            Assert.IsTrue(session.Sell());
            Assert.IsTrue(session.BuyWood());
            Assert.IsTrue(session.Depart());
            return session;
        }
    }
}
