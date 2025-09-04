using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BuildSystem : SystemBase
{
    // 유저 또는 상점 등.. 어떤 방법으로 빌딩이 될 데이터 (프리팹 또는 데이터 id) 가질것
    
    public bool isBuildMode = false;
    bool CanBuild = false;
    [SerializeField]TowerId previewTowerId = TowerId.None;

    [SerializeField] private TowerData towerDataAsset;     
    [SerializeField] private string resourcePath = "Scripts/ScriptableObjects/TowerData";

    private TowerData towerData
    {
        get
        {
            if (towerDataAsset) return towerDataAsset;
            towerDataAsset = Resources.Load<TowerData>(resourcePath);
            if (!towerDataAsset)
                Debug.LogError($"TowerData를 찾지 못했습니다. Inspector 또는 Resources(\"{resourcePath}\") 확인!");
            return towerDataAsset;
        }
    }


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

    public void Update()
    {
        if (!isBuildMode || previewTowerId == TowerId.None) 
        {
            // 빌드모드가 아니면 프리뷰 숨김
            if (previewBlueInst) previewBlueInst.SetActive(false);
            if (previewRedInst)  previewRedInst.SetActive(false);
            return;
        }

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

    private GameObject previewBlueInst;
    private GameObject previewRedInst;
    // UI선택과 연결될 부분임.. 테스트를 위해 따로 버튼을 만들자
    public void SetPreviewTower(TowerId towerId)
    {
        previewTowerId = towerId;

        var prefabObject = towerData.GetTowerPrefab(previewTowerId);
        if (!prefabObject) return;
        TowerBase tower = prefabObject.GetComponent<TowerBase>();
        if (!tower) return;
        
        previewBlueInst = Instantiate(tower.PreviewObjectBlue);
        previewRedInst = Instantiate(tower.PreviewObjectRed);
        
        // 여기서 Id랑 Tower프리팹이랑 연결하는건 나중에 ScriptableObject사용해서 데이터간 연결 시킬것
    }


}
