using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondCatch.Application;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class VoyageAudioTests : VoyageTestFixture
    {
        [UnityTest]
        public IEnumerator PortAndSeaStartTheirOwnAmbience()
        {
            yield return StartNewGame();
            var port = SceneComponent<HomePortController>();
            var portAmbience = FindLoop(port, "amb_port_quiet");
            Assert.AreEqual("Ambience", portAmbience.outputAudioMixerGroup.name);
            Assert.IsTrue(SceneComponent<AudioListener>().isActiveAndEnabled);
            yield return WaitFor(() => portAmbience.isPlaying, "Port ambience did not start");

            port.Depart();
            yield return WaitForScene(GameScenes.SEA);
            Assert.IsTrue(portAmbience == null, "Port ambience survived the scene transition");
            var voyage = SceneComponent<VoyageAudio>();
            var ocean = FindLoop(voyage, "amb_common_ocean");
            var shore = FindLoop(voyage, "amb_shore_waves");
            Assert.AreEqual("Ambience", ocean.outputAudioMixerGroup.name);
            Assert.AreEqual("Ambience", shore.outputAudioMixerGroup.name);
            yield return WaitFor(() => ocean.isPlaying && shore.isPlaying, "Sea ambience did not start at departure");
        }

        private static AudioSource FindLoop(Component owner, string name)
        {
            var source = owner.GetComponents<AudioSource>().SingleOrDefault(audio => audio.loop && audio.clip?.name == name);
            Assert.IsNotNull(source, name);
            return source;
        }
    }
}
