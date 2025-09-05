using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    /// <summary>
    /// Execution context passed to nodes.
    /// </summary>
    public class BTContext
    {
        public GameObject owner;
        public Blackboard blackboard;
        public float deltaTime;

        public T GetService<T>() where T : class => owner != null ? owner.GetComponent<T>() : null;
    }

    public abstract class BTNode : ScriptableObject
    {
        [HideInInspector] public Rect editorPosition = new Rect(50, 50, 220, 70);
        [HideInInspector] public List<BTNode> children = new();
        [HideInInspector] public string comment;

        // RUNTIME: since we instantiate nodes at runtime from the asset,
        // it's safe to keep transient fields directly on the instance.
        [NonSerialized] protected bool _entered;

        public virtual void OnEnter(BTContext ctx) { _entered = true; }
        public abstract BTState Tick(BTContext ctx);
        public virtual void OnExit(BTContext ctx) { _entered = false; }

        public virtual IEnumerable<BTNode> GetChildren() => children;
        public virtual void AddChild(BTNode node)
        {
            if (!children.Contains(node)) children.Add(node);
        }
        public virtual void RemoveChild(BTNode node)
        {
            children.Remove(node);
        }
    }

    public abstract class CompositeNode : BTNode
    {
        [NonSerialized] protected int _currentIndex;
        public override void OnEnter(BTContext ctx)
        {
            base.OnEnter(ctx);
            _currentIndex = 0;
        }
    }

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

    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Inverter", fileName = "Inverter")]
    public class InverterNode : BTNode
    {
        public override BTState Tick(BTContext ctx)
        {
            if (children.Count == 0) return BTState.Failure;
            var st = children[0].Tick(ctx);
            return st switch
            {
                BTState.Success => BTState.Failure,
                BTState.Failure => BTState.Success,
                _ => BTState.Running
            };
        }
    }

    /// <summary>
    /// Implement this on a component to expose an action callable by ActionNode.
    /// </summary>
    public interface IBehaviourAction
    {
        BTState ExecuteAction(string actionName, BTContext ctx);
    }

    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Action", fileName = "Action")]
    public class ActionNode : BTNode
    {
        public string actionName = ""; // Logical name looked up on IBehaviourAction
        public bool requireComponent = true;

        public override BTState Tick(BTContext ctx)
        {
            if (ctx.owner == null)
                return BTState.Failure;
            var comp = ctx.owner.GetComponent<IBehaviourAction>();
            if (comp == null)
                return requireComponent ? BTState.Failure : BTState.Success;
            return comp.ExecuteAction(actionName, ctx);
        }
    }

    [CreateAssetMenu(menuName = "BehaviourTree/Nodes/Wait", fileName = "Wait")]
    public class WaitNode : BTNode
    {
        public float seconds = 1f;
        [NonSerialized] private float _t;

        public override void OnEnter(BTContext ctx)
        {
            base.OnEnter(ctx);
            _t = 0f;
        }

        public override BTState Tick(BTContext ctx)
        {
            _t += ctx.deltaTime;
            if (_t >= seconds) return BTState.Success;
            return BTState.Running;
        }

        public override void OnExit(BTContext ctx)
        {
            base.OnExit(ctx);
            _t = 0f;
        }
    }
}