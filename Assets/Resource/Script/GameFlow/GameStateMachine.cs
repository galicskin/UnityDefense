using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum GameState
{
    Boot, // ��������� �ܰ�
    Loading, // �ε���(���� ������ �Ѿ�� ����� �ε����� �ذ�)
    MainMenu, // ���θ޴�
    Playing, // ���� ������
    Paused, // �Ͻ�����
    GameOver, // ���� �й�
    Victory // ���� �¸�
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
        // ���� ��Ģ�� ���⼭ ���� (�ʿ� �� Ȯ��)
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
        if (!CanSet(next)) return;  // ���� ��Ģ ���� �� �״�� ��ȯ

        var prev = State;
        State = next;

        OnChanged?.Invoke(prev, State);

    }
}
