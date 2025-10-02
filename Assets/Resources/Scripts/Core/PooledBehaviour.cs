using System;
using System.Collections.Generic;
using UnityEngine;

public class PooledBehaviour<T> where T : MonoBehaviour
{
    private List<T> poolList = new();
    private GameObject prefab = null;
    private Action<T> initSetting;
    public PooledBehaviour(GameObject Prefab)
    {
        this.poolList = new();
        var comp = Prefab.GetComponent<T>();
        if (!comp)
        {
            string componentName = typeof(T).Name;
            Debug.LogError($"{componentName} 이 없음! ");
        }
        prefab = Prefab;
    }

    public T Spawn()
    {
        if (prefab == null)
        {
            string componentName = typeof(T).Name;
            Debug.LogError($"{componentName} 가 있는 프리팹이 없음! ");
            return null;
        }

        // 1) 비활성 인스턴스 재사용
        for (int i = poolList.Count - 1; i >= 0; --i)
        {
            var inst = poolList[i];
            if (inst != null && !inst.gameObject.activeSelf)
            {
                inst.gameObject.SetActive(true);
                return inst;
            }
        }

        // 2) 없으면 새로 생성
        var go = UnityEngine.Object.Instantiate(prefab);
        var comp = go.GetComponent<T>() ?? go.AddComponent<T>();
        initSetting?.Invoke(comp);
        poolList.Add(comp);
        go.SetActive(true);
        return comp;
    }

    public void Despawn(T DespawnTarget)
    {
        if (DespawnTarget == null) return;
        DespawnTarget.gameObject.SetActive(false);
        // 리스트에 없을 수도 있으니 필요하면 추가로 등록
        if (!poolList.Contains(DespawnTarget))
            poolList.Add(DespawnTarget);
    }

    public void InitSetting(Action<T> settingAction)
    {
        initSetting = settingAction;
    }

}
