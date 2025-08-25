using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string nextSceneName = "Game"; // �ٲ㼭 ���

    public void OnClickStartGame()
    {
        _ = GameManager.Instance.StartGameAsync();
    }
}
