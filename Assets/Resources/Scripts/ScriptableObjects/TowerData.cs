using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TowerId
{
    test = -1,
    None = 0,
    Tower1 = 1,
    Tower2 = 2,
    Tower3 = 3,
    // ...
}
[System.Serializable]
public class TowerPrefabEntry
{
    public TowerId id;
    public GameObject prefab;
}

[CreateAssetMenu(menuName = "Scriptable Object/TowerData", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [SerializeField] private List<TowerPrefabEntry> towerPrefabs = new List<TowerPrefabEntry>();

    // 런타임 조회용
    private Dictionary<TowerId, GameObject> _towerDataMap;


    private Dictionary<TowerId, GameObject> prefabMap;

    private void OnEnable()
    {
        prefabMap = new Dictionary<TowerId, GameObject>();
        foreach (var e in towerPrefabs)
        {
            if (!prefabMap.ContainsKey(e.id))
                prefabMap.Add(e.id, e.prefab);
        }
    }

    public GameObject GetTowerPrefab(TowerId id)
    {
        return prefabMap.TryGetValue(id, out var prefab) ? prefab : null;
    }
}