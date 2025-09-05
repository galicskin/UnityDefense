using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    /// <summary>
    /// Example action provider. Attach this to your agent GameObject.
    /// ActionNode will call ExecuteAction with the configured actionName.
    /// </summary>
    public class ExampleBehaviourAction : MonoBehaviour, IBehaviourAction
    {
        private float _timer;
        public BTState ExecuteAction(string actionName, BTContext ctx)
        {
            switch (actionName)
            {
                case "Log":
                    Debug.Log($"[BT] Log action from {name}");
                    return BTState.Success;
                case "Idle2s":
                    _timer += ctx.deltaTime;
                    if (_timer >= 2f) { _timer = 0f; return BTState.Success; }
                    return BTState.Running;
                default:
                    return BTState.Failure;
            }
        }
    }
}
