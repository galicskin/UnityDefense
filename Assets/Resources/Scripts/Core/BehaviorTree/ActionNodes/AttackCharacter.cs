using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;
public class AttackCharacter : ActionNode
{
    
    public override void BuildBlackboardKeys()
    {
        
    }

    public override BTState Tick(BTContext ctx)
    {
        return BTState.Failure;
    }
}
