using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITestScript : MonoBehaviour
{
    // 테스트용 나중에 지우고 데이터 베이스에서 에너미 정보를 받아올것
    [SerializeField] GameObject TestEnemy;

    [SerializeField] Transform SpawnPoint;

    public void SpawnTest()
    {
        ObjectPoolSystem.Instance.Spawn("TestEnemy", SpawnPoint.position, SpawnPoint.rotation);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            SpawnTest();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            SelectTower();
        }
    }

    public void SelectTower()
    {
        var buildSystem = GameSystemManager.Instance.GetSystem<BuildSystem>();
        if (!buildSystem) return;
        buildSystem.SetPreviewTower(TowerId.test);
    }

}
