using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OreType
{
    None = 0, //
    Stone = 1, // 기본, 바닥, 길, 기본적인 구조를 만드는데 쓰임
    Iron = 2, // 건물을 지을 때 쓰임
    Copper = 3, // 전기와 관련된 구조를 만드는데 쓰임
    
    // ...
}

[System.Serializable]
public class DropRule
{
    public OreType oreType = OreType.None;
    [Min(0)] public int minCount = 0;             // 최소 보장 수량
    [Range(0f, 1f)] public float dropRate = 0.0f;   // 확률(추가 드랍용)
}

[System.Serializable]
public class BlockProp   // <-- struct 대신 class 권장
{
    public string id;                // "stone_block", "iron_ore_block" ...
    public string displayName;         // block 이름
    public int hardness;             // 도구 요구 레벨(높을수록 좋은 도구 필요)
    public int density;              // 채굴 시간/기본 산출량에 영향(밸런스 튜닝용)

    public GameObject prefab;        // 시각/사운드 등 레퍼런스(선택)
    public Sprite icon;
    [Header("Drop Table")]
    public List<DropRule> drops = new List<DropRule>();
}


[CreateAssetMenu(menuName = "Scriptable Object/MapBlockData", fileName = "MapBlockData")]
public class MapBlockData : ScriptableObject
{
    [Tooltip("displayName은 수정 가능, id는 내부에서 자동 생성되는 고유 키입니다.")]
    public List<BlockProp> BlockProperties = new List<BlockProp>();

    // 조회 캐시(런타임/에디터 공용, 에셋 저장 없음)
    [System.NonSerialized] Dictionary<string, int> _idxById;
    [System.NonSerialized] int _cachedCount;

    void OnEnable()
    {
        EnsureIds();      // 가벼운 고유화 (에디터에서만 호출됨)
        InvalidateCache();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureIds();      // 에디터에서 필드가 바뀔 때만 가볍게 한 번 훑음
        InvalidateCache();
    }
#endif

    void InvalidateCache()
    {
        _idxById = null;
        _cachedCount = -1;
    }

    // 빈/중복 id를 아주 가볍게 고유화 (O(n))
    void EnsureIds()
    {
        if (BlockProperties == null) return;

        var used = new HashSet<string>();

        for (int i = 0; i < BlockProperties.Count; i++)
        {
            var bp = BlockProperties[i];
            // 1) 비어있으면 새 id
            if (string.IsNullOrWhiteSpace(bp.id))
                bp.id = NewId();

            // 2) 중복이면 새 id
            if (!used.Add(bp.id))
                bp.id = AllocateUnique(bp.id, used);

            BlockProperties[i] = bp;
        }
    }

    // "blk_xxxxxx" 형태의 짧은 고유 id 생성
    string NewId()
    {
        // 짧은 토큰: GUID 6~8글자 일부 사용
        string token = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"blk_{token}";
    }

    string AllocateUnique(string baseId, HashSet<string> used)
    {
        // baseId가 이미 쓰였으면 _2, _3 ... 접미사
        int n = 2;
        string c;
        do { c = $"{baseId}_{n++}"; } while (used.Contains(c));
        used.Add(c);
        return c;
    }

    // ── 조회 (첫 호출만 O(n), 이후 O(1)) ──
    void BuildIndexIfNeeded()
    {
        int cur = BlockProperties?.Count ?? 0;
        if (_idxById != null && _cachedCount == cur) return;

        _idxById = new Dictionary<string, int>(cur);
        if (BlockProperties != null)
        {
            for (int i = 0; i < BlockProperties.Count; i++)
            {
                var id = BlockProperties[i].id;
                if (string.IsNullOrEmpty(id)) continue;
                if (!_idxById.ContainsKey(id)) _idxById.Add(id, i); // 최초 항목 우선
            }
        }
        _cachedCount = cur;
    }

    public bool TryGetById(string id, out BlockProp prop)
    {
        prop = default;
        if (string.IsNullOrEmpty(id)) return false;
        BuildIndexIfNeeded();
        if (_idxById.TryGetValue(id, out int idx))
        {
            prop = BlockProperties[idx];
            return true;
        }
        return false;
    }

#if UNITY_EDITOR
    // 필요시 수동 재발급(강제) 버튼
    [ContextMenu("IDs/Regenerate Missing Or Duplicated IDs")]
    void ForceEnsureIds()
    {
        EnsureIds();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
