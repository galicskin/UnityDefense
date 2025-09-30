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

        // 타입별로 1번만 찾고 저장
        private readonly Dictionary<Type, object> _serviceCache = new();

        public T GetService<T>() where T : class
        {
            if (owner == null) return null;

            var t = typeof(T);
            if (_serviceCache.TryGetValue(t, out var cached))
                return (T)cached;

            // 최초 1회만 GetComponent
            var comp = owner.GetComponent<T>();
            _serviceCache[t] = comp; // comp가 null이어도 캐시(불필요한 재탐색 방지)
            return comp;
        }
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

    [System.Serializable]
    public struct BBKeyRef
    {
        public string PropertyName; // actionNode에서 사용되는 프로퍼티 이름
        public BlackboardKey.ValueType PropertyType; // actionNode에서 사용되는 프로퍼티 타입
        public BlackboardKey BlackboardKeyRef; // blakcboard에서의 실제 참조(에디터에서 설정)
        public BBKeyRef(string propertyName, BlackboardKey.ValueType propertyType, BlackboardKey blackboardKey)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            BlackboardKeyRef = blackboardKey;
        }

    }

    public abstract class ActionNode : BTNode
    {
        public string actionName = ""; // Logical name looked up on IBehaviourAction
        public bool requireComponent = true;

        [SerializeField] public List<BBKeyRef> ActionNodeSerializedKeys = new();
        [NonSerialized] private Dictionary<string, int> _indexOf;

        protected void RebuildIndex()
        {
            _indexOf = new Dictionary<string, int>(ActionNodeSerializedKeys.Count);
            for (int i = 0; i < ActionNodeSerializedKeys.Count; i++)
            {
                var name = ActionNodeSerializedKeys[i].PropertyName;
                if (!string.IsNullOrEmpty(name))
                    _indexOf[name] = i; // 중복일 경우 마지막 항목이 덮어씀
            }
        }

        private void OnEnable()
        {
            BuildBlackboardKeys();
        }

        protected virtual void OnValidate()
        {
            BuildBlackboardKeys();
            RebuildIndex();
        }
        public abstract void BuildBlackboardKeys();

        public abstract override BTState Tick(BTContext ctx);

        public bool TryGetRef(string fieldName, out BBKeyRef result)
        {
            if (_indexOf == null) RebuildIndex();

            if (_indexOf.TryGetValue(fieldName, out int idx)
                && idx >= 0 && idx < ActionNodeSerializedKeys.Count)
            {
                result = ActionNodeSerializedKeys[idx];
                return true;
            }

            result = default;
            return false;
        }

        public bool TryGetKey(string fieldName, out BlackboardKey key)
        {
            if (TryGetRef(fieldName, out var refItem))
            {
                key = refItem.BlackboardKeyRef;
                return key != null;
            }
            key = null;
            return false;
        }

        public void AddKeyIfNotExists(string propertyName, BlackboardKey.ValueType type)
        {
            if (!ActionNodeSerializedKeys.Exists(r =>
                r.PropertyName == propertyName && r.PropertyType == type))
            {
                ActionNodeSerializedKeys.Add(new BBKeyRef(propertyName, type, null));
            }
        }

        public void ChangeBlackboardKey(string propertyName, BlackboardKey.ValueType type, BlackboardKey changeKey)
        {
            int idx = ActionNodeSerializedKeys.FindIndex(r =>
                r.PropertyName == propertyName && r.PropertyType == type);

            if (idx >= 0)
            {
                // BBKeyRef가 struct라면 "읽어서 수정 후 다시 대입"이 필요합니다.
                var item = ActionNodeSerializedKeys[idx];
                item.BlackboardKeyRef = changeKey;
                ActionNodeSerializedKeys[idx] = item;   // ← struct 대비 안전
            }
        }
    }
}