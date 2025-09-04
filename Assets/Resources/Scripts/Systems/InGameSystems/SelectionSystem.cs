using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class SelectionSystem : MonoBehaviour
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
    [SerializeField, Min(0.05f)] private float projectorDepth = 0.5f;

    private ISelectable current;
    private DecalProjector sharedProjector; // 공용 1개

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
            Debug.LogError("[SelectionSystem] selectionDecalPrefab 이 비었습니다. DecalProjector 프리팹을 할당하세요.");
            return;
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

        // 선택 대상의 자식으로 붙이고 로컬 트랜스폼 초기화
        var t = sharedProjector.transform;
        t.SetParent(target.Transform, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);
        t.localScale = Vector3.one; // 비균일 스케일 부모일 경우 왜곡될 수 있음(가능하면 앵커 빈 오브젝트 권장)

        // (선택) 타입별 반경/색 분기 원하면 여기서 sharedProjector.size/opacity/material 색을 조정
        sharedProjector.enabled = true;
    }

    private void HideRing()
    {
        if (sharedProjector) sharedProjector.enabled = false;
    }
}