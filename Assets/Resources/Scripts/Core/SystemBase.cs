using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class SystemBase : MonoBehaviour
{

    public bool IsActive { get; private set; }

    // �Ŵ��� ���� ���ο� �����ϰ� �̺�Ʈ ����
    public event Action<SystemBase> OnSystemStarted;
    public event Action<SystemBase> OnSystemStopped;


    public virtual void OnSystem()
    {
        IsActive = true;
        OnSystemStarted?.Invoke(this); // �Ŵ��� �Ǵ� �ܺ� �����ʿ��� �˸�
    }

    public virtual void OffSystem()
    {
        IsActive = false;
        OnSystemStopped?.Invoke(this);
    }
}
