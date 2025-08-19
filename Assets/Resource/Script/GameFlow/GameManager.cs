using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    // �̱��� �ν��Ͻ�
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // ������ GameManager ã�� (���� gameManager�� �־�������� ���
                _instance = FindObjectOfType<GameManager>();

                // ������ ���� ����
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

    // �ߺ� ���� + �� ��ȯ���� ����
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
            stateMachine.OnChanged -= HandleStateChanged; // ���� ����
    }

    private void HandleStateChanged(GameState prev, GameState next)
    {
        if (prev == next && prev != GameState.Boot) return;
        gameState = next;
        stateRule.Apply(prev, next);
    }


    public async Task StartGameAsync()
    {
        // ���¸� �ٲ㼭 ��Ģ ����(�Է� OFF, �ε�UI ON ��)
        stateMachine.TryChangeState(GameState.Loading);

        // ������ SceneLoader�� ���
        await sceneLoader.ChangeSceneAsync("Game");

        // �ε� ���� ������ ���� ���·�
        stateMachine.TryChangeState(GameState.Playing); 
    }
}
