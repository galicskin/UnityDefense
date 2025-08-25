using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigator : MonoBehaviour
{ 
    [SerializeField] private Transform target;        // 따라갈 목표
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float stopDistance = 1.0f;
    [SerializeField] private LayerMask navmeshAreaMask = ~0; // 모든 영역 허용(필요시 마스크로 제한)

    private NavMeshAgent agent;
    private float timer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;
        
        // 테스트용
        target = GameObject.Find("Plane").transform;
        // agent.areaMask = 1 << NavMesh.GetAreaFromName("Walkable"); // 특정 Area만 허용하려면 이렇게
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= repathInterval)
        {
            timer = 0f;
            SetSafeDestination(target.position);
        }
    }

    // 목표 지점이 네브메시 구역이 아니더라도, 가장 가까운 유효 지점으로 스냅
    void SetSafeDestination(Vector3 desired)
    {
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, agent.areaMask))
        {
            // 경로 유효성 검사
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
            }
            else
            {
                // 경로가 끊기면: 목표 근처의 다른 유효 지점 탐색 (반경 확대 등)
                if (NavMesh.SamplePosition(desired, out NavMeshHit nearHit, 20f, agent.areaMask))
                {
                    if (agent.CalculatePath(nearHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                        agent.SetPath(path);
                }
            }
        }
        // SamplePosition 실패 시: 현재 위치 유지 혹은 대체 행동(순찰 등)
    }
}
