using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapRow // 가로 줄
{
    public List<string> cells = new List<string>();
}

[CreateAssetMenu(menuName = "Scriptable Object/MapData", fileName = "MapData")]
public class MapData : ScriptableObject
{
    [Header("Grid")]
    [Min(1)] public int width = 8;
    [Min(1)] public int height = 8;

    // 사용되는 MapBlockData
    public MapBlockData mapBlockData;

    public List<MapRow> MapProp = new List<MapRow>();

    void OnEnable()
    {
        if (MapProp == null) MapProp = new List<MapRow>();
    }

    void OnValidate()
    {
        if (MapProp == null) MapProp = new List<MapRow>();
    }
    void EnsureSize()
    {
        // 행 수(height) 맞추기
        while (MapProp.Count < height)
            MapProp.Add(new MapRow());
        while (MapProp.Count > height)
            MapProp.RemoveAt(MapProp.Count - 1);

        // 각 행의 셀 수(width) 맞추기
        for (int y = 0; y < height; y++)
        {
            var row = MapProp[y];
            if (row == null) { row = new MapRow(); MapProp[y] = row; }

            while (row.cells.Count < width)
                row.cells.Add(string.Empty);
            while (row.cells.Count > width)
                row.cells.RemoveAt(row.cells.Count - 1);
        }
    }

    [Header("Unit")]

    // MapStartPoint = 0,0 block Location
    public Vector3 MapStartPoint = Vector3.zero;
    public float BlockUnit = MapSettings.bloackUnitSize;

}
