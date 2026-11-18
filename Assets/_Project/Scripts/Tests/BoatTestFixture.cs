using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SecondCatch.Boat;
using SecondCatch.Player;
using SecondCatch.Stations;
using UnityEngine;

namespace SecondCatch.Tests
{
    /// <summary>Создаёт независимую лодку и выполняет те же шаги расчёта с фиксированным временем.</summary>
    public abstract class BoatTestFixture
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        protected BoatController Boat;
        protected HelmStation Helm;
        protected SailStation Sail;
        protected WindController Wind;
        protected PlayerController Player;

        protected GameObject Create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            _objects.Add(go);
            return go;
        }

        protected static FieldInfo Field(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
        protected static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
        protected static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
        protected static object Call(object target, string name, params object[] args)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        }
        protected static void VectorEqual(Vector3 expected, Vector3 actual, float tolerance = 0.0001f)
            => Assert.LessOrEqual(Vector3.Distance(expected, actual), tolerance);

        [SetUp]
        public void CreateBoat()
        {
            Boat = Create("Boat").AddComponent<BoatController>();
            Helm = Create("Helm", Boat.transform).AddComponent<HelmStation>();
            Sail = Create("Sail", Boat.transform).AddComponent<SailStation>();
            Wind = Create("Wind").AddComponent<WindController>();
            Set(Wind, "_randomChanges", false);
            Set(Boat, "_helm", Helm);
            Set(Boat, "_sail", Sail);
            Set(Boat, "_wind", Wind);
            Set(Helm, "_boat", Boat);
            Set(Sail, "_boat", Boat);
            Call(Boat, "Awake");
            Call(Sail, "Awake");
            Player = Create("Player", Boat.transform).AddComponent<PlayerController>();
            Call(Player, "Awake");
            Set(Boat.GetComponent<BoatRespawnController>(), "_player", Player);
        }

        protected void Deploy(SailPosition position)
        {
            Sail.SetPosition(position);
            Call(Sail, "Advance", 1f);
        }
        protected void Row(float input, float dt = 1.2f)
        {
            Helm.SetActive(true);
            Helm.SetRowing(input);
            Call(Helm, "Advance", dt);
        }

        [TearDown]
        public void DestroyObjects()
        {
            foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _objects.Clear();
        }
    }
}
