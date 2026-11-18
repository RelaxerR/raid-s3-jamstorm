using NUnit.Framework;
using SecondCatch.Solo;
namespace SecondCatch.Tests
{
    public sealed class StationAssignmentTests
    {
        [Test]
        public void InvalidAssignmentsAreRejectedAndRepeatedAssignmentIsIdempotent()
        {
            var stations = new StationAssignment();
            Assert.IsFalse(stations.Assign(null, "helm.1"));
            Assert.IsFalse(stations.Assign("player.local", " "));
            Assert.IsTrue(stations.Assign("player.local", "helm.1"));
            Assert.IsTrue(stations.Assign("player.local", "helm.1"));
            stations.Release("unknown");
            Assert.AreEqual("player.local", stations.OperatorAt("helm.1"));
            stations.Release("player.local");
            stations.Release("player.local");
            Assert.IsNull(stations.OperatorAt("helm.1"));
        }

        [Test]
        public void FailedMovePreservesOldAssignmentAndSuccessfulMoveReleasesIt()
        {
            var stations = new StationAssignment();
            Assert.IsTrue(stations.Assign("player.local", "helm.1"));
            Assert.IsTrue(stations.Assign("crew.ivan", "nets.1"));
            Assert.IsFalse(stations.Assign("player.local", "nets.1"));
            Assert.AreEqual("player.local", stations.OperatorAt("helm.1"));
            Assert.IsTrue(stations.Assign("player.local", "harpoon.1"));
            Assert.IsNull(stations.OperatorAt("helm.1"));
            Assert.AreEqual("crew.ivan", stations.OperatorAt("nets.1"));
            stations.Release("player.local");
            Assert.IsNull(stations.OperatorAt("harpoon.1"));
        }
    }
}
