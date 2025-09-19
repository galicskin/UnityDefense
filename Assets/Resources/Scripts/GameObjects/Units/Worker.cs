using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviourTreeKit;
public class Worker : PooledObject, ISelectable
{
    //private NavMeshAgent agent;
    //private Animator animator;
    
    [HideInInspector] public WorkerState State = WorkerState.Idle;

    Order currentOrder = new Order(OrderType.None);
    Queue<Order> normalQueue = new Queue<Order>(); // 평시
    Stack<Order> emergencyStack = new Stack<Order>(); // 비상

    BehaviourTreeRunner behaviourTreeRunner;

    // 내부 검색용(정렬 캐시)
    [System.NonSerialized] public float __lastDist2;

    private WorkersManageSystem _manager;
    private WorkersManageSystem Manager
    {
        get
        {
            if (_manager == null)
                _manager = GameSystemManager.Instance?.GetSystem<WorkersManageSystem>();
            return _manager;
        }
    }
    private void Awake()
    {
        //agent = GetComponent<NavMeshAgent>();
        //agent = GetComponent<NavMeshAgent>();
        //animator = GetComponentInChildren<Animator>();
        if (behaviourTreeRunner == null)
        {
            behaviourTreeRunner = this.gameObject.AddComponent<BehaviourTreeRunner>();
            behaviourTreeRunner.treeAsset = Resources.Load<BTAsset>("BehaviourTreeData/WorkerBehaviour");

        }
        behaviourTreeRunner.autoRun = false;
    }

    // ── PooledObject 훅 ─────────────────────────────────────
    public override void OnTakenFromPool()
    {
        base.OnTakenFromPool(); // gameObject.SetActive(true)

        // 런타임 상태 초기화
        //hp = maxHP;
        //
        //// NavMesh/Animator 등 리셋
        //if (agent)
        //{
        //    // Spawn 직후 위치가 바뀌므로, SetPositionAndRotation 이후에 Warp 권장
        //    // (스폰 호출부에서 pos/rot를 세팅해 준 다음 프레임에 Warp 하면 안전)
        //    agent.enabled = true;
        //}
        //if (animator)
        //{
        //    animator.Rebind();
        //    animator.Update(0f);
        //}

        // 태스크/코루틴/타겟 초기화 등 필요한 것들…
        // TaskRunner.Clear();
    }

    public override void OnReturnedToPool()
    {
        // 여기서 코루틴/이벤트/타겟 정리
        // StopAllCoroutines();

        //if (agent) agent.enabled = false;

        base.OnReturnedToPool(); // gameObject.SetActive(false)
    }

    // ── 게임 로직 예시 ─────────────────────────────────────
    public void Init(/* 팀/아키타입 등 필요 파라미터 */)
    {
        // 스폰 직후 외부에서 호출해 세부값 주입
    }

    private void Die()
    {
        // 이펙트/사운드 재생 후 풀로 반납
        Despawn(); // PooledObject.Despawn() → ObjectPool.Release() 호출
    }

    // 등록/해제
    private void OnEnable() => Manager.Register(this);
    private void OnDisable() => Manager.Unregister(this);

    public void MarkReserved() => Manager.SetState(this, WorkerState.Reserved);
    public void MarkBusy() => Manager.SetState(this, WorkerState.Busy);
    public void MarkIdle() => Manager.SetState(this, WorkerState.Idle);


    Transform ISelectable.Transform
    {
        get { return this.transform; }
    }

    // SelectionType 구현
    SelectionType ISelectable.SelectionType
    {
        get { return SelectionType.Worker; }
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


    //BehaviourTree behaviourTree;

    // ========== 1) Order가 바뀔 때 1회 호출 ==========
    public void UpdateOrder(Order order)
    {
        currentOrder = order;
        State = (order == null || order.Type == OrderType.None) ? WorkerState.Idle : WorkerState.Busy;
        // Order업데이트
    }

    public void ReceiveNormalOrder(Order order)
    {
        normalQueue.Enqueue(order);
        UpdateOrder(order);
    }
    public void ReceiveEmergencyOrder(Order order)
    {
        emergencyStack.Push(order);
        UpdateOrder(order);
    }

    // ========== 2) 매 프레임 실행(세부 이동/행동) ==========
    void Update()
    {
        if (currentOrder == null || currentOrder.Type == OrderType.None)
        {
            State = WorkerState.Idle;
            return;
        }

        behaviourTreeRunner.Tick(Time.deltaTime);
    }

}
