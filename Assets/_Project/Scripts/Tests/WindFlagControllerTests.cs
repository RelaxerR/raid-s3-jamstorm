using NUnit.Framework;
using SecondCatch.Boat;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class WindFlagControllerTests : BoatTestFixture
    {
        private WindFlagController Flag(Vector3 axis)
        {
            var go = Create("WindFlag", Boat.transform);
            go.transform.localPosition = new Vector3(0.2f, 2.025f, -0.3f);
            go.transform.localRotation = Quaternion.Euler(12f, 35f, 7f);
            var flag = go.AddComponent<WindFlagController>();
            Set(flag, "_wind", Wind);
            Set(flag, "_localDirectionAxis", axis);
            Call(flag, "OnEnable");
            return flag;
        }

        [Test]
        public void UpdatesPreserveLocalPositionHeightAndParent()
        {
            var flag = Flag(Vector3.forward);
            Vector3 local = flag.transform.localPosition;
            float height = flag.transform.position.y;
            Set(Wind, "_direction", Vector3.right);
            for (int i = 0; i < 150; i++) Call(flag, "Advance", 0.02f);
            VectorEqual(local, flag.transform.localPosition);
            Assert.AreEqual(height, flag.transform.position.y, 0.0001f);
            Assert.AreSame(Boat.transform, flag.transform.parent);
            Assert.Less(Vector3.Angle(flag.transform.forward, Vector3.right), 0.1f);
        }

        [TestCase(0f, 0f, 1f)]
        [TestCase(1f, 0f, 0f)]
        [TestCase(0f, 1f, 0f)]
        public void ModelAxisFollowsGlobalWindDespiteBoatYawAndTilt(float x, float y, float z)
        {
            Vector3 axis = new Vector3(x, y, z);
            Set(Wind, "_direction", new Vector3(-1f, 8f, 1f));
            var flag = Flag(axis);
            Boat.transform.rotation = Quaternion.Euler(15f, 130f, -20f);
            Call(flag, "Advance", 0.02f);
            Assert.Less(Vector3.Angle(flag.transform.rotation * axis, Wind.Direction), 0.01f);
        }

        [Test]
        public void ChildVisualAndYawCorrectionAreRespected()
        {
            var flag = Flag(Vector3.right);
            var visual = Create("Visual", flag.transform).transform;
            visual.localPosition = new Vector3(0f, -0.15f, 0.4f);
            visual.localRotation = Quaternion.Euler(0f, 25f, 0f);
            Set(flag, "_flagVisual", visual);
            Set(flag, "_angleOffset", 30f);
            Call(flag, "OnEnable");
            VectorEqual(new Vector3(0f, -0.15f, 0.4f), visual.localPosition);
            Assert.Less(Vector3.Angle(visual.right, Quaternion.Euler(0f, 30f, 0f) * Wind.Direction), 0.01f);
        }

        [Test]
        public void InvalidWindKeepsLastHeadingAndContinuesParentCompensation()
        {
            Set(Wind, "_direction", Vector3.left);
            var flag = Flag(Vector3.forward);
            Set(Wind, "_direction", new Vector3(float.NaN, 0f, 0f));
            Boat.transform.rotation = Quaternion.Euler(0f, 80f, 0f);
            Call(flag, "Advance", 0.1f);
            Assert.Less(Vector3.Angle(flag.transform.forward, Vector3.left), 0.01f);
            Wind.enabled = false;
            Boat.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Call(flag, "Advance", 0.1f);
            Assert.Less(Vector3.Angle(flag.transform.forward, Vector3.left), 0.01f);
        }
    }
}
