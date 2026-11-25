using System.Collections;
using NUnit.Framework;
using SecondCatch.Fishing;
using SecondCatch.Solo;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondCatch.Tests
{
    public sealed class FishOnDeckTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Fish/FishOnDeck.prefab";

        [UnityTest]
        public IEnumerator StoreTransfersOneFishAndRejectsASecondAttemptOnTheSameObject()
        {
            yield return new EnterPlayMode();
            var session = SessionWithDeckFish();
            var fish = CreateFish();
            long revision = session.Revision;

            Assert.AreEqual(DeckFishStoreResult.Stored, fish.TryStore(session));
            Assert.IsFalse(fish.gameObject.activeSelf);
            Assert.AreEqual(DeckFishStoreResult.Unavailable, fish.TryStore(session));
            Assert.IsFalse(fish.TryLose(session));
            Assert.AreEqual(1, session.Cargo.Count);
            Assert.AreEqual(1, session.DeckFishCount);
            Assert.AreEqual(revision + 1, session.Revision);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator LossRemovesOneLooseFishAndRejectsLatePickupWhileOtherFishRemain()
        {
            yield return new EnterPlayMode();
            var session = SessionWithDeckFish();
            var fish = CreateFish();
            long revision = session.Revision;

            Assert.IsTrue(fish.TryLose(session));
            Assert.IsFalse(fish.GetComponent<BoxCollider>().enabled);
            Assert.IsFalse(fish.gameObject.activeSelf);
            Assert.IsFalse(fish.TryLose(session));
            Assert.AreEqual(DeckFishStoreResult.Unavailable, fish.TryStore(session));
            Assert.AreEqual(0, session.Cargo.Count);
            Assert.AreEqual(1, session.DeckFishCount);
            Assert.AreEqual(revision + 1, session.Revision);
            yield return new ExitPlayMode();
        }

        [TestCase(true, DeckFishStoreResult.Full)]
        [TestCase(false, DeckFishStoreResult.Incompatible)]
        public void RejectedFishRemainsOnDeck(bool fillBarrel, DeckFishStoreResult expected)
        {
            var session = SessionWithDeckFish();
            var fish = CreateFish();
            try
            {
                if (fillBarrel)
                    for (int i = 0; i < session.Cargo.Capacity; i++)
                        Assert.IsTrue(session.CatchFish(session.Cargo.FishId));
                else
                {
                    using var serializedFish = new SerializedObject(fish);
                    serializedFish.FindProperty("_fishId").stringValue = "fish.other";
                    serializedFish.ApplyModifiedPropertiesWithoutUndo();
                }
                long revision = session.Revision;
                Assert.AreEqual(expected, fish.TryStore(session));
                Assert.IsTrue(fish.GetComponent<BoxCollider>().enabled);
                Assert.IsTrue(fish.gameObject.activeSelf);
                Assert.AreEqual(2, session.DeckFishCount);
                Assert.AreEqual(fillBarrel ? session.Cargo.Capacity : 0, session.Cargo.Count);
                Assert.AreEqual(revision, session.Revision);
            }
            finally { Object.DestroyImmediate(fish.gameObject); }
        }

        private static FishOnDeck CreateFish()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.NotNull(prefab);
            FishOnDeck source = prefab.GetComponent<FishOnDeck>();
            Assert.NotNull(source);
            return Object.Instantiate(source);
        }

        private static SoloGameSession SessionWithDeckFish()
        {
            var session = new SoloGameSession();
            session.Depart();
            Assert.AreEqual(2, session.TakeSchoolIntoNet(1, 2, 100));
            session.LandNetCatch();
            return session;
        }
    }
}
