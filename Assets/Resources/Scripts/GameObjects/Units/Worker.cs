using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : MonoBehaviour, ISelectable
{
    Transform ISelectable.Transform
    {
        get { return this.transform; }
    }

    // SelectionType 구현
    SelectionType ISelectable.SelectionType
    {
        get { return SelectionType.Worker; }
    }
    Bounds ISelectable.SelectionBounds
    {
        get
        {
            var renderer = GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.one);
        }
    }

    void ISelectable.OnSelected()
    {
        Debug.Log($"{name} 선택됨");
        // TODO: 아웃라인 표시 or UI 갱신
    }

    void ISelectable.OnDeselected()
    {
        Debug.Log($"{name} 선택 해제");
        // TODO: 아웃라인 제거 or UI 닫기
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
