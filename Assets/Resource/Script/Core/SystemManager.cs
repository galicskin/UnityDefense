using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 시스템 매니저
// 각 개별 시스템을 등록 
// 등록된 시스템은 시작, 끝 을 의미하는 On Off기능이 있음
// On, Off 시 시스템 매니저에게 알림이 들어옴

public class SystemManager : MonoBehaviour
{
    private List<SystemBase> systems = new List<SystemBase>();


    // 정적 등록
    public virtual void RegisterSystem<T>(bool isCreate = false) where T : SystemBase
    {
     // 기존 등록된 시스템 찾기
        var existing = systems.Find(s => s is T);

        if (existing != null)
        {
            if (isCreate)
            {
                // 기존 시스템 제거
                systems.Remove(existing);
                Destroy(existing); // MonoBehaviour라 Destroy 필요
                Debug.Log($"[SystemManager] {typeof(T).Name} 시스템을 새로 생성합니다.");
            }
            else
            {
                Debug.Log($"[SystemManager] {typeof(T).Name} 시스템이 이미 등록되어 있어 유지합니다.");
                return; // 그냥 유지
            }
        }

        // 새로 생성
        T newSystem = gameObject.AddComponent<T>();
        systems.Add(newSystem);

        // 이벤트 연결
        newSystem.OnSystemStarted += HandleSystemOn;
        newSystem.OnSystemStopped += HandleSystemOff;

        Debug.Log($"[SystemManager] {typeof(T).Name} 시스템이 등록되었습니다.");
    }

    public virtual void OnSystem(SystemBase system)
    {
        system.OnSystem();
    }

    public virtual void OffSystem(SystemBase system)
    {
        system.OffSystem();
    }

    public virtual void OffAllSystems()
    {
        foreach (var sys in systems)
            OffSystem(sys);
    }

    public virtual void HandleSystemOn(SystemBase system)
    {
        Debug.Log($"[SystemManager] {system.name} 시스템이 켜졌습니다.");
    }

    public virtual void HandleSystemOff(SystemBase system)
    {
        Debug.Log($"[SystemManager] {system.name} 시스템이 꺼졌습니다.");
    }

}
