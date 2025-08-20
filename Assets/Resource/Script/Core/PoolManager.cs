using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public string key;                 // 예: "TestEnemy"
        public GameObject prefab;
        [Min(1)] public int defaultCapacity = 16;
        [Min(1)] public int maxSize = 256;
        public bool collectionChecks = false; // 개발 중엔 true 권장
    }

    [SerializeField] private List<PoolEntry> entries = new();

    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new();
    private readonly Dictionary<string, GameObject> prefabs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var e in entries)
        {
            if (e.prefab == null || string.IsNullOrEmpty(e.key)) continue;
            prefabs[e.key] = e.prefab;

            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Instantiate(e.prefab);
                    var po = go.GetComponent<PooledObject>();
                    if (po == null) po = go.AddComponent<PooledObject>();

                    // 반환 콜백 주입
                    po.ReturnToPool = (obj) => pools[e.key].Release(obj);

                    // 초기 상태 비활성
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
                    go.transform.SetParent(transform, false); // 정리
                },
                actionOnDestroy: (go) =>
                {
                    if (go != null) Destroy(go);
                },
                collectionCheck: e.collectionChecks,
                defaultCapacity: e.defaultCapacity,
                maxSize: e.maxSize
            );

            pools[e.key] = pool;
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot)
    {
        if (!pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"PoolManager: '{key}' 풀이 없습니다.");
            return null;
        }

        var go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }
}
