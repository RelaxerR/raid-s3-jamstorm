using System.IO;
using NUnit.Framework;
using SecondCatch.Fishing;
using SecondCatch.UI;
using UnityEditor;
using UnityEngine;

namespace SecondCatch.Tests
{
    public sealed class CargoIntegrationTests
    {
        private const string Project = "Assets/_Project/";

        [Test]
        public void FishAndTradePrefabsHaveRequiredTypedReferences()
        {
            var school = LoadPrefab<FishSchool>("Fish/FishSchool");
            Assert.IsTrue(school.GetComponent<BoxCollider>().isTrigger);
            Assert.IsNotEmpty(school.GetComponentsInChildren<MeshRenderer>());
            var fish = LoadPrefab<CargoFish>("Fish/CargoFish");
            Assert.NotNull(fish.GetComponent<Rigidbody>());

            var panel = LoadPrefab<PortTradePanel>("UI/PortTradePanel");
            using var panelData = new SerializedObject(panel);
            foreach (string property in new[] { "_tradeBody", "_cargo", "_coins", "_speedUpgrade",
                "_open", "_close", "_sellFish", "_sellAll", "_buyUpgrade", "_depart", "_menu" })
                Assert.NotNull(panelData.FindProperty(property).objectReferenceValue, property);

            var indicators = LoadPrefab<GameplayHud>("UI/GameplayHud").GetComponentInChildren<FutureGameplayIndicators>();
            Assert.NotNull(indicators);
            using var hudData = new SerializedObject(indicators);
            Assert.NotNull(hudData.FindProperty("_cargo").objectReferenceValue);
        }

        [Test]
        public void CargoAndTradeScriptsUseComponentReferences()
        {
            foreach (string path in new[]
            {
                "Fishing/FishSchool", "Fishing/CargoHold", "Fishing/CargoFish",
                "UI/PortTradePanel", "UI/GameplayHud", "Voyage/HomeShallowsController", "Port/HomePortController"
            })
            {
                string source = File.ReadAllText(Project + "Scripts/Runtime/" + path + ".cs");
                StringAssert.DoesNotMatch(@"\b(?:Camera\.main|CompareTag\s*\(|Find\s*\(|FindWithTag\s*\(|FindGameObjectsWithTag\s*\(|Find(?:First|Any)?ObjectOfType\s*<|Find(?:First|Any)ObjectByType\s*<|tag\s*==)", source, path);
            }
        }

        private static T LoadPrefab<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Project + "Prefabs/" + path + ".prefab");
            Assert.NotNull(prefab, path);
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject));
            var component = prefab.GetComponent<T>();
            Assert.NotNull(component, path);
            return component;
        }
    }
}
