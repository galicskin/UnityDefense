using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    public class BehaviourTreeRunner : MonoBehaviour
    {
        public BTAsset treeAsset;
        public bool autoRun = true;

        private BTNode _root;
        private Blackboard _bb;
        private BTContext _ctx;

        public void Build()
        {
            if (treeAsset == null) { Debug.LogError("[BT] No tree asset."); return; }
            (_root, _bb) = treeAsset.BuildRuntime();
            _ctx = new BTContext { owner = gameObject, blackboard = _bb };
        }

        public void Tick(float deltaTime)
        {
            if (_root == null) Build();
            _ctx.deltaTime = deltaTime;
            if (!_root) return;
            var state = _root.Tick(_ctx);
            // Optional: handle state transitions, etc.
        }

        private void Awake()
        {
            if (autoRun) Build();
        }

        private void Update()
        {
            if (autoRun && _root != null)
                Tick(Time.deltaTime);
        }
    }
}
