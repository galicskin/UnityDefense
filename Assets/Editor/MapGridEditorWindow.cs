// Assets/YourFolder/Editor/MapGridEditorWindow.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MapGridEditorWindow : EditorWindow
{
    // ===== 타겟 & 직렬화 객체 =====
    MapData _data;
    SerializedObject _so;
    SerializedProperty _propWidth, _propHeight, _propMapBlockData, _propMapProp;

    // 크기 접근자 (null-safe)
    int W => Mathf.Max(1, _data?.width ?? 1);
    int H => Mathf.Max(1, _data?.height ?? 1);

    // 스크롤
    Vector2 _scroll;

    // 셀 크기(아이콘)
    const float CELL_W = 40f;
    const float CELL_H = 40f;
    const float GAP = 4f;

    // 헤더
    const float X_TITLE_H = 16f;
    const float X_INDEX_H = 16f;
    const float Y_TITLE_W = 70f;
    const float Y_INDEX_W = 22f;
    float HeaderLeft => Y_TITLE_W + Y_INDEX_W;
    float HeaderTop => X_TITLE_H + X_INDEX_H;

    // 스타일
    GUIStyle _wrapBoldMini;
    GUIStyle _emptyText;

    // 브러시/페인트
    string _brushId = "";
    int _brushIndex = 0;
    bool _isPainting = false;
    bool _eraseMode = false;
    HashSet<Vector2Int> _paintedThisDrag = new HashSet<Vector2Int>();
    Vector2Int _hover = new Vector2Int(-1, -1);

    // 옵션 캐시: 값(id) / 라벨(displayName)
    string[] _idOptions = new string[1] { "" };
    string[] _labelOptions = new string[1] { "(empty)" };
    Dictionary<string, int> _id2Index = new Dictionary<string, int>();

    // 아이콘 캐시
    Dictionary<string, Sprite> _iconById = new Dictionary<string, Sprite>();

    [MenuItem("Tools/Map Grid Editor")]
    static void OpenEmpty() => GetWindow<MapGridEditorWindow>("Map Grid Editor").Show();

    public static void Open(MapData data)
    {
        var win = GetWindow<MapGridEditorWindow>("Map Grid Editor");
        win.SetTarget(data);
        win.Show();
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    void OnPlayModeChanged(PlayModeStateChange st)
    {
        // 플레이 전환에도 참조/바인딩 갱신
        if (_data != null) SetTarget(_data);
    }

    void SetTarget(MapData data)
    {
        _data = data;

        if (_data == null)
        {
            _so = null;
            _propWidth = _propHeight = _propMapBlockData = _propMapProp = null;
            return;
        }

        _so = new SerializedObject(_data);
        _propWidth = _so.FindProperty("width");
        _propHeight = _so.FindProperty("height");
        _propMapBlockData = _so.FindProperty("mapBlockData");
        _propMapProp = _so.FindProperty("MapProp");

        EnsureSize();                      // 그리드 크기 보정
        RebuildOptionsAndIcons(true);      // 브러시 옵션/아이콘 재구성
        Repaint();
    }

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            var newData = (MapData)EditorGUILayout.ObjectField("MapData", _data, typeof(MapData), false);
            if (newData != _data) SetTarget(newData);
            if (GUILayout.Button("Reload", GUILayout.Width(70))) SetTarget(_data);
        }

        if (_data == null || _so == null)
        {
            EditorGUILayout.HelpBox("Assign a MapData asset.", MessageType.Info);
            return;
        }

        _so.Update(); // ★ 항상 Update

        // width/height/mapBlockData를 SerializedProperty로 그리기
        EditorGUILayout.PropertyField(_propWidth);
        EditorGUILayout.PropertyField(_propHeight);
        EditorGUILayout.PropertyField(_propMapBlockData);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Size"))
            {
                // 값 보정 후 적용
                _propWidth.intValue = Mathf.Max(1, _propWidth.intValue);
                _propHeight.intValue = Mathf.Max(1, _propHeight.intValue);

                _so.ApplyModifiedProperties();
                Undo.RecordObject(_data, "Resize Map");
                EnsureSize();
                EditorUtility.SetDirty(_data);
                Repaint();
            }

            if (GUILayout.Button("Clear All"))
            {
                Undo.RecordObject(_data, "Clear Map");
                ClearAll();
                EditorUtility.SetDirty(_data);
                _so.ApplyModifiedProperties();
                Repaint();
            }

            if (GUILayout.Button("Save"))
            {
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_data);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Grid   X(width →),  Y(height ↓)   ({W} × {H})", EditorStyles.boldLabel);

        if (_wrapBoldMini == null)
            _wrapBoldMini = new GUIStyle(EditorStyles.miniBoldLabel) { wordWrap = true, alignment = TextAnchor.UpperLeft };
        if (_emptyText == null)
            _emptyText = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter };

        // 브러시 UI
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Brush", GUILayout.Width(50));

            int newIdx = EditorGUILayout.Popup(_brushIndex, _labelOptions, GUILayout.MaxWidth(260));
            if (newIdx != _brushIndex)
            {
                _brushIndex = newIdx;
                _brushId = IndexToId(_brushIndex);
            }

            _eraseMode = GUILayout.Toggle(_eraseMode, "Eraser", "Button", GUILayout.Width(70));
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("Left-click: choose | Alt+Right: pick | Right-drag: paint  (Eraser ON → erase)", MessageType.None);
        }

        // 스크롤 및 뷰 렉트
        float gridW = W * (CELL_W + GAP);
        float gridH = H * (CELL_H + GAP);
        float contentW = HeaderLeft + gridW;
        float contentH = HeaderTop + gridH;

        Rect viewRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (viewRect.width < 50) viewRect.width = 50;
        if (viewRect.height < 50) viewRect.height = 50;

        _scroll = GUI.BeginScrollView(viewRect, _scroll, new Rect(0, 0, contentW, contentH));

        // 가시 범위
        VisibleRangeX(_scroll.x, viewRect.width, out int xStart, out int xEnd);
        VisibleRangeY(_scroll.y, viewRect.height, out int yStart, out int yEnd);

        // 헤더
        GUI.Label(new Rect(HeaderLeft, 0, gridW, X_TITLE_H), "X (width →)", EditorStyles.miniBoldLabel);
        for (int x = xStart; x <= xEnd; x++)
        {
            float cx = HeaderLeft + x * (CELL_W + GAP);
            GUI.Label(new Rect(cx, X_TITLE_H, CELL_W, X_INDEX_H), x.ToString(), EditorStyles.miniLabel);
        }

        GUI.Label(new Rect(0, HeaderTop, Y_TITLE_W, gridH), "Y\n(height ↓)", _wrapBoldMini);
        for (int y = yStart; y <= yEnd; y++)
        {
            float cy = HeaderTop + y * (CELL_H + GAP);
            GUI.Label(new Rect(Y_TITLE_W, cy, Y_INDEX_W, CELL_H), y.ToString(), EditorStyles.miniLabel);
        }

        // ====== 셀 그리기 ======
        for (int y = yStart; y <= yEnd; y++)
        {
            var row = _data.MapProp[y]; if (row == null) continue;
            for (int x = xStart; x <= xEnd; x++)
            {
                float cx = HeaderLeft + x * (CELL_W + GAP);
                float cy = HeaderTop + y * (CELL_H + GAP);
                var r = new Rect(cx, cy, CELL_W, CELL_H);

                string cellId = row.cells[x] ?? "";

                // 배경
                EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.15f));

                // 아이콘/텍스트
                if (_iconById.TryGetValue(cellId, out var sp) && sp != null)
                    DrawSprite(r, sp);
                else
                    GUI.Label(r, string.IsNullOrEmpty(cellId) ? "" : GetLabelForId(cellId), _emptyText);

                // 호버 강조
                if (_hover.x == x && _hover.y == y)
                    EditorGUI.DrawRect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(0.2f, 0.5f, 1f, 0.25f));
            }
        }

        // ====== 입력 처리 ======
        HandleMouseInput(xStart, xEnd, yStart, yEnd);

        GUI.EndScrollView();

        // 마지막에 Apply
        _so.ApplyModifiedProperties();
    }

    // ---------- 유틸들 ----------

    void DrawSprite(Rect dst, Sprite sp)
    {
        if (sp == null || sp.texture == null) return;
        var tex = sp.texture;
        var tr = sp.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);

        var pad = 2f;
        var rd = new Rect(dst.x + pad, dst.y + pad, dst.width - pad * 2f, dst.height - pad * 2f);
        GUI.DrawTextureWithTexCoords(rd, tex, uv, true);
    }

    void HandleMouseInput(int xStart, int xEnd, int yStart, int yEnd)
    {
        var e = Event.current;

        if (GUIUtility.hotControl != 0 || EditorGUIUtility.editingTextField) return;

        Vector2 mp = e.mousePosition;

        // 호버 계산
        _hover = new Vector2Int(-1, -1);
        for (int y = yStart; y <= yEnd; y++)
        {
            for (int x = xStart; x <= xEnd; x++)
            {
                var rr = new Rect(HeaderLeft + x * (CELL_W + GAP), HeaderTop + y * (CELL_H + GAP), CELL_W, CELL_H);
                if (rr.Contains(mp)) { _hover = new Vector2Int(x, y); break; }
            }
            if (_hover.x != -1) break;
        }

        // Left Click: 메뉴
        if (e.type == EventType.MouseDown && e.button == 0 && _hover.x != -1)
        {
            ShowCellMenuAtMouse(_hover.x, _hover.y);
            e.Use();
            return;
        }

        // Alt + Right: 픽
        if (e.type == EventType.MouseDown && e.alt && e.button == 1 && _hover.x != -1)
        {
            string id = _data.MapProp[_hover.y].cells[_hover.x] ?? "";
            EnsureIdInCache(id, GetLabelForId(id));
            _brushId = id;
            _brushIndex = _id2Index[_brushId];
            e.Use();
            return;
        }

        // Right: 드래그 페인트
        if (e.type == EventType.MouseDown && e.button == 1 && _hover.x != -1)
        {
            _isPainting = true;
            _paintedThisDrag.Clear();
            PaintCell(_hover, erase: _eraseMode);
            e.Use();
        }

        if (_isPainting && e.type == EventType.MouseDrag)
        {
            _hover = new Vector2Int(-1, -1);
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    var rr = new Rect(HeaderLeft + x * (CELL_W + GAP), HeaderTop + y * (CELL_H + GAP), CELL_W, CELL_H);
                    if (rr.Contains(e.mousePosition)) { _hover = new Vector2Int(x, y); break; }
                }
                if (_hover.x != -1) break;
            }

            if (_hover.x != -1)
                PaintCell(_hover, erase: _eraseMode);

            e.Use();
        }

        if (_isPainting && (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow))
        {
            _isPainting = false;
            _paintedThisDrag.Clear();
            // 그리드 변경 후 저장 표시
            EditorUtility.SetDirty(_data);
            _so.ApplyModifiedProperties();
            e.Use();
        }
    }

    void ShowCellMenuAtMouse(int x, int y)
    {
        var menu = new GenericMenu();

        bool isEmpty = string.IsNullOrEmpty(_data.MapProp[y].cells[x]);
        menu.AddItem(new GUIContent("(empty)"), isEmpty, () =>
        {
            Undo.RecordObject(_data, "Paint Cell");
            _data.MapProp[y].cells[x] = "";
            EditorUtility.SetDirty(_data);
            Repaint();
        });

        for (int i = 1; i < _idOptions.Length; i++)
        {
            string id = _idOptions[i];
            string label = _labelOptions[i];
            bool on = (_data.MapProp[y].cells[x] == id);

            int cx = x, cy = y, ci = i;
            menu.AddItem(new GUIContent(label), on, () =>
            {
                Undo.RecordObject(_data, "Paint Cell");
                _data.MapProp[cy].cells[cx] = _idOptions[ci];
                EditorUtility.SetDirty(_data);
                Repaint();
            });
        }

        menu.ShowAsContext();
        Event.current.Use();
    }

    void PaintCell(in Vector2Int cell, bool erase)
    {
        if (cell.x < 0 || cell.x >= W || cell.y < 0 || cell.y >= H) return;
        if (_paintedThisDrag.Contains(cell)) return;
        _paintedThisDrag.Add(cell);

        var row = _data.MapProp[cell.y];
        string val = erase ? "" : (_brushId ?? "");
        EnsureIdInCache(val, GetLabelForId(val));

        if (row.cells[cell.x] != val)
        {
            Undo.RecordObject(_data, "Paint Cell");
            row.cells[cell.x] = val;
            EditorUtility.SetDirty(_data);
        }
    }

    string GetLabelForId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "(empty)";
        if (_id2Index.TryGetValue(id, out int idx)) return _labelOptions[idx];
        return id;
    }

    void RebuildOptionsAndIcons(bool scanExistingCells)
    {
        var idList = new List<string>() { "" };
        var labelList = new List<string>() { "(empty)" };
        var set = new HashSet<string>() { "" };

        _iconById.Clear();

        // mapBlockData → BlockProperties(id, displayName, icon)에서 옵션 구성
        if (_data?.mapBlockData?.BlockProperties != null)
        {
            foreach (var bp in _data.mapBlockData.BlockProperties)
            {
                var id = bp.id;
                if (string.IsNullOrEmpty(id) || set.Contains(id)) continue;

                set.Add(id);
                idList.Add(id);
                labelList.Add(string.IsNullOrEmpty(bp.displayName) ? id : bp.displayName);

                if (bp.icon != null)
                    _iconById[id] = bp.icon;
            }
        }

        // 현재 그리드에만 존재하는 id도 옵션에 포함(아이콘은 없음)
        if (scanExistingCells && _data?.MapProp != null)
        {
            for (int y = 0; y < _data.MapProp.Count; y++)
            {
                var row = _data.MapProp[y]; if (row == null) continue;
                for (int x = 0; x < row.cells.Count; x++)
                {
                    var id = row.cells[x];
                    if (string.IsNullOrEmpty(id) || set.Contains(id)) continue;
                    set.Add(id);
                    idList.Add(id);
                    labelList.Add(id);
                }
            }
        }

        _idOptions = idList.ToArray();
        _labelOptions = labelList.ToArray();

        _id2Index.Clear();
        for (int i = 0; i < _idOptions.Length; i++)
            _id2Index[_idOptions[i]] = i;

        EnsureIdInCache(_brushId, GetLabelForId(_brushId));
        _brushIndex = _id2Index[_brushId];
    }

    void EnsureIdInCache(string id, string label)
    {
        id = id ?? "";
        if (_id2Index.ContainsKey(id)) return;

        var ids = new List<string>(_idOptions) { id };
        var lbs = new List<string>(_labelOptions) { string.IsNullOrEmpty(label) ? "(empty)" : label };
        _idOptions = ids.ToArray();
        _labelOptions = lbs.ToArray();
        _id2Index[id] = _idOptions.Length - 1;
    }

    string IndexToId(int idx)
    {
        if (_idOptions == null || idx < 0 || idx >= _idOptions.Length) return "";
        return _idOptions[idx];
    }

    void VisibleRangeX(float scrollX, float viewW, out int start, out int end)
    {
        float left = Mathf.Max(0, scrollX - HeaderLeft);
        float right = scrollX + viewW - HeaderLeft;

        int first = Mathf.FloorToInt(left / (CELL_W + GAP));
        int last = Mathf.FloorToInt(Mathf.Ceil(right / (CELL_W + GAP))) - 1;

        start = Mathf.Clamp(first, 0, W - 1);
        end = Mathf.Clamp(last, 0, W - 1);
        if (end < start) end = start;
    }

    void VisibleRangeY(float scrollY, float viewH, out int start, out int end)
    {
        float top = Mathf.Max(0, scrollY - HeaderTop);
        float bottom = scrollY + viewH - HeaderTop;

        int first = Mathf.FloorToInt(top / (CELL_H + GAP));
        int last = Mathf.FloorToInt(Mathf.Ceil(bottom / (CELL_H + GAP))) - 1;

        start = Mathf.Clamp(first, 0, H - 1);
        end = Mathf.Clamp(last, 0, H - 1);
        if (end < start) end = start;
    }

    // 리스트 크기 보정 (직접 데이터에 작업하되, 직전 Undo/Dirty 처리)
    void EnsureSize()
    {
        if (_data.MapProp == null) _data.MapProp = new List<MapRow>();

        // 행 수 보정
        while (_data.MapProp.Count < H) _data.MapProp.Add(new MapRow());
        while (_data.MapProp.Count > H) _data.MapProp.RemoveAt(_data.MapProp.Count - 1);

        // 각 행의 열 수 보정
        for (int y = 0; y < H; y++)
        {
            var row = _data.MapProp[y];
            if (row == null) { row = new MapRow(); _data.MapProp[y] = row; }

            while (row.cells.Count < W) row.cells.Add(string.Empty);
            while (row.cells.Count > W) row.cells.RemoveAt(row.cells.Count - 1);
        }
    }

    void ClearAll()
    {
        EnsureSize();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                _data.MapProp[y].cells[x] = string.Empty;
    }
}
