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
    // ������ ��ư���� ȣ��� �� ������ ������ ������ ���۸� ���ο��� ����(���û���)
    [ContextMenu("Create Preview Prefabs (Red & Blue)")]
    private void __CreatePreviewPrefabs_ContextMenu()
    {
        TowerPreviewGenerator.CreatePreviewPrefabs(this.gameObject);
    }
#endif

}
