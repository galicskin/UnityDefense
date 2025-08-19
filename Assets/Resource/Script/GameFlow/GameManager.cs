using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 GameManager 찾기 (직접 gameManager를 넣어놨을때를 대비
                _instance = FindObjectOfType<GameManager>();

                // 없으면 새로 생성
                if (_instance == null)
                {
                    GameObject gm = new GameObject("GameManager");
                    _instance = gm.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    //GameCoordinator gameCoordinator = null;
    private SceneLoader sceneLoader { get; set; }
    private GameStateMachine stateMachine { get; set; }
    private StateChangeRule stateRule { get; set; }

    [SerializeField] GameState gameState;

    // 중복 방지 + 씬 전환에도 유지
    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }
    private void Initialize()
    {
        sceneLoader = new SceneLoader();
        stateMachine = new GameStateMachine();
        stateMachine.OnChanged += HandleStateChanged;
        stateRule = new StateChangeRule();
    }
    private void OnDestroy()
    {
        if (stateMachine != null)
            stateMachine.OnChanged -= HandleStateChanged; // 구독 해제
    }

    private void HandleStateChanged(GameState prev, GameState next)
    {
        if (prev == next && prev != GameState.Boot) return;
        gameState = next;
        stateRule.Apply(prev, next);
    }


    public async Task StartGameAsync()
    {
        // 상태만 바꿔서 규칙 적용(입력 OFF, 로딩UI ON 등)
        stateMachine.TryChangeState(GameState.Loading);

        // 절차는 SceneLoader가 담당
        await sceneLoader.ChangeSceneAsync("Game");

        // 로딩 끝난 시점에 다음 상태로
        stateMachine.TryChangeState(GameState.Playing); 
    }
}
