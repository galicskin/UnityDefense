using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    [CreateAssetMenu(menuName = "BehaviourTree/Blackboard", fileName = "Blackboard")]
    public class Blackboard : ScriptableObject
    {
        [SerializeField] private List<BlackboardKey> entries = new();

        public T Get<T>(string key)
        {
            var e = entries.Find(x => x.key == key);
            if (e == null) throw new Exception($"Blackboard key not found: {key}");
            object val = null;
            switch (e.type)
            {
                case BlackboardKey.ValueType.Bool: val = e.boolValue; break;
                case BlackboardKey.ValueType.Int: val = e.intValue; break;
                case BlackboardKey.ValueType.Float: val = e.floatValue; break;
                case BlackboardKey.ValueType.Vector3: val = e.vector3Value; break;
                case BlackboardKey.ValueType.Object: val = e.objectValue; break;
            }
            return (T)val;
        }

        public void Set<T>(string key, T value)
        {
            var e = entries.Find(x => x.key == key);
            if (e == null)
            {
                e = new BlackboardKey { key = key };
                entries.Add(e);
            }
            switch (value)
            {
                case bool b: e.type = BlackboardKey.ValueType.Bool; e.boolValue = b; break;
                case int i: e.type = BlackboardKey.ValueType.Int; e.intValue = i; break;
                case float f: e.type = BlackboardKey.ValueType.Float; e.floatValue = f; break;
                case Vector3 v: e.type = BlackboardKey.ValueType.Vector3; e.vector3Value = v; break;
                case UnityEngine.Object o: e.type = BlackboardKey.ValueType.Object; e.objectValue = o; break;
                default: throw new Exception($"Unsupported type {typeof(T)} for blackboard set");
            }
        }

        public Blackboard CloneInstance()
        {
            var inst = Instantiate(this);
            // List<T> is value-copied because it's serialized; Instantiate copies serialized fields.
            return inst;
        }
    }
}
