using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : PooledObject
{
    [SerializeField] protected float moveSpeed = 2f;

    public override void OnTakenFromPool()
    {
        base.OnTakenFromPool();
        // 예: HP 초기화, 상태 리셋 등
    }

    public override void OnReturnedToPool()
    {
        base.OnReturnedToPool();
        // 예: 이펙트 중지, 변수 초기화 등
    }
}
