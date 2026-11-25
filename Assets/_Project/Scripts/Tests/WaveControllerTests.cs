using NUnit.Framework;
using SecondCatch.Boat;
using SecondCatch.Stations;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using SecondCatch.UI;

namespace SecondCatch.Tests
{
    public sealed class WaveControllerTests : BoatTestFixture
    {
        [Test]
        public void GrayboxWiresSharedCenterBoundaryFlagAndFourBuoyancyPoints()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox/SecondCatchGraybox.unity");
                var boat = Object.FindFirstObjectByType<BoatController>();
                var waves = Object.FindFirstObjectByType<WaveController>();
                var wind = Object.FindFirstObjectByType<WindController>();
                var buoyancy = boat.GetComponent<BoatBuoyancy>();
                foreach (string name in new[] { "_frontLeft", "_frontRight", "_rearLeft", "_rearRight" })
                    Assert.IsTrue(Get<Transform>(buoyancy, name).IsChildOf(boat.transform));
                var center = Get<Transform>(waves, "_islandCenter");
                Assert.AreEqual("IslandCenter", center.name);
                Assert.AreSame(center, Get<Transform>(wind, "_islandCenter"));
                VectorEqual(Vector3.zero, center.position);
                Assert.AreEqual("HomeIsland", center.parent.name);
                Assert.NotNull(GameObject.Find("Environment/HomePort"));
                var boundary = Get<BoxCollider>(wind, "_seaSurface");
                Assert.AreEqual("MapBoundary", boundary.name);
                VectorEqual(new Vector3(400f, 2f, 400f), boundary.size);
                Assert.AreEqual(400f, Get<float>(waves.Ocean, "oceanSize"));
                Assert.AreEqual(200f, Get<float>(wind, "_maxZoneRadius"));
                Assert.AreSame(center, Get<Transform>(Object.FindFirstObjectByType<GameplayHud>(), "_mapCenter"));
                var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/GameplayHud.prefab");
                Assert.AreEqual(new Vector2(400f, 400f), Get<Vector2>(hudPrefab.GetComponentInChildren<MinimapView>(), "_worldSize"));
                var flag = boat.transform.Find("WindFlag");
                VectorEqual(new Vector3(0f, 2.025f, 0f), flag.localPosition);
                VectorEqual(new Vector3(0f, -0.15f, 0.4f), flag.Find("FlagCloth").localPosition);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        [Test]
        public void ForceAndOutwardResistanceIncreaseWithDistanceFromIsland()
        {
            var waves = Create("Waves").AddComponent<WaveController>();
            var center = Create("IslandCenter").transform;
            center.position = new Vector3(17f, 0f, -21f);
            Set(waves, "_islandCenter", center);
            Vector3 near = center.position + Vector3.right * 20f;
            Vector3 far = center.position + Vector3.right * 180f;
            VectorEqual(Vector3.left, waves.DriftVelocityAt(far).normalized);
            VectorEqual(Vector3.zero, waves.DriftVelocityAt(center.position));
            Assert.Greater(waves.DriftVelocityAt(far).magnitude, waves.DriftVelocityAt(near).magnitude);
            Assert.Greater(waves.ForceAt(far), waves.ForceAt(near));
            Assert.Greater(waves.ResistanceAt(far), waves.ResistanceAt(near));
            Assert.AreEqual(0f, waves.DistanceFactor(center.position));
            Assert.AreEqual(1f, waves.DistanceFactor(center.position + Vector3.forward * 200f));
        }

        [Test]
        public void SurfaceUsesMapCenterAndSharedParametersForAllFourWaves()
        {
            var ocean = Create("Ocean").AddComponent<OceanSystem>();
            var waves = Create("Waves").AddComponent<WaveController>();
            var center = Create("IslandCenter").transform;
            center.position = new Vector3(35f, 0f, -40f);
            Set(waves, "_ocean", ocean);
            Set(waves, "_islandCenter", center);
            Call(waves, "ConfigureSurface");
            VectorEqual(center.position, Get<Vector3>(ocean, "fieldCenter"));
            Assert.AreEqual(200f, Get<float>(ocean, "outerRadius"));
            var components = Get<System.Array>(ocean, "waves");
            Assert.AreEqual(4, components.Length);
            // Каждый компонент по отдельности использует общее поле амплитуды и радиальную фазу.
            for (int i = 0; i < components.Length; i++)
            {
                var isolated = System.Array.CreateInstance(components.GetType().GetElementType(), 4);
                isolated.SetValue(components.GetValue(i), i);
                Set(ocean, "waves", isolated);
                Vector3 sample = center.position + Vector3.right * 80f;
                float height = ocean.GetWaterHeight(sample, out Vector3 normal, out float foam);
                Assert.IsFalse(float.IsNaN(height) || float.IsInfinity(height));
                Assert.IsFalse(float.IsNaN(normal.y) || float.IsNaN(foam));
                Vector3 shift = new Vector3(72f, 0f, 15f);
                ocean.ConfigureAmplitudeField(center.position + shift, 12f, 200f, 0.08f, 0.9f);
                Assert.AreEqual(height, ocean.GetWaterHeight(sample + shift, out _, out _), 0.0001f);
                Call(waves, "ConfigureSurface");
            }
        }

        [Test]
        public void WindUsesExpandedBoundaryAndSeededSelectionIsRepeatable()
        {
            var center = Create("IslandCenter").transform;
            var boundary = Create("MapBoundary").AddComponent<BoxCollider>();
            boundary.size = new Vector3(400f, 2f, 400f);
            Set(Wind, "_islandCenter", center);
            Set(Wind, "_boat", Boat.transform);
            Set(Wind, "_seaSurface", boundary);
            Set(Wind, "_shoreRadius", 12f);
            Set(Wind, "_distanceExponent", 1f);
            Boat.transform.position = Vector3.right * 12f;
            Assert.AreEqual(0f, Wind.NormalizedDistance);
            Assert.AreEqual(0.05f, Wind.IslandReturnProbability, 0.001f);
            Boat.transform.position = Vector3.right * 106f;
            Assert.AreEqual(0.5f, Wind.NormalizedDistance);
            float middleProbability = Wind.IslandReturnProbability;
            Assert.AreEqual(0.525f, middleProbability, 0.001f);
            Boat.transform.position = Vector3.right * 180f;
            Assert.Greater(Wind.IslandReturnProbability, middleProbability);
            Assert.Less(Wind.IslandReturnProbability, 1f);
            Boat.transform.position = Vector3.right * 200f;
            Assert.AreEqual(1f, Wind.IslandReturnProbability);
            Assert.Greater(Wind.IslandReturnProbability, middleProbability);
            var randomState = Random.state;
            try
            {
                Random.InitState(18011);
                Wind.ChooseNextWind();
                Assert.AreEqual(-90f, Get<float>(Wind, "_targetHeading"), 0.001f);
                float speed = Get<float>(Wind, "_targetSpeed");
                var type = Wind.TargetType;
                Random.InitState(18011);
                Wind.ChooseNextWind();
                Assert.AreEqual(speed, Get<float>(Wind, "_targetSpeed"));
                Assert.AreEqual(type, Wind.TargetType);
            }
            finally { Random.state = randomState; }
        }

        [Test]
        public void WindChangesSpeedBeforeItChangesCourseOrType()
        {
            Set(Wind, "_randomChanges", true);
            Set(Wind, "_changeInterval", new Vector2(20f, 20f));
            Set(Wind, "_speedChangeInterval", new Vector2(4f, 4f));
            Set(Wind, "_speedTransitionDuration", 2f);
            Set(Wind, "_normal", new WindProfile { MinSpeed = 1f, MaxSpeed = 2f });
            Set(Wind, "_speed", 2f);
            Call(Wind, "Awake");
            // Фиксируем следующий выбор скорости, оставляя направление и тип прежними.
            Get<WindProfile>(Wind, "_normal").MaxSpeed = 1f;

            Call(Wind, "Advance", 3.9f);
            Assert.AreEqual(2f, Wind.Speed);
            Call(Wind, "Advance", 0.2f);
            Call(Wind, "Advance", 1f);
            Assert.AreEqual(1.5f, Wind.Speed, 0.0001f);
            Call(Wind, "Advance", 1f);
            Assert.AreEqual(1f, Wind.Speed);
            VectorEqual(Vector3.forward, Wind.Direction);
            Assert.AreEqual(WindType.Normal, Wind.Type);
            Assert.AreEqual(13.9f, Wind.TimeUntilChange, 0.001f);
        }

        [TestCase(WindType.Normal)]
        [TestCase(WindType.Storm)]
        [TestCase(WindType.Calm)]
        public void OuterCalmCutsSailForceWithoutChangingWeatherOrWaveDrift(WindType type)
        {
            var center = Create("IslandCenter").transform;
            center.position = new Vector3(25f, 0f, -50f);
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(waves, "_islandCenter", center);
            Set(Boat, "_waves", waves);
            Set(Wind, "_islandCenter", center);
            Set(Wind, "_boat", Boat.transform);
            Set(Wind, "_outerCalmRecoveryDuration", 2f);
            Set(Wind, "_type", type);
            Call(Wind, "Awake");
            Deploy(SailPosition.Full);

            Vector3 radial = new Vector3(0.6f, 0f, 0.8f);
            Boat.transform.position = center.position + radial * 199f + Vector3.up * 20f;
            Vector3 insideForce = Boat.SailForce;
            if (type == WindType.Calm) VectorEqual(Vector3.zero, insideForce);
            else Assert.Greater(insideForce.magnitude, 0f);
            float timeUntilChange = Wind.TimeUntilChange;
            float driftSpeed = waves.DriftVelocityAt(Boat.transform.position).magnitude;
            Boat.transform.position += radial;
            Assert.IsTrue(Wind.IsInOuterCalm, "The horizontal radius applies before the weather ticks");
            VectorEqual(Vector3.zero, Boat.SailForce);
            Assert.IsTrue(Boat.RowingAvailable);
            Call(Wind, "Advance", 0.5f);
            Call(Boat, "ApplyWaveField", 0.5f);
            Assert.Greater(Vector3.Dot(Boat.WaveDrift, -radial), 0f);
            Assert.That(waves.DriftVelocityAt(Boat.transform.position).magnitude, Is.EqualTo(driftSpeed).Within(0.01f));
            Assert.AreEqual(type, Wind.Type);
            Assert.AreEqual(timeUntilChange, Wind.TimeUntilChange);
            VectorEqual(Vector3.forward, Wind.Direction);

            Boat.transform.position -= radial * 2f;
            Assert.IsFalse(Wind.IsInOuterCalm);
            Assert.AreEqual(0f, Wind.SailExposure);
            Call(Wind, "Advance", 1f);
            VectorEqual(insideForce * 0.5f, Boat.SailForce);
            Call(Wind, "Advance", 1f);
            VectorEqual(insideForce, Boat.SailForce);
        }

        [Test]
        public void WavesMoveTheBoatTowardsIslandWithoutInput()
        {
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(Boat, "_waves", waves);
            Vector3 start = Vector3.back * 100f;
            Boat.transform.position = start;

            for (int i = 0; i < 150; i++) Call(Boat, "AdvanceMotion", 0.02f);

            Assert.Greater(Boat.transform.position.z - start.z, 0.5f);
            Assert.AreEqual(0f, Boat.LongitudinalSpeed, 0.0001f);
            VectorEqual(Boat.WaveDrift, Boat.Velocity);
            Assert.Greater(Boat.WaveDrift.z, 0f);
            Assert.Less(Boat.WaveDrift.magnitude, 1.2f);
        }

        [TestCase(SailPosition.Folded)]
        [TestCase(SailPosition.Full)]
        public void WaveSpeedAddsToRowingAndSailingTowardsIsland(SailPosition position)
        {
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(waves, "_minimumDriftSpeed", 0.4f);
            Set(waves, "_maximumDriftSpeed", 0.4f);
            Boat.transform.position = Vector3.back * 100f;
            Deploy(position);
            if (position == SailPosition.Folded) Row(1f);
            for (int i = 0; i < 300; i++) Call(Boat, "AdvanceMotion", 0.02f);
            float poweredSpeed = Boat.CurrentSpeed;
            Assert.Greater(poweredSpeed, 0.1f);

            Set(Boat, "_waves", waves);
            for (int i = 0; i < 300; i++) Call(Boat, "AdvanceMotion", 0.02f);

            Assert.AreEqual(poweredSpeed, Boat.LongitudinalSpeed, 0.001f);
            Assert.AreEqual(poweredSpeed + 0.4f, Boat.CurrentSpeed, 0.003f);
        }

        [TestCase(SailPosition.Folded)]
        [TestCase(SailPosition.Full)]
        public void WaveDriftWorksRegardlessOfRowingAvailability(SailPosition position)
        {
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(Boat, "_waves", waves);
            Boat.transform.position = Vector3.right * 180f;
            Deploy(position);
            Call(Boat, "ApplyWaveField", 0.1f);
            Assert.Less(Boat.WaveDrift.x, 0f);
            VectorEqual(Boat.WaveDrift, Boat.TotalBoatVelocity);
            Assert.AreEqual(position == SailPosition.Folded, Boat.RowingAvailable);
        }
    }
}
