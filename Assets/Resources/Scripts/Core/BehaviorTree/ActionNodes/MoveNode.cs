using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviourTreeKit
{ 
    public class MoveNode : ActionNode
    {

        private Vector3 Destination;
        private Vector3 _LastDestination;

        public override void BuildBlackboardKeys()
        {
            AddKeyIfNotExists(nameof(Destination), BlackboardKey.ValueType.Vector3);
        }

        public override BTState Tick(BTContext ctx)
        {
            if (ctx?.owner == null || ctx.blackboard == null)
            {
                Debug.Log($"ctx?.owner == null || ctx.blackboard == null : {ctx?.owner == null || ctx.blackboard == null}");
                return BTState.Failure;
            }

            if (!TryGetKey(nameof(Destination), out var bbKey))
            {
                Debug.Log($"blackboardKey 가져오기 실패");
                return BTState.Failure;
            }

            string blackboardKey = bbKey.key;

            if (!ctx.blackboard.TryGet(blackboardKey, out Vector3 dest))
            {
                Debug.Log($"{blackboardKey} : Destination 가져오기 실패");
                return BTState.Failure;
            }

            Destination = dest;
            IMoveable move = ctx.GetService<IMoveable>();
            var agent = move.navAgent; // 필요시 move.MoveTo(...)로 캡슐화 추천
            if (agent == null || !agent.isOnNavMesh)
            {
                Debug.Log($"agent 없음, 또는 isOnNavMesh 가 아님");
                return BTState.Failure;
            }


            // 목적지가 바뀌었거나, 경로가 없거나, 막혔으면 SetDestination
            bool needSet =
                !Approximately(_LastDestination, Destination) ||
                !agent.hasPath ||
                agent.pathStatus == NavMeshPathStatus.PathInvalid;

            if (needSet)
            {
                agent.SetDestination(Destination);
                _LastDestination = Destination;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.ResetPath(); // 선택
                return BTState.Success;
            }


            return BTState.Running;
        }


        private static bool Approximately(in Vector3 a, in Vector3 b, float eps = 0.0001f)
            => (a - b).sqrMagnitude <= eps * eps;
    }

}