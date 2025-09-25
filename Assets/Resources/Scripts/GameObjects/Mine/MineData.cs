using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MineProp
{
    public OreType oreType;
    public GameObject prefab;
}

[CreateAssetMenu(menuName = "Scriptable Object/MineData", fileName = "MineData")]
public class MineData : ScriptableSingleton<MineData>
{
    public List<MineProp> mineProps = new();

    // 런타임 캐시
    private Dictionary<OreType, GameObject> _prefabMap;

    protected override void OnEnable()
    {
        base.OnEnable();
        BuildMap(); // 에셋이 로드될 때 캐시 구성
    }

#if UNITY_EDITOR
    // 인스펙터에서 값 바꿀 때도 중복 체크/캐시 갱신
    void OnValidate()
    {
        BuildMap();
    }
#endif

    private void BuildMap()
    {
        _prefabMap = new Dictionary<OreType, GameObject>();
        if (mineProps == null) return;

        var seen = new HashSet<OreType>();
        foreach (var p in mineProps)
        {
            if (p == null) continue;
            if (p.prefab == null)
            {
                Debug.LogWarning($"[MineData] '{p.oreType}'에 prefab이 비어 있습니다.", this);
                continue;
            }

            if (!seen.Add(p.oreType))
            {
                Debug.LogWarning($"[MineData] '{p.oreType}'가 중복 등록되었습니다. 첫 항목만 사용합니다.", this);
                continue;
            }

            _prefabMap[p.oreType] = p.prefab;
        }
    }

    public bool TryGetPrefab(OreType type, out GameObject prefab)
    {
        if (_prefabMap == null || _prefabMap.Count == 0)
            BuildMap();

        return _prefabMap.TryGetValue(type, out prefab);
    }

    public GameObject GetPrefabOrNull(OreType type)
    {
        return TryGetPrefab(type, out var go) ? go : null;
    }
}
