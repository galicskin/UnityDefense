using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;
public class CarryMinesNode : ActionNode
{

    GameObject targetMine;

    public override void BuildBlackboardKeys()
    {
        AddKeyIfNotExists(nameof(targetMine), BlackboardKey.ValueType.Object);
    }

    public override BTState Tick(BTContext ctx)
    {
        Object _targetMine;

        // 애니매이션 또는 데이터 변경 실행
        return BTState.Success;
    }
}
