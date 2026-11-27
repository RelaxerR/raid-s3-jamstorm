using NUnit.Framework;
using SecondCatch.Solo;
namespace SecondCatch.Tests
{
    public sealed class StationAssignmentTests
    {
        [Test]
        public void HiringAndAssigningAreSeparatePortCommandsAndSurviveVoyages()
        {
            var session = new SoloGameSession();
            Assert.IsFalse(session.AssignHelmsman());
            Assert.IsFalse(session.HireHelmsman());
            session.Depart();
            session.CatchSchool(1, 15);
            session.Dock();
            Assert.IsTrue(session.Sell());
            session.Depart();
            Assert.IsFalse(session.HireHelmsman());
            session.Dock();
            Assert.IsTrue(session.HireHelmsman());
            Assert.AreEqual(50, session.Coins);
            Assert.IsFalse(session.HelmsmanAssigned);
            Assert.IsFalse(session.HireHelmsman());
            session.Depart();
            Assert.IsFalse(session.AssignHelmsman());
            session.Dock();

            int assigned = 0;
            session.OnStationAssigned += (member, station) =>
            {
                Assert.AreEqual(SoloGameSession.HELMSMAN_ID, member);
                Assert.AreEqual(SoloGameSession.HELM_ID, station);
                assigned++;
            };
            Assert.IsTrue(session.AssignHelmsman());
            Assert.IsFalse(session.AssignHelmsman());
            Assert.AreEqual(1, assigned);
            Assert.AreEqual(50, session.Coins);
            session.Depart();
            Assert.IsTrue(session.HelmsmanAssigned);
            Assert.IsTrue(session.Damage(session.Boat.Hp));
            Assert.IsTrue(session.HelmsmanAssigned);
            Assert.IsTrue(session.HasHelmsman);
        }

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
