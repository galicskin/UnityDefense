using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StateChangeRule
{
    // (prev, next) -> ���� ���� ��Ģ
    private readonly Dictionary<(GameState from, GameState to), Action> _transition = new();

    // ====== �ʱ� ���� ======
    public StateChangeRule()
    {
        RegisterCommonEnterExit();  // ���� Enter/Exit ��Ģ(���� �޼���) ����
        RegisterTransitions();      // ���̺�(����) ��Ģ ����
    }

    // ====== �ܺο��� ȣ���� �� �ϳ��� ������ ======
    public void Apply(GameState prev, GameState next)
    {
        ApplyExit(prev);                   // 1) ���� ���¿��� ��������
        ApplyTransition(prev, next);       // 2) ���� ���� ��Ģ
        ApplyEnter(next);                  // 3) �� ���� �ø���
    }

    // --------------------------------------------------------------------
    // 1) ���� Exit/Enter ��Ģ(���� ����) - �ʿ� �� ���ο��� ����
    // --------------------------------------------------------------------
    private void RegisterCommonEnterExit() { /* �ڸ�ǥ����: �ʿ� �� ���ҽ� �ε� �� */ }

    private void ApplyExit(GameState prev)
    {
        switch (prev)
        {
            case GameState.Playing:
                // ��: �Է� ��Ȱ��, ���� ����, Ÿ�̸� ���� ��
                break;
            case GameState.MainMenu:
                // ��: �޴� UI �ݱ�
                break;
            case GameState.Paused:
                // ��: �Ͻ����� UI �ݱ�
                break;
        }
    }

    private void ApplyEnter(GameState next)
    {
        switch (next)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // �޴� UI �ѱ�, BGM �޴��� ��
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // HUD ON, �Է�/���� ���, BGM ���ӿ� ��
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // �Ͻ����� UI ON
                break;

            case GameState.GameOver:
            case GameState.Victory:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // ��� UI ON
                break;
        }
    }

    // --------------------------------------------------------------------
    // 2) ����(����) ��Ģ ���/���� - (prev, next) ���� ����
    // --------------------------------------------------------------------
    private void RegisterTransitions()
    {
        // MainMenu �� Loading
        _transition[(GameState.MainMenu, GameState.Loading)] = () =>
        {
            // ���̵� �� ����, �ε� �г� ǥ��, ���ð� ���� ��
        };

        // Loading �� Playing
        _transition[(GameState.Loading, GameState.Playing)] = () =>
        {
            // �� �غ� Ȯ��, ī�޶�/�÷��̾� �ʱ�ȭ, BGM ��ȯ
        };

        // Paused �� Playing
        _transition[(GameState.Paused, GameState.Playing)] = () =>
        {
            // �Ͻ����� ���� ����Ʈ, Ÿ�ӽ����� ������ Enter���� ó��
        };

        // Playing �� Paused
        _transition[(GameState.Playing, GameState.Paused)] = () =>
        {
            // ������ ����, �Ͻ����� UI �ִϸ��̼� ���� ��
        };

        // Playing �� GameOver
        _transition[(GameState.Playing, GameState.GameOver)] = () =>
        {
            // ��� ����, �Է� ����(Exit������ �ϵ� �̰����� ��� ���� Ʈ����)
        };

        // Playing �� Victory
        _transition[(GameState.Playing, GameState.Victory)] = () =>
        {
            // �¸� ����, ��� ���
        };

        // �ʿ� ���̸� �߰��� ��ϡ�
    }

    private void ApplyTransition(GameState prev, GameState next)
    {
        if (_transition.TryGetValue((prev, next), out var rule))
            rule?.Invoke();
        // ������ �ƹ� �͵� �� ��(�⺻ Enter/Exit�����ε� ���)
    }
    
}
