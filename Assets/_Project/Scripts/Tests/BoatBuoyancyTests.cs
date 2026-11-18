using NUnit.Framework;
using SecondCatch.Boat;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class BoatBuoyancyTests : BoatTestFixture
    {
        [Test]
        public void FourHullPointsGenerateSupportAndLimitPitchAndRoll()
        {
            var ocean = Create("Ocean").AddComponent<OceanSystem>();
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(waves, "_ocean", ocean);
            var buoyancy = Boat.gameObject.AddComponent<BoatBuoyancy>();
            Set(buoyancy, "_waves", waves);
            string[] names = { "_frontLeft", "_frontRight", "_rearLeft", "_rearRight" };
            Vector3[] positions = { new Vector3(-1f, 0f, 2f), new Vector3(1f, 0f, 2f),
                new Vector3(-1f, 0f, -2f), new Vector3(1f, 0f, -2f) };
            for (int i = 0; i < names.Length; i++)
            {
                var point = Create(names[i], Boat.transform).transform;
                point.localPosition = positions[i];
                Set(buoyancy, names[i], point);
                Assert.IsTrue(point.IsChildOf(Boat.transform));
            }
            Call(buoyancy, "Awake");
            Boat.transform.position = Vector3.down * 5f;
            Set(buoyancy, "_maximumTilt", 7f);
            Call(buoyancy, "Advance", 0.02f);
            Assert.Greater(buoyancy.SupportAcceleration, 0f);
            Assert.Greater(buoyancy.VerticalSpeed, 0f);
            Assert.Greater(Boat.transform.position.y, -5f);
            for (int i = 0; i < 200; i++) Call(buoyancy, "Advance", 0.02f);
            Assert.LessOrEqual(Vector3.Angle(Vector3.up, Boat.transform.up), 7.01f);
            Assert.LessOrEqual(Mathf.Abs(buoyancy.VerticalSpeed), 2f);
        }
    }
}
