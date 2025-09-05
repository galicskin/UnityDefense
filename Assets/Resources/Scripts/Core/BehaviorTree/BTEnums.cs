using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    public enum BTState { Success, Failure, Running }

    [Serializable]
    public class BlackboardKey
    {
        public string key;
        public enum ValueType { Bool, Int, Float, Vector3, Object }
        public ValueType type = ValueType.Bool;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public Vector3 vector3Value;
        public UnityEngine.Object objectValue;
    }
}
