using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // �׽�Ʈ�� ���߿� ����� ������ ���̽����� ���ʹ� ������ �޾ƿð�
    [SerializeField] GameObject TestEnemy;

    [SerializeField] Transform SpawnPoint;

    public void SpawnTest()
    {
        PoolManager.Instance.Spawn("TestEnemy", SpawnPoint.position, SpawnPoint.rotation);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            SpawnTest();
        }
    }

}
