using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
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

    
    public abstract class ActionNode : BTNode
    {
        public string actionName = ""; // Logical name looked up on IBehaviourAction
        public bool requireComponent = true;
        public abstract Dictionary<(string,BlackboardKey.ValueType), BlackboardKey> BlackboardKeys { get; set; }
        public abstract override BTState Tick(BTContext ctx);

        public virtual bool TryGetValue<T>(BTContext ctx, string fieldName, out T value)
        {

            BlackboardKey keyRef = BlackboardKeys[(fieldName, Blackboard.TypeMap[typeof(T)])];
            if (keyRef == null)
            {
                value = default;
                return false;
            }

            if (!ctx.blackboard.TryGet<T>(keyRef.key, out value))
            {
                value = default;
                return false;
            }

            return true;
        }

    }
}