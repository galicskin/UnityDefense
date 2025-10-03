using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class SelectionSystem : SystemBase
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float raycastMaxDistance = 500f;

    [Header("Shared Decal Prefab (미리 세팅된 프리팹)")]
    [Tooltip("DecalProjector 컴포넌트가 붙어 있고, BaseMap(링 텍스처)과 머티리얼이 세팅된 프리팹")]
    [SerializeField] private GameObject selectionDecalPrefab;

    [Header("Ring Size Override (선택)")]
    [SerializeField] private bool overrideSize = false;
    [SerializeField, Min(0.1f)] private float ringRadius = 0.8f;    // 미터 기준
    [SerializeField, Min(0.05f)] private float projectorDepth = 1.0f;

    [Header("HighLight Color")]
    [SerializeField] Color HighlightColor = new Color(0f,125f,0f,0.5f);
    private ISelectable current;
    private PooledBehaviour<DecalProjector> sharedProjectorPool; // 공용 1개에서 여러개로...
    private Dictionary<ISelectable, DecalProjector> selectedRings;
    private Dictionary<ISelectable, bool> selectedBools;


    [Header("선택 링 크기 조절")]
    [SerializeField] float autoPaddingPercent = 0.5f;
    //[SerializeField, Min(0.05f)] private float projectorDepthPercent = 0.3f;

    enum SelectMode
    {
        Single = 0,
        Multiple = 1,

    }
    SelectMode selectMode = SelectMode.Single;
    SelectionType selectableTypes = SelectionType.Building | SelectionType.Worker;

    public void SetSingleMode()
    {
        //Debug.Log("SetSingleMode");
        selectMode = SelectMode.Single;
        ClearSelection();
    }

    public void SetMultipleMode()
    {
        //Debug.Log("SetMultipleMode");
        selectMode = SelectMode.Multiple;
        ClearSelection();
    }

    public void SetSelectableTypes(SelectionType selectionTypes)
    {
        selectableTypes = selectionTypes;
        ClearSelection();
    }

    private void Awake()
    {
        if (!mainCamera) mainCamera = Camera.main;
        selectedRings = new Dictionary<ISelectable, DecalProjector>();
        selectedBools = new Dictionary<ISelectable, bool>();
        PrewarmSharedProjector();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            switch (selectMode)
            {
                case SelectMode.Single:
                    ClickSelectSingle(Input.mousePosition); // 단일 교체 선택
                    break;

                case SelectMode.Multiple:
                    ClickSelectMultiple(Input.mousePosition); // ← 새 함수
                    break;
            }
        }

        // (단일 전용 파괴 감시가 필요하면 유지)
        if (current is Object u && u == null)
            ClearSelection();
    }

    // ───────────────────────────────WorkerMode────────────────────────────────────────
    private void ClickSelectSingle(Vector2 screenPos)
    {
        var ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, raycastMaxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            var target = hit.transform.GetComponentInParent<ISelectable>();
            if (target != null && (target.SelectionType & selectableTypes) != 0)
            {
                SetSelection(target); 
                return; 
            }
        }
        ClearSelection();
    }

    private void SetSelection(ISelectable next)
    {
        if (current == next) return;

        if (current != null)
        { 
            if(TryHideHighlight(current))
                current.OnDeselected();
        }

        current = next;

        if (current != null)
        {
            if(TryHighligtSelect(current))
                current.OnSelected();
        }
        else
        {
            ClearSelection();
        }
    }

    // ───────────────────────────────MineMode────────────────────────────────────────
    private void ClickSelectMultiple(Vector2 screenPos)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        var ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, raycastMaxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            var target = hit.transform.GetComponentInParent<ISelectable>();
            if (target != null && (target.SelectionType & selectableTypes) != 0)
            {
                if (ctrl)
                {
                    ToggleOne(target);
                }
                else if (shift)
                {
                    SelectOne(target); // 추가
                }
                else
                {
                    Debug.Log("교체");
                    ClearAllSelectable();
                    SelectOne(target);
                }
                return;
            }
        }

        // 빈 공간 클릭
        if (!shift && !ctrl)
            ClearAllSelectable(); // 교체 정책일 때만
    }
    private void SelectOne(ISelectable s)
    {
        if (s == null) return;

        if(TryHighligtSelect(s)) // 내부에서 selectedObjects[s] = projector; 처리됨
            s.OnSelected();
    }

    private void DeselectOne(ISelectable s)
    {
        if (s == null) return;
        if(TryHideHighlight(s))
            s.OnDeselected();
    }

    private void ToggleOne(ISelectable s)
    {
        if (s == null) return;

        if (TryHighligtSelect(s))
            s.OnSelected();
        else if (TryHideHighlight(s))
            s.OnDeselected();
    }

    private void ClearSelection()
    {
        if (current != null)
            current.OnDeselected();
        current = null;

        foreach (var key in selectedRings.Keys)
        {
            if (selectedRings[key] == null)
                continue;
            key.OnDeselected();
        }
        foreach (var key in selectedBools.Keys)
        {
            if (selectedBools[key] == false)
                continue;
            key.OnDeselected();
        }
        ClearAllSelectable();
    }

    // ───────────────────── 공용 Projector 준비/부착/토글 ─────────────────────
    private void PrewarmSharedProjector()
    {

        if (!selectionDecalPrefab)
        {
            selectionDecalPrefab = Resources.Load<GameObject>("Prefabs/SelectDecal/SelectionDecal");
        }
        sharedProjectorPool = new PooledBehaviour<DecalProjector>(selectionDecalPrefab);

        sharedProjectorPool.InitSetting(
            (decalProjector) =>
            {
                var go = decalProjector.gameObject;
                go.name = "__SharedDecalRing";
                go.hideFlags = HideFlags.DontSave;

                DecalProjector sharedProjector = go.GetComponent<DecalProjector>();
                if (!sharedProjector)
                {
                    Debug.LogError("[SelectionSystem] 프리팹에 DecalProjector 가 없습니다.");
                    return;
                }

                // 아래로 향하게(-Z 투영 → 로컬 +X 90도 회전)
                sharedProjector.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                sharedProjector.enabled = false;

                if (overrideSize)
                {
                    var diameter = ringRadius * 2f;
                    sharedProjector.size = new Vector3(diameter, diameter, projectorDepth);
                }


            });

    }

    private bool TryHighligtSelect(ISelectable target)
    {
        //if (!sharedProjector) return;

        switch (target.SelectionType)
        {
           
            case SelectionType.Worker:
            case SelectionType.Building:
                if (selectedRings.ContainsKey(target) &&  selectedRings[target] != null) return false;

                DecalProjector sharedProjector = sharedProjectorPool.Spawn();
                selectedRings[target] = sharedProjector;

                var t = sharedProjector.transform;
                t.SetParent(target.Transform, true);               // 부모 영향은 유지하되, 이후 position은 월드로 세팅
                t.localRotation = Quaternion.Euler(90f, 0f, 0f);
                t.localScale = Vector3.one;

                Bounds sb = target.SelectionBounds;

                // ── 크기(X,Y): XZ 최대 치수로 원형 링 직경
                Vector3 sizeWS = sb.size;                          // SelectionBounds가 월드 기준이라고 가정
                float baseDiameter = Mathf.Max(sizeWS.x, sizeWS.z);
                float diameter = overrideSize ? ringRadius * 2f
                                              : baseDiameter * (1f + autoPaddingPercent);

                // ── 깊이(Z): 지면 요철 높이폭 기반 (최소 기존 값 유지)
                float depthLocal = Mathf.Max(projectorDepth, sizeWS.y + 0.02f);

                sharedProjector.size = new Vector3(diameter, diameter, depthLocal);

                // ── 위치: 월드로 '가장 높은 점'에 맞춤 → 상자가 아래로 파고들며 겹침
                t.position = new Vector3(sb.center.x, sb.center.y, sb.center.z);

                sharedProjector.enabled = true;

                return true;


            case SelectionType.Special:
                if (selectedBools.ContainsKey(target) && selectedBools[target] == true) return false;
                selectedBools[target] = true;

                var r = target.SelectionRenderer;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor(r.sharedMaterial && r.sharedMaterial.HasProperty("_BaseColor") ? Shader.PropertyToID("_BaseColor") : Shader.PropertyToID("_Color"), HighlightColor);
                r.SetPropertyBlock(mpb);
                // 해당 타겟의 material에 들어가서 texture base의 색깔을 연하게 하든 빛나게하든 하기.

                return true;
            default:
                return false;
        }

    }

    private bool TryHideHighlight(ISelectable selectable)
    {
        switch (selectable.SelectionType)
        {
            case SelectionType.Worker:
            case SelectionType.Building:
                if (!selectedRings.ContainsKey(selectable) || selectedRings[selectable] == null) return false; 
                var ring = selectedRings[selectable];
                sharedProjectorPool.Despawn(ring);
                selectedRings[selectable] = null;
                return true;
            case SelectionType.Special:
                if (!selectedBools.ContainsKey(selectable) || selectedBools[selectable] == false) return false;
                var r = selectable.SelectionRenderer;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.Clear();               // MPB 비우면 원래 머티리얼 값으로 돌아갑니다
                r.SetPropertyBlock(mpb);
                selectedBools[selectable] = false;
                return true ;

            default:
                return false;

        }
    }

    private void ClearAllSelectable()
    {
        foreach (var kv in selectedRings.ToArray()) // 스냅샷
        {
            if (kv.Value != null) sharedProjectorPool.Despawn(kv.Value);
            TryHideHighlight(kv.Key);
            selectedRings[kv.Key] = null; // 또는 Remove(kv.Key);
        }

        foreach (var key in selectedBools.Keys.ToArray()) // 스냅샷
        {
            TryHideHighlight(key);
            selectedBools[key] = false;
        }
    }

    public List<ISelectable> GetSelectedList()
    {
        if (selectableTypes == SelectionType.Special)
        {
            return selectedBools.Keys.ToList();
        }
        else if ((selectableTypes & (SelectionType.Worker | SelectionType.Building)) != 0 )
        {
            return selectedRings.Keys.ToList();
        }
        return null;
    }

}