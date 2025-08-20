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
        public string key;                 // ��: "TestEnemy"
        public GameObject prefab;
        [Min(1)] public int defaultCapacity = 16;
        [Min(1)] public int maxSize = 256;
        public bool collectionChecks = false; // ���� �߿� true ����
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

                    // ��ȯ �ݹ� ����
                    po.ReturnToPool = (obj) => pools[e.key].Release(obj);

                    // �ʱ� ���� ��Ȱ��
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
                    go.transform.SetParent(transform, false); // ����
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
            Debug.LogError($"PoolManager: '{key}' Ǯ�� �����ϴ�.");
            return null;
        }

        var go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }
}
