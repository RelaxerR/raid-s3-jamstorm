using NUnit.Framework;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class SailStationTests : BoatTestFixture
    {
        [TestCase(SailPosition.Folded, 0f)]
        [TestCase(SailPosition.Partial, 0.5f)]
        [TestCase(SailPosition.Full, 1f)]
        public void ThreePositionsControlDeployment(SailPosition position, float deployment)
        {
            Deploy(position);
            Assert.AreEqual(position, Sail.Position);
            Assert.AreEqual(deployment, Sail.Deployment, 0.001f);
        }

        [Test]
        public void RotationAcceleratesThenDecaysAndAnglePersistsAfterExit()
        {
            Assert.IsTrue(Sail.TryEnter(Player));
            Sail.HandleInput(Vector2.right);
            Call(Sail, "Advance", 0.1f);
            float early = Sail.AngularVelocity;
            Call(Sail, "Advance", 0.1f);
            Assert.Greater(Sail.AngularVelocity, early);
            Player.ExitStation();
            float angle = Sail.Angle;
            float speed = Sail.AngularVelocity;
            Call(Sail, "Advance", 0.02f);
            Assert.That(Sail.AngularVelocity, Is.InRange(0.001f, speed - 0.001f));
            Assert.Greater(Sail.Angle, angle);
            for (int i = 0; i < 100; i++) Call(Sail, "Advance", 0.02f);
            angle = Sail.Angle;
            Call(Sail, "Advance", 1f);
            Assert.Greater(angle, 0f);
            Assert.AreEqual(angle, Sail.Angle);
        }
    }
}
