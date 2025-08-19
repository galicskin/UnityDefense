using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;


public class GameSystemManager : SystemManager
{

    [Serializable]
    private class DiscoveredSystemEntry
    {
        [HideInInspector] public string displayName;            // 표시용 (FullName)
        [HideInInspector] public string assemblyQualifiedName;  // 타입 복원용
        public bool enable;                                     // 등록할지
    }

    [Header("자동 스캔된 SystemBase 파생 타입 (체크한 것만 등록)")]
    [SerializeField] private List<DiscoveredSystemEntry> discoveredSystems = new();

#if UNITY_EDITOR
    // 에디터에서 값이 바뀌거나 도메인 리로드될 때 자동 호출
    private void OnValidate()
    {
        // 1) 현재 도메인의 모든 SystemBase 파생 타입 수집 (추상 제외)
        var foundTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            })
            .Where(t => typeof(SystemBase).IsAssignableFrom(t) && !t.IsAbstract)
            .Distinct()
            .ToList();

        // 2) 기존 목록과 매칭(이름으로). 없던 타입은 추가, 사라진 타입은 제거
        var indexByAQN = discoveredSystems.ToDictionary(x => x.assemblyQualifiedName, x => x);
        var next = new List<DiscoveredSystemEntry>(foundTypes.Count);

        foreach (var t in foundTypes.OrderBy(t => t.FullName)) // 정렬 규칙(원하면 바꾸세요)
        {
            var aqn = t.AssemblyQualifiedName;
            if (aqn == null) continue;

            if (indexByAQN.TryGetValue(aqn, out var keep))
            {
                // 유지
                keep.displayName = t.FullName;
                next.Add(keep);
            }
            else
            {
                // 신규 추가 (초기엔 꺼둠)
                next.Add(new DiscoveredSystemEntry
                {
                    displayName = t.FullName,
                    assemblyQualifiedName = aqn,
                    enable = false
                });
            }
        }

        discoveredSystems = next;
        // 인스펙터에 표시 이름 갱신
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public override void RegisterSystem<T>(bool isCreate = false)
    {
        base.RegisterSystem<T>(isCreate);
        UpdateEntryEnable(typeof(T), true);
    }
    void RegisterByType(Type type, bool isCreate = false)
    {
        var mi = GetType().GetMethod(nameof(RegisterSystem), new[] { typeof(bool) });
        var gmi = mi.MakeGenericMethod(type);
        gmi.Invoke(this, new object[] { isCreate });
    }

    void Start()
    {
        //RegisterSystem<GameFlowSystem>();
        foreach (var entry in discoveredSystems.Where(e => e.enable))
        {
            var type = Type.GetType(entry.assemblyQualifiedName, throwOnError: false);
            if (type == null)
            {
                Debug.LogError($"[GameSystemManager] 타입을 찾지 못했습니다: {entry.displayName}");
                continue;
            }

            RegisterByType(type);  // 필요 시 true로
        }
    }

    void Update()
    {

    }

    // ---- 노티파이 수신 시 런타임 상태 갱신 ----
    public override void HandleSystemOn(SystemBase system)
    {
        base.HandleSystemOn(system); // 기존 로그 유지 원하면 호출
    }

    public override void HandleSystemOff(SystemBase system)
    {
        base.HandleSystemOff(system);
    }

    private void UpdateEntryEnable(Type type, bool enable)
    {
        var aqn = type.AssemblyQualifiedName;
        var entry = discoveredSystems.FirstOrDefault(e => e.assemblyQualifiedName == aqn);
        if (entry == null) return;

        entry.enable = enable;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
