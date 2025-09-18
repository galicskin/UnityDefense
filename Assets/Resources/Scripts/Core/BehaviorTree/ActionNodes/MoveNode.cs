using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;

public class MoveNode : ActionNode
{
    public override BTState Tick(BTContext ctx)
    {
        
        return BTState.Running;
    }
}
