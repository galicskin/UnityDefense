using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    // PoolManager�� �������ִ� ��ȯ �ݹ�
    public Action<GameObject> ReturnToPool;

    // �ܺο��� ȣ���Ͽ� �ݳ�
    public void Despawn()
    {
        if (ReturnToPool != null) ReturnToPool(gameObject);
        else gameObject.SetActive(false);
    }

    // ������ ��/�ݳ��� �� �ʱ�ȭ�� �� (����)
    public virtual void OnTakenFromPool() { gameObject.SetActive(true); }
    public virtual void OnReturnedToPool() { gameObject.SetActive(false); }
}
