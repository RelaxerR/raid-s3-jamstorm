using NUnit.Framework;
using SecondCatch.Boat;
using SecondCatch.Camera;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class BoatCameraControllerTests : BoatTestFixture
    {
        private BoatCameraController Camera()
        {
            var camera = Create("Camera").AddComponent<BoatCameraController>();
            Set(camera, "_boat", Boat.transform);
            Call(camera, "OnEnable");
            return camera;
        }

        [Test]
        public void ManualOrbitAndWheelChangeAzimuthAndClampElevationAndDistance()
        {
            var camera = Camera();
            Call(camera, "Advance", new Vector2(100f, -10000f), 100f, false, 0.02f);
            Assert.AreEqual(20f, Get<float>(camera, "_azimuth"), 0.001f);
            Assert.AreEqual(80f, Get<float>(camera, "_elevation"));
            Assert.AreEqual(3f, Get<float>(camera, "_targetDistance"));
            Call(camera, "Advance", new Vector2(0f, 10000f), -100f, false, 0.02f);
            Assert.AreEqual(12f, Get<float>(camera, "_elevation"));
            Assert.AreEqual(15f, Get<float>(camera, "_targetDistance"));
        }

        [Test]
        public void ResetCommandReturnsSmoothlyToBoatAndDefaultDistance()
        {
            var camera = Camera();
            Call(camera, "Advance", new Vector2(500f, 100f), -2f, false, 0.02f);
            float azimuth = Get<float>(camera, "_azimuth");
            Vector3 position = camera.transform.position;
            Call(camera, "Advance", Vector2.zero, 0f, true, 0.02f);
            Assert.That(Get<float>(camera, "_azimuth"), Is.InRange(1f, azimuth - 1f));
            Vector3 destination = (Vector3)Call(camera, "OrbitPosition");
            VectorEqual(Vector3.Lerp(position, destination, 1f - Mathf.Exp(-8f * 0.02f)), camera.transform.position);
            Assert.Greater(Vector3.Distance(destination, camera.transform.position), 0.01f);
            Assert.AreEqual(7.4f, Get<float>(camera, "_targetDistance"));
            for (int i = 0; i < 300; i++) Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, Get<float>(camera, "_azimuth"))), 0.01f);
            Assert.AreEqual(42f, Get<float>(camera, "_elevation"), 0.01f);
            Assert.AreEqual(7.4f, Get<float>(camera, "_distance"), 0.01f);
        }

        [Test]
        public void StationOrbitBlendsAndResetsToDeckOnExitOrRespawn()
        {
            var camera = Camera();
            Set(camera, "_player", Player);
            Set(camera, "_defaultAzimuth", 34f);
            Set(camera, "_defaultElevation", 53f);
            Call(camera, "OnEnable");

            var net = Create("Net", Boat.transform).AddComponent<NetStation>();
            Set(net, "_boat", Boat);
            Set(net, "_cameraProfile", new StationCameraProfile { Azimuth = 48f, Elevation = 37f, Distance = 7f });
            Assert.IsTrue(net.TryEnter(Player));
            Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.That(Get<float>(camera, "_elevation"), Is.InRange(37.1f, 52.9f));
            for (int i = 0; i < 20; i++) Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.AreEqual(37f, Get<float>(camera, "_elevation"), 0.25f);
            Assert.AreEqual(48f, Get<float>(camera, "_azimuth"), 0.25f);

            Player.ExitStation();
            for (int i = 0; i < 21; i++) Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.AreEqual(53f, Get<float>(camera, "_elevation"), 0.25f);
            Assert.AreEqual(34f, Get<float>(camera, "_azimuth"), 0.25f);

            Set(Helm, "_cameraProfile", new StationCameraProfile { Elevation = 24f, Distance = 7f });
            Assert.IsTrue(Helm.TryEnter(Player));
            for (int i = 0; i < 21; i++) Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.AreEqual(24f, Get<float>(camera, "_elevation"), 0.25f);
            var respawn = Boat.GetComponent<BoatRespawnController>();
            Call(respawn, "Awake");
            Call(respawn, "BeginRespawn");
            Assert.IsTrue(Boat.IsRespawning);
            Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            Assert.IsNull(Get<StationInteraction>(camera, "_station"));
            Assert.AreEqual(7.4f, Get<float>(camera, "_targetDistance"), 0.001f);
        }

        [Test]
        public void BoatRollDoesNotChangeCameraFocusOrHorizon()
        {
            var camera = Camera();
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Boat.transform.rotation = Quaternion.Euler(0f, 0f, 20f);
            Call(camera, "Advance", Vector2.zero, 0f, false, 0.02f);
            VectorEqual(position, camera.transform.position);
            Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), 0.01f);
        }
    }
}
