using NUnit.Framework;
using SecondCatch.Boat;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class BoatHealthTests : BoatTestFixture
    {
        [TestCase(BoatObstacleType.Reef, 15f)]
        [TestCase(BoatObstacleType.Rock, 25f)]
        [TestCase(BoatObstacleType.Debris, 5f)]
        public void ContactAppliesTypedDamageWithCooldownPerBoat(BoatObstacleType type, float damage)
        {
            var obstacle = Create("Obstacle").AddComponent<BoatDamageSource>();
            Set(obstacle, "_type", type);
            var collider = Boat.gameObject.AddComponent<BoxCollider>();
            Call(obstacle, "TryDamage", collider);
            Assert.AreEqual(100f - damage, Boat.HitPoints);
            for (int i = 0; i < 10; i++) Call(obstacle, "TryDamage", collider);
            Assert.AreEqual(100f - damage, Boat.HitPoints);
            var deadlines = Get<System.Collections.Generic.Dictionary<BoatHealth, float>>(obstacle, "_nextDamageTimes");
            deadlines[Boat.Health] = Time.time - 1f;
            Call(obstacle, "TryDamage", collider);
            Assert.AreEqual(100f - damage * 2f, Boat.HitPoints);
        }

        [Test]
        public void HealthClampsAtZeroAndDestructionFiresOnce()
        {
            int destroyed = 0;
            Boat.Health.Destroyed += () => destroyed++;
            Boat.Health.TakeDamage(1000f);
            Boat.Health.TakeDamage(1000f);
            Assert.AreEqual(0f, Boat.HitPoints);
            Assert.AreEqual(1, destroyed);
        }
    }
}
