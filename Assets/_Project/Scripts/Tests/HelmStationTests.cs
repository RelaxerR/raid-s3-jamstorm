using NUnit.Framework;
using SecondCatch.Stations;
using SecondCatch.Boat;
using SecondCatch.Solo;
using UnityEngine;
using SecondCatch.UI;
using UnityEngine.UI;

namespace SecondCatch.Tests
{
    public sealed class HelmStationTests : BoatTestFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LeavingHelmUnderSailShowsWaveTurnOrHeldCourse(bool hired)
        {
            var session = new SoloGameSession();
            if (hired)
            {
                session.Depart();
                session.CatchSchool(1, 10);
                session.Dock();
                session.Sell();
                Assert.IsTrue(session.HireHelmsman());
                Assert.IsTrue(session.AssignHelmsman());
            }
            session.Depart();
            Boat.transform.position = new Vector3(40f, 0f, 0f);
            var waves = Create("Waves").AddComponent<WaveController>();
            Set(Boat, "_waves", waves);
            Helm.BindSession(session);
            Deploy(SailPosition.Full);
            Assert.IsTrue(Helm.TryEnter(Player));
            Helm.SetSteering(1f);
            AdvanceSailing(1f);
            var heading = Boat.HullDirection;
            Player.ExitStation();
            Assert.AreEqual(hired, Helm.CrewSteering);
            AdvanceSailing(8f);
            float change = Vector3.SignedAngle(heading, Boat.HullDirection, Vector3.up);
            TestContext.WriteLine($"Heading change after 8 s: {change:F2} degrees (crew = {hired})");
            if (hired)
                Assert.That(Mathf.Abs(change), Is.LessThan(1.5f), "Ivan must correct the residual turn");
            else
                Assert.That(change, Is.LessThan(-8f), "Waves should turn the unattended helm towards port");

            Assert.IsTrue(Helm.TryEnter(Player));
            Assert.IsFalse(Helm.CrewSteering);
            Helm.SetSteering(1f);
            AdvanceSailing(3f);
            Assert.Greater(Helm.RudderAngle, 0f);
            Vector3 newHeading = Boat.HullDirection;
            Player.ExitStation();
            AdvanceSailing(12f);
            if (hired) Assert.Less(Vector3.Angle(newHeading, Boat.HullDirection), 1.5f);
        }

        private void AdvanceSailing(float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds / 0.02f); i++)
            {
                Call(Helm, "Advance", 0.02f);
                Call(Boat, "AdvanceMotion", 0.02f);
            }
        }

        [Test]
        public void GameplayHudShowsSailBlockingInStatusAndInteractionHint()
        {
            var status = Create("Status").AddComponent<BoatStatusPanel>();
            var statusText = Create("StatusText").AddComponent<Text>();
            Set(status, "_status", statusText);
            var hint = Create("Hint").AddComponent<InteractionHintView>();
            var hintText = Create("HintText").AddComponent<Text>();
            Set(hint, "_label", hintText);
            var hud = Create("Hud").AddComponent<GameplayHud>();
            Set(hud, "_boatStatus", status);
            Set(hud, "_interactionHint", hint);
            Assert.IsTrue(Helm.TryEnter(Player));
            Deploy(SailPosition.Full);
            hud.Bind(Boat, Player);
            StringAssert.Contains("Гребля недоступна: работает парус", statusText.text);
            StringAssert.Contains("Гребля недоступна: работает парус", hintText.text);
            Deploy(SailPosition.Folded);
            hud.Bind(Boat, Player);
            StringAssert.Contains("Гребля доступна", statusText.text);
            StringAssert.Contains("Гребля доступна", hintText.text);
        }

        [Test]
        public void RudderReturnsToCenterAfterInputStops()
        {
            Helm.SetActive(true);
            Helm.SetSteering(1f);
            Call(Helm, "Advance", 0.3f);
            float angle = Helm.RudderAngle;
            Assert.Greater(angle, 0f);
            Helm.SetSteering(0f);
            Call(Helm, "Advance", 0.01f);
            Assert.Less(System.Math.Abs(Helm.RudderAngle - angle), 1f);
            for (int i = 0; i < 200; i++) Call(Helm, "Advance", 0.02f);
            Assert.AreEqual(0f, Helm.RudderAngle, 0.001f);
        }

        [Test]
        public void ActiveSailBlocksRowingButAllowsSteering()
        {
            Deploy(SailPosition.Full);
            Helm.SetActive(true);
            Helm.HandleInput(new UnityEngine.Vector2(1f, 1f));
            Call(Helm, "Advance", 1.2f);
            Assert.IsFalse(Helm.RowingAvailable);
            Assert.AreEqual(0f, Boat.RowingThrust);
            Assert.Greater(Helm.RudderAngle, 0f);
            Assert.Greater(Boat.RowingInput, 0f);
        }
    }
}
