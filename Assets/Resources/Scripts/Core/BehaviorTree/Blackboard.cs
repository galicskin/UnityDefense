using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTreeKit
{
    [CreateAssetMenu(menuName = "BehaviourTree/Blackboard", fileName = "Blackboard")]
    public class Blackboard : ScriptableObject
    {
        [SerializeField] private List<BlackboardKey> entries = new();

        // 빠른 조회용 인덱스 (key → entries index)
        [NonSerialized] private Dictionary<string, int> _index;

        // T → ValueType 매핑
        public static readonly Dictionary<Type, BlackboardKey.ValueType> TypeMap = new()
        {
            { typeof(bool), BlackboardKey.ValueType.Bool },
            { typeof(int), BlackboardKey.ValueType.Int },
            { typeof(float), BlackboardKey.ValueType.Float },
            { typeof(Vector3), BlackboardKey.ValueType.Vector3 },
            { typeof(UnityEngine.Object), BlackboardKey.ValueType.Object },
        };

        // ---------- 인덱스 관리 ----------
        private void RebuildIndex()
        {
            _index = new Dictionary<string, int>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var k = entries[i]?.key;
                if (string.IsNullOrEmpty(k)) continue;
                if (!_index.ContainsKey(k))
                    _index.Add(k, i); // 중복 키는 앞의 것을 유지
            }
        }

        private BlackboardKey FindEntry(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_index == null) RebuildIndex();
            return _index.TryGetValue(key, out var i) ? entries[i] : null;
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal); // 필요시 OrdinalIgnoreCase
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) { entries.RemoveAt(i--); continue; }

                var k = e.key;
                if (string.IsNullOrWhiteSpace(k)) continue; // 비어있으면 일단 놔둠(사용자가 입력할 시간 줌)

                k = k.Trim();
                var baseKey = k;
                int n = 1;
                while (!seen.Add(k))              // 이미 존재하면
                    k = $"{baseKey}_{n++}";       // 유니크하게 바꿔줌

                e.key = k;                        // (변경사항 반영)
            }
            RebuildIndex();
        }

#endif

        public BlackboardKey GetRefBlackboardKey(BlackboardKey.ValueType valueType, string key)
        {
            var e = FindEntry(key);
            return (e != null && e.type == valueType) ? e : null;
        }

        // 안전한 TryGet
        public bool TryGet<T>(string key, out T value)
        {
            value = default;

            foreach (var entry in entries)
            {
                Debug.Log($"{entry.key} : {entry.type}");
            }

            var e = FindEntry(key);
            if (e == null)
            {
                Debug.Log($"TryGet에서 FindEntry 부분 실패");
                return false;
            }

            if (!TypeMap.TryGetValue(typeof(T), out var vt))
            {
                Debug.Log($"TypeMap.TryGetValue 에서 vt 받아오기 실패");
                return false;
            }
            if (e.type != vt)
            {
                Debug.Log($"(e.type == vt");
                return false;
            }

            object boxed = null;
            switch (e.type)
            {
                case BlackboardKey.ValueType.Bool: boxed = e.boolValue; break;
                case BlackboardKey.ValueType.Int: boxed = e.intValue; break;
                case BlackboardKey.ValueType.Float: boxed = e.floatValue; break;
                case BlackboardKey.ValueType.Vector3: boxed = e.vector3Value; break;
                case BlackboardKey.ValueType.Object: boxed = e.objectValue; break;
                default:
                    Debug.Log($"{e.type} 이 default로 들어감");
                    return false;
            }

            if (boxed is T t)
            {
                Debug.Log("boxed is T t 성공");
                value = t;
                return true;
            }
            Debug.Log($"boxed is T t 실패 ");
            return false;
        }

        public bool TryGetByType(BlackboardKey.ValueType vt, string key, out object value)
        {
            value = null;
            if (string.IsNullOrEmpty(key)) return false;

            var e = entries.Find(x => x.key == key);
            if (e == null || e.type != vt) return false;

            switch (vt)
            {
                case BlackboardKey.ValueType.Bool: value = e.boolValue; return true;
                case BlackboardKey.ValueType.Int: value = e.intValue; return true;
                case BlackboardKey.ValueType.Float: value = e.floatValue; return true;
                case BlackboardKey.ValueType.Vector3: value = e.vector3Value; return true;
                case BlackboardKey.ValueType.Object: value = e.objectValue; return true;
                    // enum에 String을 쓰신다면 ↓ 추가
                    // case BlackboardKey.ValueType.String:  value = e.stringValue;  return true;
            }
            return false;
        }

        // 기존 Get<T>는 TryGet 기반으로 친절한 예외 메시지
        public T Get<T>(string key)
        {
            if (TryGet<T>(key, out var v)) return v;
            var e = FindEntry(key);
            if (e == null) throw new Exception($"Blackboard key not found: {key}");
            TypeMap.TryGetValue(typeof(T), out var vt);
            throw new Exception($"Type mismatch for key '{key}'. Stored={e.type}, Requested={typeof(T).Name}");
        }

        // Set: 기본은 타입을 '설정된 값의 타입'으로 덮어씀. 엄격 모드(strictType=true)면 타입 변경 금지.
        public bool Set<T>(string key, T value, bool strictType = false)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (!TypeMap.TryGetValue(typeof(T), out var vt))
                throw new Exception($"Unsupported type {typeof(T)} for blackboard set");

            var e = FindEntry(key);
            if (e == null)
            {
                e = new BlackboardKey { key = key, type = vt };
                entries.Add(e);
                _index = null; // 인덱스 리빌드 플래그
            }
            else if (strictType && e.type != vt)
            {
                // 타입이 이미 있는데 바꾸지 않도록 요청됨
                return false;
            }

            e.type = vt;
            switch (value)
            {
                case bool b: e.boolValue = b; break;
                case int i: e.intValue = i; break;
                case float f: e.floatValue = f; break;
                case Vector3 v3: e.vector3Value = v3; break;
                case UnityEngine.Object o: e.objectValue = o; break;
                default:
                    throw new Exception($"Unsupported type {typeof(T)} for blackboard set");
            }
            return true;
        }

        public bool Contains(string key) => FindEntry(key) != null;

        public bool Remove(string key)
        {
            if (_index == null) RebuildIndex();
            if (_index != null && _index.TryGetValue(key, out var i))
            {
                entries.RemoveAt(i);
                _index = null;
                return true;
            }
            return false;
        }

        public List<string> GetKeyList(BlackboardKey.ValueType valueType)
        {
            List<string> keyList = new();
            foreach (var entry in entries)
            {
                if (entry.type == valueType)
                {
                    keyList.Add(entry.key);
                }
            }
            return keyList;
        }

        public Blackboard CloneInstance()
        {
            var inst = Instantiate(this);
            // List<T> is value-copied because it's serialized; Instantiate copies serialized fields.
            return inst;
        }
    }
}
