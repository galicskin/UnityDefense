using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SelectionType
{
    None,
    Worker,     // 광부, 일반 유닛 → 대부분의 명령 실행 가능 -> 링이생김
    Building,   // 건물 → 이동은 불가, 생산/특수 명령 가능 -> 링이생김
    Special     // 특수 오브젝트(예: 자원 노드, 퀘스트용 구조물, 상호작용 대상) -> 명령없이 빛남.
}

public interface ISelectable
{
    Transform Transform { get; }
    SelectionType SelectionType { get; } // Unit/Building/Resource
    Bounds SelectionBounds { get; }
    Renderer SelectionRenderer { get; }
    void OnSelected(); void OnDeselected();
}