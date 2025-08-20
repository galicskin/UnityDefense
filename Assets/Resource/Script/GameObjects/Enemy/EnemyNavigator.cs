using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigator : MonoBehaviour
{ 
    [SerializeField] private Transform target;        // ���� ��ǥ
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float stopDistance = 1.0f;
    [SerializeField] private LayerMask navmeshAreaMask = ~0; // ��� ���� ���(�ʿ�� ����ũ�� ����)

    private NavMeshAgent agent;
    private float timer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;
        
        // �׽�Ʈ��
        target = GameObject.Find("Plane").transform;
        // agent.areaMask = 1 << NavMesh.GetAreaFromName("Walkable"); // Ư�� Area�� ����Ϸ��� �̷���
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

    // ��ǥ ������ �׺�޽� ������ �ƴϴ���, ���� ����� ��ȿ �������� ����
    void SetSafeDestination(Vector3 desired)
    {
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, agent.areaMask))
        {
            // ��� ��ȿ�� �˻�
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
            }
            else
            {
                // ��ΰ� �����: ��ǥ ��ó�� �ٸ� ��ȿ ���� Ž�� (�ݰ� Ȯ�� ��)
                if (NavMesh.SamplePosition(desired, out NavMeshHit nearHit, 20f, agent.areaMask))
                {
                    if (agent.CalculatePath(nearHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                        agent.SetPath(path);
                }
            }
        }
        // SamplePosition ���� ��: ���� ��ġ ���� Ȥ�� ��ü �ൿ(���� ��)
    }
}
