using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    // PoolManager가 주입해주는 반환 콜백
    public Action<GameObject> ReturnToPool;

    // 외부에서 호출하여 반납
    public void Despawn()
    {
        if (ReturnToPool != null) ReturnToPool(gameObject);
        else gameObject.SetActive(false);
    }

    // 가져올 때/반납될 때 초기화용 훅 (선택)
    public virtual void OnTakenFromPool() { gameObject.SetActive(true); }
    public virtual void OnReturnedToPool() { gameObject.SetActive(false); }
}
