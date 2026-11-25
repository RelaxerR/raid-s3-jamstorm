using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SecondCatch.Application;
using SecondCatch.Boat;
using SecondCatch.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondCatch.Tests
{
    public sealed class OuterCalmVoyageTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator SailingBoatCrossesBoundaryAndDriftsBackBeforeWeatherChange()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var waves = SceneComponent<WaveController>();
            var wind = boat.Wind;
            var status = SceneComponent<BoatStatusPanel>().GetComponentInChildren<Text>();
            var flag = boat.transform.Find("WindFlag");
            Assert.That(wind.TimeUntilChange, Is.InRange(29f, 30f));
            Assert.AreEqual(Vector3.forward, wind.Direction);

            // Start just inside the northern edge. Let gameplay movement cross both ways.
            PlaceBoat(boat, waves.Center + Vector3.forward * 198f + Vector3.up * boat.transform.position.y);
            boat.SetHelmActive(true);
            boat.SetRowing(true);
            yield return WaitFor(() => boat.RowingInput == 1f, "Rowing did not reach full input");
            boat.SetSailRaised(true);
            yield return WaitFor(() => boat.SailForwardThrust > 1f, "The sail did not pull before the boundary");
            yield return WaitFor(() => wind.IsInOuterCalm, "The boat did not sail across the boundary");
            Assert.That(wind.TimeUntilChange, Is.GreaterThan(0f));
            Assert.That(boat.SailThrust, Is.EqualTo(Vector3.zero));
            Assert.That(boat.SailForwardThrust, Is.Zero);
            Assert.That(boat.RowingInput, Is.EqualTo(1f));
            Assert.That(boat.RowingThrust, Is.GreaterThan(0f));
            Assert.That(boat.WaveDrift.z, Is.LessThan(0f));
            yield return WaitFor(() => status.text.Contains("Внешний штиль · тяга 0"), "HUD did not show the outer calm");
            StringAssert.Contains("→ 000°", status.text);
            Assert.That(Vector3.Angle(flag.forward, wind.Direction), Is.LessThan(1f));

            yield return WaitFor(() => boat.Velocity.z < 0f, "Full outward rowing overcame wave drift");
            Assert.That(wind.IsInOuterCalm, Is.True);
            Assert.That(boat.RowingInput, Is.EqualTo(1f));
            yield return WaitFor(() => !wind.IsInOuterCalm, "Wave drift did not bring the boat inside");
            Assert.That(boat.RowingInput, Is.EqualTo(1f));
            boat.SetRowing(false);
            Assert.That(wind.SailExposure, Is.LessThan(0.1f));
            yield return WaitFor(() => wind.SailExposure > 0.25f, "Sail exposure did not begin to recover");
            Assert.That(wind.SailExposure, Is.LessThan(1f));
            Assert.That(boat.SailForwardThrust, Is.GreaterThan(0f));
            StringAssert.DoesNotContain("Внешний штиль", status.text);
            // Fold the sail to keep the outward wind from taking the boat over the edge again.
            boat.SetSailRaised(false);
            yield return WaitFor(() => wind.SailExposure == 1f, "Sail exposure did not fully recover");
            boat.SetSailRaised(true);
            yield return WaitFor(() => boat.SailForwardThrust > 1f, "Sail thrust did not recover after raising the sail again");
            Assert.That(wind.TimeUntilChange, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator TimerNotifiesDirectionSubscribersWhileKeepingTheNormalProfile()
        {
            yield return StartNewGame();
            Assert.IsTrue(App.Depart());
            yield return WaitForScene(GameScenes.SEA);
            var boat = SceneComponent<BoatController>();
            var wind = boat.Wind;
            var status = SceneComponent<BoatStatusPanel>().GetComponentInChildren<Text>();
            var flag = boat.transform.Find("WindFlag");
            var waves = SceneComponent<WaveController>();
            Assert.That(wind.Type, Is.EqualTo(WindType.Normal));
            Assert.That(wind.Direction, Is.EqualTo(Vector3.forward));
            Assert.That(wind.TimeUntilChange, Is.InRange(29f, 30f));

            // Zero weights keep the current profile. Beyond the western boundary the timer picks +X.
            foreach (string field in new[] { "_calm", "_normal", "_storm" })
                ((WindProfile)typeof(WindController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(wind)).SelectionWeight = 0f;
            waves.enabled = false;
            PlaceBoat(boat, waves.Center + Vector3.left * 201f + Vector3.up * boat.transform.position.y);
            Assert.That(wind.IslandReturnProbability, Is.EqualTo(1f));

            int typeChanges = 0;
            int directionChanges = 0;
            Vector3 reportedDirection = Vector3.forward;
            wind.WindTypeChanged += _ => typeChanges++;
            wind.WindDirectionChanged += direction =>
            {
                Assert.That(direction, Is.EqualTo(wind.Direction));
                directionChanges++;
                reportedDirection = direction;
            };

            float originalTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 4f;
                yield return WaitFor(() => wind.TimeUntilChange < 0.5f, "The wind timer did not count down");
                Assert.That(directionChanges, Is.Zero);
                yield return WaitFor(() => reportedDirection.x > 0.2f, "No direction event during the timed transition");
                Assert.That(wind.TimeUntilChange, Is.GreaterThan(28f));
                Assert.That(Vector3.Angle(Vector3.right, reportedDirection), Is.GreaterThan(10f));
                yield return WaitFor(() => status.text.Contains("→ 090°") && Vector3.Angle(flag.forward, wind.Direction) < 1f,
                    "HUD and flag did not converge on the automatic direction");
                yield return WaitFor(() => wind.TimeUntilChange < 27.8f, "The timed transition did not finish");
                Assert.That(reportedDirection, Is.EqualTo(wind.Direction));
                Assert.That(Vector3.Distance(Vector3.right, reportedDirection), Is.LessThan(0.0001f));
                Assert.That(directionChanges, Is.GreaterThan(1));
                Assert.That(typeChanges, Is.Zero);
                Assert.That(wind.Type, Is.EqualTo(WindType.Normal));
            }
            finally { Time.timeScale = originalTimeScale; }
        }
    }
}
