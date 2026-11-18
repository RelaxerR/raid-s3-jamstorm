using NUnit.Framework;
using SecondCatch.Stations;
using SecondCatch.UI;
using UnityEngine.UI;

namespace SecondCatch.Tests
{
    public sealed class HelmStationTests : BoatTestFixture
    {
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
