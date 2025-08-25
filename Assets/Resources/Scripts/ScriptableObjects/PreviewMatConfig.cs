using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Preview Mat Config", fileName = "PreviewMatConfig")]
public class PreviewMatConfig : ScriptableObject
{
    public Material blueMat;
    public Material redMat;

    // ���� ���� ���� (Resources/Build/PreviewMatConfig.asset �� �� ���)
    private static PreviewMatConfig _instance;
    public static PreviewMatConfig Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load<PreviewMatConfig>("Scripts/ScriptableObjects/PreviewMatConfig");
            return _instance;
        }
    }
}