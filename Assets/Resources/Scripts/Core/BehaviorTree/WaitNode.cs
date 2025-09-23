using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{ 
    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Wait", fileName = "Wait")]
    public class WaitNode : BTNode
    {
        public float seconds = 1f;
        [NonSerialized] private float _t;

        public override void OnEnter(BTContext ctx)
        {
            base.OnEnter(ctx);
            _t = 0f;
        }

        public override BTState Tick(BTContext ctx)
        {
            _t += ctx.deltaTime;
            if (_t >= seconds) return BTState.Success;
            return BTState.Running;
        }

        public override void OnExit(BTContext ctx)
        {
            base.OnExit(ctx);
            _t = 0f;
        }
    }
}
