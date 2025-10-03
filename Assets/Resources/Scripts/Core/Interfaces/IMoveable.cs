using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public interface IMoveable
{
    NavMeshAgent navAgent { get; }
    AStarAgent aStarAgent { get; }
}
