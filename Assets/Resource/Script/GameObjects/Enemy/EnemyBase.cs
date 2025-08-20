using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : PooledObject
{
    [SerializeField] protected float moveSpeed = 2f;

    public override void OnTakenFromPool()
    {
        base.OnTakenFromPool();
        // ��: HP �ʱ�ȭ, ���� ���� ��
    }

    public override void OnReturnedToPool()
    {
        base.OnReturnedToPool();
        // ��: ����Ʈ ����, ���� �ʱ�ȭ ��
    }
}
