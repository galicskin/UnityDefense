using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// �ý��� �Ŵ���
// �� ���� �ý����� ��� 
// ��ϵ� �ý����� ����, �� �� �ǹ��ϴ� On Off����� ����
// On, Off �� �ý��� �Ŵ������� �˸��� ����

public class SystemManager : MonoBehaviour
{
    private List<SystemBase> systems = new List<SystemBase>();


    // ���� ���
    public virtual void RegisterSystem<T>(bool isCreate = false) where T : SystemBase
    {
     // ���� ��ϵ� �ý��� ã��
        var existing = systems.Find(s => s is T);

        if (existing != null)
        {
            if (isCreate)
            {
                // ���� �ý��� ����
                systems.Remove(existing);
                Destroy(existing); // MonoBehaviour�� Destroy �ʿ�
                Debug.Log($"[SystemManager] {typeof(T).Name} �ý����� ���� �����մϴ�.");
            }
            else
            {
                Debug.Log($"[SystemManager] {typeof(T).Name} �ý����� �̹� ��ϵǾ� �־� �����մϴ�.");
                return; // �׳� ����
            }
        }

        // ���� ����
        T newSystem = gameObject.AddComponent<T>();
        systems.Add(newSystem);

        // �̺�Ʈ ����
        newSystem.OnSystemStarted += HandleSystemOn;
        newSystem.OnSystemStopped += HandleSystemOff;

        Debug.Log($"[SystemManager] {typeof(T).Name} �ý����� ��ϵǾ����ϴ�.");
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
        Debug.Log($"[SystemManager] {system.name} �ý����� �������ϴ�.");
    }

    public virtual void HandleSystemOff(SystemBase system)
    {
        Debug.Log($"[SystemManager] {system.name} �ý����� �������ϴ�.");
    }

}
