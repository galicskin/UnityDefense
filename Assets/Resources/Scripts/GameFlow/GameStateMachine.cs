using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum GameState
{
    Boot, // 만들어지는 단계
    Loading, // 로딩중(게임 씬으로 넘어갈떄 생기는 로딩문제 해결)
    MainMenu, // 메인메뉴
    Playing, // 게임 진행중
    Paused, // 일시정지
    GameOver, // 게임 패배
    Victory // 게임 승리
}

public class GameStateMachine : MonoBehaviour
{
    public GameState State { get; private set; }
    public event Action<GameState, GameState> OnChanged; // (prev, next)
    public GameStateMachine(GameState initial = GameState.Boot)
    {
        State = initial;
    }

    public bool CanSet(GameState next)
    {
        // 전이 규칙을 여기서 정의 (필요 시 확장)
        return State switch
        {
            GameState.Boot => next == GameState.MainMenu || next == GameState.Loading,
            GameState.MainMenu => next == GameState.Loading,
            GameState.Loading => next == GameState.Playing,
            GameState.Playing => next == GameState.Paused || next == GameState.GameOver || next == GameState.Victory,
            GameState.Paused => next == GameState.Playing || next == GameState.MainMenu,
            GameState.GameOver => next == GameState.MainMenu,
            GameState.Victory => next == GameState.MainMenu,
            _ => throw new InvalidOperationException($"Unhandled state: {State}"),
        };
    }

    public void TryChangeState(GameState next)
    {
        if (State == next) return;
        if (!CanSet(next)) return;  // 전이 규칙 위반 → 그대로 반환

        var prev = State;
        State = next;

        OnChanged?.Invoke(prev, State);

    }
}
