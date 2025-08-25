using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class SystemBase : MonoBehaviour
{

    public bool IsActive { get; private set; }

    // 매니저 연결 여부와 무관하게 이벤트 제공
    public event Action<SystemBase> OnSystemStarted;
    public event Action<SystemBase> OnSystemStopped;


    public virtual void OnSystem()
    {
        IsActive = true;
        OnSystemStarted?.Invoke(this); // 매니저 또는 외부 리스너에게 알림
    }

    public virtual void OffSystem()
    {
        IsActive = false;
        OnSystemStopped?.Invoke(this);
    }
}
