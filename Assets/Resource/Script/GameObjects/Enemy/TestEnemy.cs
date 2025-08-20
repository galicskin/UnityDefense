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

        // 예시: 화면 밖/수명 만료 시 반납
        // if (조건) Despawn();
    }
}
