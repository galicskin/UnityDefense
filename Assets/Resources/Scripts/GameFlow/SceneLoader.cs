using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SceneLoader : MonoBehaviour
{

    public async Task ChangeSceneAsync(string sceneName)
    {
        // 비동기 씬 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 90%까지 로드될 때까지 대기
        while (op.progress < 0.9f)
        {
            Debug.Log($"Loading progress: {op.progress / 0.9f * 100f}%");
            await Task.Yield(); // 다음 프레임까지 기다림
        }

        // 여기서 UI나 페이드아웃이 끝났다고 가정
        op.allowSceneActivation = true;

        // 씬 교체 완료될 때까지 대기
        while (!op.isDone)
        {
            await Task.Yield();
        }

        Debug.Log("씬 로드 완료!");
    }
}
