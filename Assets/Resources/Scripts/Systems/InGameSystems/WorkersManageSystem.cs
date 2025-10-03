using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum WorkerState { Idle, Reserved, Busy, Dead }
public enum Faction { Player, Enemy, Neutral } // 필요 시 확장

[DefaultExecutionOrder(-100)]
public class WorkersManageSystem : SystemBase
{

    private readonly HashSet<Worker> _workers = new();
    private readonly HashSet<Worker> _idle = new();   // 상태별 뷰(빠른 쿼리용)
    private readonly HashSet<Worker> _busy = new();
    private readonly HashSet<Worker> _reserved = new();

    public event Action<Worker> OnWorkerAdded;
    public event Action<Worker> OnWorkerRemoved;
    public event Action<Worker, WorkerState> OnWorkerStateChanged;


    // ── 등록/해제 ─────────────────────────────────────────
    public void Register(Worker w)
    {
        if (w == null || _workers.Contains(w)) return;
        _workers.Add(w);
        SetStateInternal(w, w.State, invokeEvent: false); // 초기 상태 반영
        OnWorkerAdded?.Invoke(w);

    }

    public void Unregister(Worker w)
    {
        if (w == null || !_workers.Remove(w)) return;
        _idle.Remove(w); _reserved.Remove(w); _busy.Remove(w);
        OnWorkerRemoved?.Invoke(w);
    }

    // ── 상태 변경(공용 API) ───────────────────────────────
    public void SetState(Worker w, WorkerState s)
    {
        if (w == null || !_workers.Contains(w)) return;
        SetStateInternal(w, s, invokeEvent: true);
    }

    private void SetStateInternal(Worker w, WorkerState s, bool invokeEvent)
    {
        _idle.Remove(w); _reserved.Remove(w); _busy.Remove(w);
        switch (s)
        {
            case WorkerState.Idle: _idle.Add(w); break;
            case WorkerState.Reserved: _reserved.Add(w); break;
            case WorkerState.Busy: _busy.Add(w); break;
        }
        w.State = s;
        if (invokeEvent) OnWorkerStateChanged?.Invoke(w, s);
    }

    public Worker FindLeastBusyWorker()
    {
        // 1) Idle 상태 우선
        if (_idle.Count > 0)
            return _idle.First();

        // 2) Idle이 없으면 Reserved 중 하나
        if (_reserved.Count > 0)
            return _reserved.First();

        // 3) 모두 바쁘면 Busy 중 하나 (혹은 null 반환)
        if (_busy.Count > 0)
            return _busy.First();

        // 4) 아무도 없으면 null
        return null;
    }

}


