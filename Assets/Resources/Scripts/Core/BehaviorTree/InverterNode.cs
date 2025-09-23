using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{ 
    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Inverter", fileName = "Inverter")]
    public class InverterNode : BTNode
    {
        public override BTState Tick(BTContext ctx)
        {
            if (children.Count == 0) return BTState.Failure;
            var st = children[0].Tick(ctx);
            return st switch
            {
                BTState.Success => BTState.Failure,
                BTState.Failure => BTState.Success,
                _ => BTState.Running
            };
        }
    }

}