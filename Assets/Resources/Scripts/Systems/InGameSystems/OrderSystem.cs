using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OrderType
{
    None,       // 아무 명령 없음 (대기)
    Mine,       // 광물 캐기
    Carry,      // 광물 옮기기
    Fight,      // 괴물 물리치기
    Move,       // 강제 이동
    Return,     // 본진 복귀
    // 추후 필요시 추가
}
public class Order
{
    public OrderType Type { get; private set; }

    // 예: 대상 위치, 목표 대상, 목표 자원 등
    public Vector3? TargetPosition { get; private set; }
    public GameObject TargetObject { get; private set; }

    public Order(OrderType type, Vector3? targetPos = null, GameObject targetObj = null)
    {
        Type = type;
        TargetPosition = targetPos;
        TargetObject = targetObj;
    }
}

public class OrderSystem : SystemBase
{
    private List<Worker> workers = new List<Worker>();

    private WorkersManageSystem WorkersManager;

    public void Start()
    {
        WorkersManager = GameSystemManager.Instance?.GetSystem<WorkersManageSystem>();
        if (!WorkersManager)
            Debug.Log("WorkersManager not exist");
    }

    // 새 명령 등록
    public void IssueOrder(Order order)
    {
        Worker worker = WorkersManager.FindLeastBusyWorker();

    }


}
