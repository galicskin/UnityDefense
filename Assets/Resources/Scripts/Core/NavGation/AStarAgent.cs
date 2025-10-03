using System.Collections.Generic;
using UnityEngine;

public class AStarAgent : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveEpsilon = 0.05f;

    [Header("Grid -> World")]
    [SerializeField] private float tileSize = 1f;        // 타일 한 변 길이
    [SerializeField] private Vector3 worldOrigin = Vector3.zero; // (0,0) 타일의 월드 기준점
    [SerializeField] private float groundY = 0f;         // 이동 높이(Y)

    // 외부에서 주입
    private byte[,] mapData;

    // 내부 상태
    private readonly List<GridPos> _path = new();
    private int _pathIndex = -1;
    private bool _hasPath = false;

    /// <summary>맵/좌표 설정을 주입한다.</summary>
    public void Initialize(byte[,] mapData, float tileSize, Vector3 worldOrigin, float groundY = 0f)
    {
        this.mapData = mapData;
        this.tileSize = tileSize;
        this.worldOrigin = worldOrigin;
        this.groundY = groundY;
    }

    /// <summary>월드 좌표 목적지 지정(타일로 스냅 후 경로계산)</summary>
    public bool SetDestinationWorld(Vector3 worldPos)
    {
        if (mapData == null) return false;
        var goal = WorldToGrid(worldPos);
        return SetDestination(goal);
    }

    /// <summary>타일 좌표 목적지 지정</summary>
    public bool SetDestination(GridPos goal)
    {
        if (mapData == null) return false;

        var start = WorldToGrid(transform.position);

        if (!AStar4.FindPath(mapData, start, goal, out var path))
        {
            _hasPath = false;
            _path.Clear();
            _pathIndex = -1;
            return false;
        }

        _path.Clear();
        _path.AddRange(path);
        _pathIndex = 0;
        _hasPath = _path.Count > 0;
        return _hasPath;
    }

    private void Update()
    {
        if (!_hasPath || _pathIndex < 0 || _pathIndex >= _path.Count) return;

        Vector3 target = GridToWorld(_path[_pathIndex]);
        Vector3 pos = transform.position;

        // 같은 높이로 이동
        target.y = groundY;
        pos.y = groundY;

        Vector3 dir = (target - pos);
        float dist = dir.magnitude;

        if (dist <= arriveEpsilon)
        {
            _pathIndex++;
            if (_pathIndex >= _path.Count)
            {
                _hasPath = false; // 도착
            }
            return;
        }

        dir /= dist;
        
        transform.position += dir * (moveSpeed * Time.deltaTime);
    }

    // ---- 좌표 변환 ----
    public GridPos WorldToGrid(Vector3 world)
    {
        // 타일 중심 스냅
        int gx = Mathf.FloorToInt((world.x - worldOrigin.x) / tileSize);
        int gy = Mathf.FloorToInt((world.z - worldOrigin.z) / tileSize);
        return new GridPos(gx, gy);
    }

    public Vector3 GridToWorld(GridPos g)
    {
        float x = worldOrigin.x + (g.x + 0.5f) * tileSize;
        float z = worldOrigin.z + (g.y + 0.5f) * tileSize;
        return new Vector3(x, groundY, z);
    }
}
