using System.Collections;
using System.Collections.Generic;
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


    private ISelectable current;
    private DecalProjector sharedProjector; // 공용 1개

    [Header("선택 링 크기 조절")]
    [SerializeField] float autoPaddingPercent = 0.5f;
    //[SerializeField, Min(0.05f)] private float projectorDepthPercent = 0.3f;
    private void Awake()
    {
        if (!mainCamera) mainCamera = Camera.main;
        PrewarmSharedProjector();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySelectAt(Input.mousePosition);

        // 선택 대상이 파괴되면 자동 해제
        if (current is Object u && u == null)
            ClearSelection();
    }

    // ───────────────────────────────────────────────────────────────────────
    private void TrySelectAt(Vector2 screenPos)
    {
        var ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, raycastMaxDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            var target = hit.transform.GetComponentInParent<ISelectable>();
            if (target != null) { SetSelection(target); return; }
        }
        ClearSelection();
    }

    private void SetSelection(ISelectable next)
    {
        if (current == next) return;

        if (current != null)
            current.OnDeselected();

        current = next;

        if (current != null)
        {
            current.OnSelected();
            AttachRingTo(current);
        }
        else
        {
            HideRing();
        }
    }

    private void ClearSelection()
    {
        if (current != null)
            current.OnDeselected();

        current = null;
        HideRing();
    }

    // ───────────────────── 공용 Projector 준비/부착/토글 ─────────────────────
    private void PrewarmSharedProjector()
    {
        if (sharedProjector) return;

        if (!selectionDecalPrefab)
        {
            selectionDecalPrefab = Resources.Load<GameObject>("Prefabs/SelectDecal/SelectionDecal");

        }

        var go = Instantiate(selectionDecalPrefab);
        go.name = "__SharedDecalRing";
        go.hideFlags = HideFlags.DontSave;

        sharedProjector = go.GetComponent<DecalProjector>();
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
    }

    private void AttachRingTo(ISelectable target)
    {
        if (!sharedProjector) return;

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
    }

    private void HideRing()
    {
        if (sharedProjector) sharedProjector.enabled = false;
    }
}