using NUnit.Framework;
using SecondCatch.Boat;
using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class BoatRespawnControllerTests : BoatTestFixture
    {
        [Test]
        public void ZeroHpRespawnsRestoresHealthReleasesStationsAndResetsAllMotion()
        {
            var body = Boat.gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            var respawn = Boat.GetComponent<BoatRespawnController>();
            Call(respawn, "OnDisable");
            Call(respawn, "Awake");
            Set(respawn, "_player", Player);
            Call(respawn, "OnEnable");
            Deploy(SailPosition.Partial);
            Sail.SetAngle(37f);
            Assert.IsTrue(Helm.TryEnter(Player));
            Vector3 deckStart = Player.transform.localPosition;
            Player.transform.localPosition = Vector3.right * 0.5f;
            Boat.transform.position = Vector3.forward * 100f;
            Set(Boat, "_propulsionVelocity", Vector3.forward * 3f);
            Set(Boat, "_waveVelocity", Vector3.left);
            Set(Boat, "_yawSpeed", 5f);
            body.linearVelocity = Vector3.one;
            body.angularVelocity = Vector3.one;
            Boat.TakeDamage(100f);
            Assert.IsTrue(Boat.IsRespawning);
            Assert.IsNull(Helm.Operator);
            Assert.IsNull(Player.ActiveStation);
            Call(respawn, "Advance", 2f);
            Assert.IsFalse(Boat.IsRespawning);
            Assert.AreEqual(100f, Boat.HitPoints);
            VectorEqual(Vector3.zero, Boat.Velocity);
            Assert.AreEqual(0f, Boat.YawSpeed);
            VectorEqual(Vector3.zero, body.linearVelocity);
            VectorEqual(Vector3.zero, body.angularVelocity);
            VectorEqual(Vector3.zero, Boat.transform.position);
            VectorEqual(deckStart, Player.transform.localPosition);
            Assert.AreEqual(PlayerControlMode.Walking, Player.ControlMode);
            Assert.AreEqual(SailPosition.Partial, Sail.Position);
            Assert.AreEqual(37f, Sail.Angle);
        }
    }
}
