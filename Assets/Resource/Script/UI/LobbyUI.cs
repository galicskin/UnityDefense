using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string nextSceneName = "Game"; // ¹Ù²ã¼­ »ç¿ë

    public void OnClickStartGame()
    {
        _ = GameManager.Instance.StartGameAsync();
    }
}
