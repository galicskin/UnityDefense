using UnityEngine;
using System.Collections.Generic;

public class MapLoader : MonoBehaviour
{
    [Header("Fallback if no MapData is passed to LoadMapData")]
    public MapData defaultMapData;

    [Header("Spawn")]
    public Transform parent;                 // 생성할 부모 Transform
    public Vector2 cellSize = new Vector2(1, 1);  // 셀 간격(2D 기준)
    public bool originTopLeft_YDown = true;  // (0,0)이 좌상단이고 +y가 아래로 갈지 여부

    private MapData mapData;
    private Dictionary<string, BlockProp> blockById; // id -> BlockProp 캐시

    public void LoadMapData(MapData _mapData = null)
    {
        if (_mapData != null)
        {
            mapData = _mapData;
        }
        else if (defaultMapData != null)
        {
            mapData = defaultMapData;
        }
        else
        {
            // 주의: Resources 폴더 하위 경로여야 함 (예: Assets/Resources/Scripts/Map/MapData.asset)
            mapData = Resources.Load<MapData>("Scripts/Map/MapData");
        }

        if (mapData == null)
        {
            Debug.LogError("MapLoader: MapData not found.");
            return;
        }

        BuildLookup(); // id->BlockProp 캐시 구축
    }


    private void BuildLookup()
    {
        blockById = new Dictionary<string, BlockProp>();

        if (mapData.mapBlockData == null || mapData.mapBlockData.BlockProperties == null)
            return;

        foreach (var bp in mapData.mapBlockData.BlockProperties)
        {
            // BlockProp가 struct면 null 비교는 불필요하지만, id와 prefab 체크는 하자
            if (string.IsNullOrEmpty(bp.id)) continue;
            if (!blockById.ContainsKey(bp.id))
                blockById.Add(bp.id, bp);
        }
    }


    public void CreateMap()
    {
        if (mapData == null)
        {
            Debug.LogWarning("MapLoader: call LoadMapData first.");
            return;
        }
        if (mapData.MapProp == null)
        {
            Debug.LogWarning("MapLoader: MapData.MapProp is null.");
            return;
        }

        int width = Mathf.Max(1, mapData.width);
        int height = Mathf.Max(1, mapData.height);

        for (int y = 0; y < height; y++)
        {
            var row = (y < mapData.MapProp.Count) ? mapData.MapProp[y] : null;
            if (row == null) continue;

            for (int x = 0; x < width; x++)
            {
                string id = (x < row.cells.Count) ? row.cells[x] : string.Empty;
                if (string.IsNullOrEmpty(id)) continue; // 빈 칸은 스킵

                if (!blockById.TryGetValue(id, out var bp) || bp.prefab == null)
                {
                    Debug.LogWarning($"MapLoader: No BlockProp/prefab for id '{id}' at ({x},{y}).");
                    continue;
                }

                // 월드 좌표 계산 (2D 기준)
                Vector3 pos = originTopLeft_YDown
                    ? new Vector3(x * cellSize.x, -y * cellSize.y, 0f)  // 위에서 아래로 +y
                    : new Vector3(x * cellSize.x, y * cellSize.y, 0f); // 일반 좌하단 원점

                Instantiate(bp.prefab, pos, Quaternion.identity, parent);
            }
        }
    }


    public string GetCellId(int x, int y)
    {
        if (mapData == null || mapData.MapProp == null) return null;
        if (y < 0 || y >= mapData.MapProp.Count) return null;

        var row = mapData.MapProp[y];
        if (row == null || x < 0 || x >= row.cells.Count) return null;

        return row.cells[x];
    }
}
