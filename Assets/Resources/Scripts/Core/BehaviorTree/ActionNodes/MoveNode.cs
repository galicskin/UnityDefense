using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{ 
    public class MoveNode : ActionNode
    {
        public override Dictionary<(string, BlackboardKey.ValueType), BlackboardKey> BlackboardKeys { get; set; }
            = new()
            {
                { (nameof(Destination), BlackboardKey.ValueType.Vector3), null },
                { (nameof(Speed), BlackboardKey.ValueType.Float), null },
                { (nameof(StopWhenArrived), BlackboardKey.ValueType.Bool), null },
            };

        private Vector3 Destination;
        private float Speed;
        private bool StopWhenArrived;
        public override BTState Tick(BTContext ctx)
        {
            if (ctx?.owner == null || ctx.blackboard == null)
                return BTState.Failure;

            if(TryGetValue(ctx, nameof(Destination),out Destination))
                return BTState.Failure;
            if (TryGetValue(ctx, nameof(Speed), out Speed))
                return BTState.Failure;
            if (TryGetValue(ctx, nameof(StopWhenArrived), out StopWhenArrived))
                return BTState.Failure;

            var t = ctx.owner.transform;
            t.position = Vector3.MoveTowards(t.position, Destination, Speed * ctx.deltaTime);

            if (StopWhenArrived && t.position == Destination)
                return BTState.Success;

            return BTState.Running;
        }
    }

}