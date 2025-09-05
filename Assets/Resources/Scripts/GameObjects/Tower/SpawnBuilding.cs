using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBuilding : TowerBase, ISelectable
{
    public override int Id => 1; // 임시적인 번호 할당

    ObjectPoolSystem _Spawner;
    ObjectPoolSystem Spawner 
    {
        get
        {
            if (!_Spawner)
                _Spawner = GameSystemManager.Instance.GetSystem<ObjectPoolSystem>();
            return _Spawner;
        }
    }

    [SerializeField] Transform SpawnTransform; 


    public void SpawnWorker()
    {
        Quaternion spawnRot = GetYawOnlyRotation(SpawnTransform);
        Spawner.Spawn("Worker", SpawnTransform.position, spawnRot);
    }

    // 추후 유틸로 옮겨질 수 있음
    private static Quaternion GetYawOnlyRotation(Transform t)
    {
        Vector3 f = Vector3.ProjectOnPlane(t.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-6f) f = Vector3.right; // 안전장치
        return Quaternion.LookRotation(f.normalized, Vector3.up);
    }

    Transform ISelectable.Transform
    {
        get { return this.transform; }
    }
    // SelectionType 구현
    SelectionType ISelectable.SelectionType
    {
        get { return SelectionType.Building; }
    }
    Bounds ISelectable.SelectionBounds
    {
        get
        {
            var renderer = GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.one);
        }
    }
    void ISelectable.OnSelected()
    {
        Debug.Log($"{name} 선택됨");
        // TODO: 아웃라인 표시 or UI 갱신
    }

    void ISelectable.OnDeselected()
    {
        Debug.Log($"{name} 선택 해제");
        // TODO: 아웃라인 제거 or UI 닫기
    }
}
