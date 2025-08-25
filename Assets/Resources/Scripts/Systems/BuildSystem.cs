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
    // ���� �Ǵ� ���� ��.. � ������� ������ �� ������ (������ �Ǵ� ������ id) ������
    
    public bool isBuildMode = false;
    bool CanBuild = false;
    TowerId previewTowerId = TowerId.None;
    public void CreateTower(TowerId towerId)
    {
        if (!CheckBuildCondition(towerId))
            return;

        // towerObject = towerId( �̰ɷ� Ÿ�� �θ��� �Ǵ� �����)

    }

    public bool CheckBuildCondition(TowerId towerId)
    {

        return CanBuild;
    }

    public GameObject previewTowerPrefab_B;
    public GameObject previewTowerPrefab_R; // �׽�Ʈ �����̹Ƿ� ���߿� �ٲ�
public void Update()
{
    if (!isBuildMode) 
    {
        // �����尡 �ƴϸ� ������ ����
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
        // ��ġ ���� �Ķ� ������ ON, ���� OFF
        if (previewRedInst)  previewRedInst.SetActive(false);
        if (previewBlueInst)
        {
            previewBlueInst.SetActive(true);
            previewBlueInst.transform.SetPositionAndRotation(hit.point, Quaternion.identity); // ȸ�� �ʿ�� ����
        }
        CanBuild = true;
    }
    else
    {
        // ��ġ �Ұ� ���� ������ ON, �Ķ� OFF
        if (previewBlueInst) previewBlueInst.SetActive(false);
        if (previewRedInst)
        {
            previewRedInst.SetActive(true);
            // ��Ʈ�� ���� ���� ī�޶� �� �ӽ� ��ġ
            var pos = cam.transform.position + cam.transform.forward * 5f;
            previewRedInst.transform.SetPositionAndRotation(pos, Quaternion.identity);
        }
        CanBuild = false;
    }
}

    public void SetPreviewTower(TowerId towerId)
    {
        previewTowerId = towerId;
        // ���⼭ Id�� Tower�������̶� �����ϴ°� ���߿� ScriptableObject����ؼ� �����Ͱ� ���� ��ų��
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
