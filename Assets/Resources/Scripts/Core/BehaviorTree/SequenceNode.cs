using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit

{ 
    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Sequence", fileName = "Sequence")]
    public class SequenceNode : CompositeNode
    {
        public override BTState Tick(BTContext ctx)
        {
            while (_currentIndex < children.Count)
            {
                var child = children[_currentIndex];
                var state = child.Tick(ctx);
                if (state == BTState.Running) return BTState.Running;
                if (state == BTState.Failure)
                {
                    // reset when leaving
                    _currentIndex = 0;
                    return BTState.Failure;
                }
                _currentIndex++;
            }
            _currentIndex = 0;
            return BTState.Success;
        }
    }
}
