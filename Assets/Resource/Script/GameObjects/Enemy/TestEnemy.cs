using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemy : EnemyBase
{
    private Vector3 _dir;

    private void OnEnable()
    {
        var r = Random.insideUnitSphere; r.y = 0f;
        _dir = r.normalized;
    }

    private void Update()
    {
        //transform.position += _dir * moveSpeed * Time.deltaTime;

        // ����: ȭ�� ��/���� ���� �� �ݳ�
        // if (����) Despawn();
    }
}
