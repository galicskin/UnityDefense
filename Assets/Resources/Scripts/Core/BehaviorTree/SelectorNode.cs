using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{ 
    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Selector", fileName = "Selector")]
    public class SelectorNode : CompositeNode
    {
        public override BTState Tick(BTContext ctx)
        {
            while (_currentIndex < children.Count)
            {
                var child = children[_currentIndex];
                var state = child.Tick(ctx);
                if (state == BTState.Running) return BTState.Running;
                if (state == BTState.Success)
                {
                    _currentIndex = 0;
                    return BTState.Success;
                }
                _currentIndex++;
            }
            _currentIndex = 0;
            return BTState.Failure;
        }
    }
}
