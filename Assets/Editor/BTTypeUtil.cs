#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{ 

    static class BTTypeUtil
    {
        public static List<Type> GetConcreteActionNodeTypes()
        {
            var list = new List<Type>();
            foreach (var t in TypeCache.GetTypesDerivedFrom<ActionNode>())
            {
                if (t.IsAbstract || t.IsGenericType) continue;
                // Unity가 인스턴스화 가능한 타입만
                if (typeof(ScriptableObject).IsAssignableFrom(t))
                    list.Add(t);
            }
            // 보기 좋게 이름순 정렬
            list.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            return list;
        }

        public static string GetMenuPathForAction(Type t)
        {
            var attr = (BTNodeMenuAttribute)Attribute.GetCustomAttribute(t, typeof(BTNodeMenuAttribute));
            return attr != null ? attr.path : $"Action/{t.Name}";
        }
    }
}
#endif
