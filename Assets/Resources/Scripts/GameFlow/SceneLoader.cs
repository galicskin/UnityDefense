using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SceneLoader : MonoBehaviour
{

    public async Task ChangeSceneAsync(string sceneName)
    {
        // �񵿱� �� �ε� ����
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 90%���� �ε�� ������ ���
        while (op.progress < 0.9f)
        {
            Debug.Log($"Loading progress: {op.progress / 0.9f * 100f}%");
            await Task.Yield(); // ���� �����ӱ��� ��ٸ�
        }

        // ���⼭ UI�� ���̵�ƿ��� �����ٰ� ����
        op.allowSceneActivation = true;

        // �� ��ü �Ϸ�� ������ ���
        while (!op.isDone)
        {
            await Task.Yield();
        }

        Debug.Log("�� �ε� �Ϸ�!");
    }
}
