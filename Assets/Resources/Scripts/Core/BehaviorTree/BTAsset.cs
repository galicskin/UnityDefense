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
        [HideInInspector] public List<BTNode> nodes = new();

        public BTNode[] GetAllNodes() => nodes.ToArray();

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
