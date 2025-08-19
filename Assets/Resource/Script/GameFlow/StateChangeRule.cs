using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StateChangeRule
{
    // (prev, next) -> 전이 전용 규칙
    private readonly Dictionary<(GameState from, GameState to), Action> _transition = new();

    // ====== 초기 구성 ======
    public StateChangeRule()
    {
        RegisterCommonEnterExit();  // 공통 Enter/Exit 규칙(내부 메서드) 정의
        RegisterTransitions();      // 전이별(조합) 규칙 정의
    }

    // ====== 외부에서 호출할 단 하나의 진입점 ======
    public void Apply(GameState prev, GameState next)
    {
        ApplyExit(prev);                   // 1) 이전 상태에서 내려놓기
        ApplyTransition(prev, next);       // 2) 전이 조합 규칙
        ApplyEnter(next);                  // 3) 새 상태 올리기
    }

    // --------------------------------------------------------------------
    // 1) 공통 Exit/Enter 규칙(상태 단위) - 필요 시 내부에서 수정
    // --------------------------------------------------------------------
    private void RegisterCommonEnterExit() { /* 자리표시자: 필요 시 리소스 로드 등 */ }

    private void ApplyExit(GameState prev)
    {
        switch (prev)
        {
            case GameState.Playing:
                // 예: 입력 비활성, 스폰 중지, 타이머 정지 등
                break;
            case GameState.MainMenu:
                // 예: 메뉴 UI 닫기
                break;
            case GameState.Paused:
                // 예: 일시정지 UI 닫기
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
                // 메뉴 UI 켜기, BGM 메뉴로 등
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // HUD ON, 입력/스폰 허용, BGM 게임용 등
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // 일시정지 UI ON
                break;

            case GameState.GameOver:
            case GameState.Victory:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // 결과 UI ON
                break;
        }
    }

    // --------------------------------------------------------------------
    // 2) 전이(조합) 규칙 등록/적용 - (prev, next) 별로 정의
    // --------------------------------------------------------------------
    private void RegisterTransitions()
    {
        // MainMenu → Loading
        _transition[(GameState.MainMenu, GameState.Loading)] = () =>
        {
            // 페이드 인 시작, 로딩 패널 표시, 선택값 저장 등
        };

        // Loading → Playing
        _transition[(GameState.Loading, GameState.Playing)] = () =>
        {
            // 씬 준비 확인, 카메라/플레이어 초기화, BGM 전환
        };

        // Paused → Playing
        _transition[(GameState.Paused, GameState.Playing)] = () =>
        {
            // 일시정지 해제 이펙트, 타임스케일 복구는 Enter에서 처리
        };

        // Playing → Paused
        _transition[(GameState.Playing, GameState.Paused)] = () =>
        {
            // 스냅샷 저장, 일시정지 UI 애니메이션 시작 등
        };

        // Playing → GameOver
        _transition[(GameState.Playing, GameState.GameOver)] = () =>
        {
            // 사망 연출, 입력 차단(Exit에서도 하되 이곳에서 즉시 연출 트리거)
        };

        // Playing → Victory
        _transition[(GameState.Playing, GameState.Victory)] = () =>
        {
            // 승리 연출, 결과 계산
        };

        // 필요 전이만 추가로 등록…
    }

    private void ApplyTransition(GameState prev, GameState next)
    {
        if (_transition.TryGetValue((prev, next), out var rule))
            rule?.Invoke();
        // 없으면 아무 것도 안 함(기본 Enter/Exit만으로도 충분)
    }
    
}
