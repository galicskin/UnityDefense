using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;
public class DetechedObject : ActionNode
{
    public override Dictionary<(string, BlackboardKey.ValueType), BlackboardKey> BlackboardKeys { get; set; }
    = new();

    public override BTState Tick(BTContext ctx)
    {
        return BTState.Failure;
    }
}
