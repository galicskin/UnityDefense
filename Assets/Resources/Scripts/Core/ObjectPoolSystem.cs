using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolSystem : SystemBase
{
    [System.Serializable]
    public class PoolEntry
    {
        public string key;
        public GameObject prefab;
        [Min(1)] public int defaultCapacity = 16;
        [Min(1)] public int maxSize = 256;
        public bool collectionChecks = false;

        public PoolEntry() { }
        public PoolEntry(string poolKey, GameObject poolPrefab, int defaultCapacity = 16, int maxSize = 256, bool collectionChecks = false)
        {
            key = poolKey;
            prefab = poolPrefab;
            this.defaultCapacity = defaultCapacity;
            this.maxSize = maxSize;
            this.collectionChecks = collectionChecks;
        }
    }

    [SerializeField] private List<PoolEntry> entries = new();

    // 등록(데이터)과 실체(풀)를 분리
    private readonly Dictionary<string, PoolEntry> registry = new();                    // key -> 설정(프리팹 등)
    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new();          // key -> 풀 인스턴스

    private void Awake()
    {
        // 인스펙터에 미리 넣어둔 엔트리들만 "등록만" 해둔다. (풀은 아직 만들지 않음)
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.key) || e.prefab == null) continue;
            if (!registry.ContainsKey(e.key))
                registry[e.key] = new PoolEntry(e.key, e.prefab, e.defaultCapacity, e.maxSize, e.collectionChecks);
        }
    }

    // ===== Lazy 등록/보조 메서드 =====

    public bool Contains(string key) => registry.ContainsKey(key);

    /// <summary>엔트리만 등록(멱등). 풀은 만들지 않음.</summary>
    public void SetEntry(string key, GameObject prefab, int defaultCapacity = 16, int maxSize = 256, bool collectionChecks = false)
    {
        if (string.IsNullOrEmpty(key) || prefab == null)
        {
            Debug.LogWarning("[Pool] SetEntry: invalid key/prefab");
            return;
        }

        if (registry.TryGetValue(key, out var exist))
        {
            // 이미 등록된 키면 동일 프리팹이면 무시, 다르면 경고만 남기고 유지(원하면 업데이트 로직으로 바꿔도 됨)
            if (exist.prefab != prefab)
                Debug.LogWarning($"[Pool] SetEntry: key '{key}' already registered with a different prefab. Keeping the first one.");
            return;
        }

        registry[key] = new PoolEntry(key, prefab, defaultCapacity, maxSize, collectionChecks);
    }

    /// <summary>필요할 때만 풀을 생성한다.</summary>
    private ObjectPool<GameObject> EnsurePool(string key)
    {
        if (pools.TryGetValue(key, out var pool))
            return pool;

        if (!registry.TryGetValue(key, out var entry) || entry.prefab == null)
        {
            Debug.LogError($"[Pool] EnsurePool: no registered entry for key '{key}'. Call SetEntry first.");
            return null;
        }

        // 풀 생성 (여기서 실제 생성)
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(entry.prefab);
                var po = go.GetComponent<PooledObject>();
                if (po == null) po = go.AddComponent<PooledObject>();

                // 반환 콜백
                po.ReturnToPool = (obj) => pools[key].Release(obj);

                // 초기 상태 비활성화
                po.OnReturnedToPool();
                return go;
            },
            actionOnGet: (go) =>
            {
                var po = go.GetComponent<PooledObject>();
                po?.OnTakenFromPool();
            },
            actionOnRelease: (go) =>
            {
                var po = go.GetComponent<PooledObject>();
                po?.OnReturnedToPool();
                go.transform.SetParent(transform, false); // 정리용 부모
            },
            actionOnDestroy: (go) =>
            {
                if (go != null) Destroy(go);
            },
            collectionCheck: entry.collectionChecks,
            defaultCapacity: entry.defaultCapacity,
            maxSize: entry.maxSize
        );

        pools[key] = pool;
        return pool;
    }

    // ===== Public API =====

    /// <summary>
    /// Lazy 스폰: key가 등록만 되어 있으면 이 순간 풀을 만들고 객체를 꺼낸다.
    /// </summary>
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        var pool = EnsurePool(key);
        if (pool == null) return null;

        var go = pool.Get();
        go.transform.SetPositionAndRotation(position, rotation);
        return go;
    }

    /// <summary>
    /// 키가 아직 등록되지 않은 경우, prefab을 넘기면 즉시 등록 후 스폰(완전 지연).
    /// </summary>
    public GameObject SpawnLazy(string key, GameObject prefabIfNotRegistered, Vector3 position, Quaternion rotation,
                                int defaultCapacity = 16, int maxSize = 256, bool collectionChecks = false)
    {
        if (!Contains(key))
            SetEntry(key, prefabIfNotRegistered, defaultCapacity, maxSize, collectionChecks);

        return Spawn(key, position, rotation);
    }

    /// <summary>미리 몇 개 만들어둘 때 사용(이 시점에 풀 생성)</summary>
    public void Prewarm(string key, int count)
    {
        var pool = EnsurePool(key);
        if (pool == null) return;

        // 간단 프리웜: 꺼냈다 바로 반납
        var temp = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
            temp.Add(pool.Get());
        foreach (var go in temp)
            pool.Release(go);
    }

    /// <summary>바깥에서 직접 반환해야 할 때</summary>
    public void Despawn(GameObject go)
    {
        if (go == null) return;
        var po = go.GetComponent<PooledObject>();
        if (po != null)
            po.ReturnToPool?.Invoke(go);
        else
            Destroy(go); // 풀 관리 대상이 아니면 그냥 파괴
    }
}
