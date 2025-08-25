using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TowerId
{
    test = -1,
    None = 0,
    Archer_01 = 1001,
    Cannon_01 = 2001,
    // ...
}

public class BuildingSystem : SystemBase
{
    // 유저 또는 상점 등.. 어떤 방법으로 빌딩이 될 데이터 (프리팹 또는 데이터 id) 가질것
    
    public bool isBuildMode = false;
    bool CanBuild = false;
    TowerId previewTowerId = TowerId.None;
    public void CreateTower(TowerId towerId)
    {
        if (!CheckBuildCondition(towerId))
            return;

        // towerObject = towerId( 이걸로 타워 부르기 또는 만들기)

    }

    public bool CheckBuildCondition(TowerId towerId)
    {

        return CanBuild;
    }

    public GameObject previewTowerPrefab_B;
    public GameObject previewTowerPrefab_R; // 테스트 버전이므로 나중에 바뀔
public void Update()
{
    if (!isBuildMode) 
    {
        // 빌드모드가 아니면 프리뷰 숨김
        if (previewBlueInst) previewBlueInst.SetActive(false);
        if (previewRedInst)  previewRedInst.SetActive(false);
        return;
    }

    EnsurePreviews();

    var cam = GameManager.Instance.Player.GetCamera();
    var ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
    int environmentMask = LayerMask.GetMask("Environment");

    if (Physics.Raycast(ray, out RaycastHit hit, 100f, environmentMask))
    {
        // 설치 가능 파란 프리뷰 ON, 빨강 OFF
        if (previewRedInst)  previewRedInst.SetActive(false);
        if (previewBlueInst)
        {
            previewBlueInst.SetActive(true);
            previewBlueInst.transform.SetPositionAndRotation(hit.point, Quaternion.identity); // 회전 필요시 수정
        }
        CanBuild = true;
    }
    else
    {
        // 설치 불가 빨간 프리뷰 ON, 파랑 OFF
        if (previewBlueInst) previewBlueInst.SetActive(false);
        if (previewRedInst)
        {
            previewRedInst.SetActive(true);
            // 히트가 없을 때는 카메라 앞 임시 위치
            var pos = cam.transform.position + cam.transform.forward * 5f;
            previewRedInst.transform.SetPositionAndRotation(pos, Quaternion.identity);
        }
        CanBuild = false;
    }
}

    public void SetPreviewTower(TowerId towerId)
    {
        previewTowerId = towerId;
        // 여기서 Id랑 Tower프리팹이랑 연결하는건 나중에 ScriptableObject사용해서 데이터간 연결 시킬것
    }

    private GameObject previewBlueInst;
    private GameObject previewRedInst;
    private void EnsurePreviews()
    {
        if (previewBlueInst == null && previewTowerPrefab_B != null)
        {
            previewBlueInst = Instantiate(previewTowerPrefab_B);
            previewBlueInst.SetActive(false);
        }
        if (previewRedInst == null && previewTowerPrefab_R != null)
        {
            previewRedInst = Instantiate(previewTowerPrefab_R);
            previewRedInst.SetActive(false);
        }
    }

}
