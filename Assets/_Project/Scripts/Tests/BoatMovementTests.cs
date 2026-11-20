using NUnit.Framework;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class BoatMovementTests : BoatTestFixture
    {
        [TestCase(1f, 0.6f)]
        [TestCase(-1f, -0.2f)]
        public void RowingWorksWithFoldedSail(float input, float expected)
        {
            Row(input);
            Assert.IsTrue(Boat.RowingAvailable);
            Assert.AreEqual(expected, Boat.RowingThrust, 0.0001f);
        }

        [TestCase(SailPosition.Partial)]
        [TestCase(SailPosition.Full)]
        public void SailThrustBlocksBothRowingDirectionsAndPreservesInertia(SailPosition position)
        {
            Deploy(position);
            Set(Boat, "_propulsionVelocity", Vector3.forward * 2f);
            foreach (float input in new[] { 1f, -1f })
            {
                Row(input, 3f);
                Assert.Greater(Boat.SailForwardThrust, Boat.SailThrustEpsilon);
                Assert.IsFalse(Boat.RowingAvailable);
                Assert.AreEqual(0f, Boat.RowingThrust);
                VectorEqual(Vector3.forward * 2f, Boat.Velocity);
            }
            Deploy(SailPosition.Folded);
            Assert.IsTrue(Boat.RowingAvailable);
        }

        [Test]
        public void BlockedRowingDoesNotAddAccelerationToSail()
        {
            Deploy(SailPosition.Full);
            Call(Boat, "AdvancePropulsion", 0.1f);
            Vector3 sailOnly = Boat.Velocity;
            Boat.ResetMotion();
            Row(1f);
            Call(Boat, "AdvancePropulsion", 0.1f);
            VectorEqual(sailOnly, Boat.Velocity);
            Boat.ResetMotion();
            Row(-1f, 3f);
            Call(Boat, "AdvancePropulsion", 0.1f);
            VectorEqual(sailOnly, Boat.Velocity);
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, 180f)]
        public void CalmAndZeroEfficiencyAllowRowing(float strength, float angle)
        {
            Deploy(SailPosition.Full);
            Set(Wind, "_strength", strength);
            Sail.SetAngle(angle);
            Row(1f);
            Assert.LessOrEqual(Boat.SailForwardThrust, Boat.SailThrustEpsilon);
            Assert.IsTrue(Boat.RowingAvailable);
            Assert.Greater(Boat.RowingThrust, 0f);
        }

        [Test]
        public void EpsilonUsesActualForwardThrust()
        {
            Deploy(SailPosition.Partial);
            Set(Wind, "_strength", 0.0001f);
            Assert.IsTrue(Boat.RowingAvailable);
            Set(Wind, "_strength", 0.01f);
            Assert.IsFalse(Boat.RowingAvailable);
        }

        [Test]
        public void AccelerationUsesConfiguredTimeAndMass()
        {
            Set(Boat, "_movementResistance", 0f);
            Set(Boat, "_maxSpeed", 0.6f);
            Row(1f);
            Call(Boat, "AdvancePropulsion", 1f);
            Assert.AreEqual(0.3f, Boat.LongitudinalSpeed, 0.0001f);
            Call(Boat, "AdvancePropulsion", 1f);
            Assert.AreEqual(0.6f, Boat.LongitudinalSpeed, 0.0001f);
            Boat.ResetMotion();
            Set(Boat, "_mass", 2000f);
            Row(1f);
            Call(Boat, "AdvancePropulsion", 1f);
            Assert.AreEqual(0.15f, Boat.LongitudinalSpeed, 0.0001f);
        }

        [Test]
        public void BrakingIsGradualAndResistanceIncreasesDecay()
        {
            Set(Boat, "_propulsionVelocity", Vector3.forward);
            Set(Boat, "_movementResistance", 0f);
            Call(Boat, "AdvancePropulsion", 0.1f);
            float withoutResistance = Boat.CurrentSpeed;
            Assert.That(withoutResistance, Is.InRange(0.9f, 0.9999f));
            Set(Boat, "_propulsionVelocity", Vector3.forward);
            Set(Boat, "_movementResistance", 2f);
            Call(Boat, "AdvancePropulsion", 0.1f);
            Assert.Less(Boat.CurrentSpeed, withoutResistance);
        }

        [Test]
        public void YawBuildsAndDecaysWithoutCourseJump()
        {
            Set(Boat, "_propulsionVelocity", Vector3.forward);
            Helm.SetActive(true);
            Helm.SetSteering(1f);
            Call(Helm, "Advance", 0.2f);
            Call(Boat, "AdvanceYaw", 0.1f);
            float yaw = Boat.YawSpeed;
            Assert.Greater(yaw, 0f);
            Helm.SetSteering(0f);
            Call(Helm, "Advance", 0.02f);
            float heading = Boat.transform.eulerAngles.y;
            Call(Boat, "AdvanceYaw", 0.02f);
            Assert.Greater(Boat.YawSpeed, 0f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(heading, Boat.transform.eulerAngles.y)), 1f);
            for (int i = 0; i < 500; i++)
            {
                Call(Helm, "Advance", 0.02f);
                Call(Boat, "AdvanceYaw", 0.02f);
            }
            Assert.AreEqual(0f, Boat.YawSpeed, 0.001f);
        }
    }
}
