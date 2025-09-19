using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviourTreeKit;
public class CarryMinesNode : ActionNode
{
    public override Dictionary<(string, BlackboardKey.ValueType), BlackboardKey> BlackboardKeys { get; set; }
    = new()
    {
        { (nameof(targetMine), BlackboardKey.ValueType.Object), null }
    };

    GameObject targetMine;

    public override BTState Tick(BTContext ctx)
    {
        Object _targetMine;
        if (!TryGetValue<Object>(ctx, nameof(targetMine), out _targetMine))
            return BTState.Failure;

        targetMine = (_targetMine as GameObject);

        // 애니매이션 또는 데이터 변경 실행
        return BTState.Success;
    }
}
