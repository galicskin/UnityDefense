using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TowerBase : PooledObject
{
    public abstract int Id { get; }
    public bool isWork = false;
    public GameObject PreviewObjectRed;
    public GameObject PreviewObjectBlue;

    public virtual void Update()
    {
        if (!isWork)
            return;

    }

    
#if UNITY_EDITOR
    // 에디터 버튼에서 호출될 때 프리뷰 프리팹 생성용 헬퍼를 내부에서 래핑(선택사항)
    [ContextMenu("Create Preview Prefabs (Red & Blue)")]
    private void __CreatePreviewPrefabs_ContextMenu()
    {
        TowerPreviewGenerator.CreatePreviewPrefabs(this.gameObject);
    }
#endif

}
