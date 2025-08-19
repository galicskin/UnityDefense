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
        [HideInInspector] public string displayName;            // ǥ�ÿ� (FullName)
        [HideInInspector] public string assemblyQualifiedName;  // Ÿ�� ������
        public bool enable;                                     // �������
    }

    [Header("�ڵ� ��ĵ�� SystemBase �Ļ� Ÿ�� (üũ�� �͸� ���)")]
    [SerializeField] private List<DiscoveredSystemEntry> discoveredSystems = new();

#if UNITY_EDITOR
    // �����Ϳ��� ���� �ٲ�ų� ������ ���ε�� �� �ڵ� ȣ��
    private void OnValidate()
    {
        // 1) ���� �������� ��� SystemBase �Ļ� Ÿ�� ���� (�߻� ����)
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

        // 2) ���� ��ϰ� ��Ī(�̸�����). ���� Ÿ���� �߰�, ����� Ÿ���� ����
        var indexByAQN = discoveredSystems.ToDictionary(x => x.assemblyQualifiedName, x => x);
        var next = new List<DiscoveredSystemEntry>(foundTypes.Count);

        foreach (var t in foundTypes.OrderBy(t => t.FullName)) // ���� ��Ģ(���ϸ� �ٲټ���)
        {
            var aqn = t.AssemblyQualifiedName;
            if (aqn == null) continue;

            if (indexByAQN.TryGetValue(aqn, out var keep))
            {
                // ����
                keep.displayName = t.FullName;
                next.Add(keep);
            }
            else
            {
                // �ű� �߰� (�ʱ⿣ ����)
                next.Add(new DiscoveredSystemEntry
                {
                    displayName = t.FullName,
                    assemblyQualifiedName = aqn,
                    enable = false
                });
            }
        }

        discoveredSystems = next;
        // �ν����Ϳ� ǥ�� �̸� ����
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
                Debug.LogError($"[GameSystemManager] Ÿ���� ã�� ���߽��ϴ�: {entry.displayName}");
                continue;
            }

            RegisterByType(type);  // �ʿ� �� true��
        }
    }

    void Update()
    {

    }

    // ---- ��Ƽ���� ���� �� ��Ÿ�� ���� ���� ----
    public override void HandleSystemOn(SystemBase system)
    {
        base.HandleSystemOn(system); // ���� �α� ���� ���ϸ� ȣ��
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
