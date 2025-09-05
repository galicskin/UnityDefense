using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public abstract class TowerBase : PooledObject
{
    public abstract int Id { get; }
    public bool isWork = false;
    [SerializeField] private GameObject previewObjectRed; // 인스펙터에 보이고 저장됨

    public GameObject PreviewObjectRed   // 외부에서 읽기 가능
    {
        get => previewObjectRed;
        protected set => previewObjectRed = value; // 자신/상속자에서만 세팅 가능
    }

    [SerializeField] private GameObject previewObjectBlue; // 인스펙터에 보이고 저장됨

    public GameObject PreviewObjectBlue   // 외부에서 읽기 가능
    {
        get => previewObjectBlue;
        protected set => previewObjectBlue = value; // 자신/상속자에서만 세팅 가능
    }

    

    public virtual void Update()
    {
        if (!isWork)
            return;

    }

    public void SetPreviewObject(GameObject _previewObjectBlue, GameObject _previewObjectRed)
    {
        PreviewObjectBlue = _previewObjectBlue;
        PreviewObjectRed = _previewObjectRed;
    }

    // 추후 업데이트 또는 막 이것저것 진행해서 Preiew가 설정이 안될때마다 아래 버튼을 눌러 갱신하는 방식은 
    // 타워가 많아질수록 손해보게됨 에디터에서 따로 일괄적용하는 코드 또는 자체 타워가 내부적으로 알아서 확인하는 코드같은게 필요.
#if UNITY_EDITOR
    // 에디터 버튼에서 호출될 때 프리뷰 프리팹 생성용 헬퍼를 내부에서 래핑(선택사항)
    [ContextMenu("Create Preview Prefabs (Red & Blue)")]
    private void __CreatePreviewPrefabs_ContextMenu()
    {
        TowerPreviewGenerator.CreatePreviewPrefabs(this.gameObject);
    }
#endif

}
