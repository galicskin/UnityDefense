using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    [CreateAssetMenu(menuName = "BehaviourTree/Tree", fileName = "BehaviourTreeAsset")]
    public class BTAsset : ScriptableObject
    {
        public Blackboard blackboardTemplate;
        public BTNode root;
#if UNITY_EDITOR
        [HideInInspector] public List<BTNode> nodes = new();
#endif


        /// <summary>
        /// 런타임에서도 모든 노드를 가져오고 싶을 때는 root부터 재귀 탐색
        /// </summary>
        public BTNode[] GetAllNodes()
        {
            var list = new List<BTNode>();
            CollectRecursive(root, list);
            return list.ToArray();
        }

        private void CollectRecursive(BTNode n, List<BTNode> list)
        {
            if (n == null || list.Contains(n)) return;
            list.Add(n);
            if (n.children == null) return;
            foreach (var c in n.children)
                CollectRecursive(c, list);
        }

        /// <summary>
        /// Build a runtime instance of the tree and blackboard.
        /// Nodes are deep-instantiated so they carry their own transient runtime state.
        /// </summary>
        public (BTNode runtimeRoot, Blackboard runtimeBB) BuildRuntime()
        {
            if (root == null) throw new Exception("BTAsset has no root node.");
            var map = new Dictionary<BTNode, BTNode>();

            BTNode CloneRecursive(BTNode n)
            {
                if (n == null) return null;
                if (map.TryGetValue(n, out var cached)) return cached;
                var clone = Instantiate(n);
                clone.hideFlags = HideFlags.DontSave;
                clone.children = new List<BTNode>();
                map[n] = clone;
                foreach (var c in n.children)
                {
                    var c2 = CloneRecursive(c);
                    if (c2 != null) clone.children.Add(c2);
                }
                return clone;
            }

            var runtimeRoot = CloneRecursive(root);
            var runtimeBB = blackboardTemplate != null ? blackboardTemplate.CloneInstance() : ScriptableObject.CreateInstance<Blackboard>();
            return (runtimeRoot, runtimeBB);
        }
    }
}
