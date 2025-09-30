using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;
public class DetechedObject : ActionNode
{

    public override void BuildBlackboardKeys()
    {
        
    }

    public override BTState Tick(BTContext ctx)
    {
        // test
        Worker worker = ctx.GetService<Worker>();
        Debug.Log(worker.TestBool);
        if (worker.TestBool)
        {
            Debug.Log("test ¼º°ø");
            return BTState.Success;
        }

        return BTState.Failure;
    }
}
