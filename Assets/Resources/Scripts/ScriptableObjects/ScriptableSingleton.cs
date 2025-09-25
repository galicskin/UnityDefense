// Assets/Scripts/Framework/ScriptableSingleton.cs
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// - 에디터: 프로젝트 어디에 있든 t:{TypeName} 에셋을 찾아 첫 번째를 사용(중복 경고)
/// - 런타임/빌드: Resources/{ResourcesPath}.asset 에서만 로드 (없으면 경고, null 반환)
/// - 자동 생성 없음
/// </summary>
public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
{
    private static T _instance;
    private static bool _searched;

    // 빌드에서 사용되는 Resources 상대 경로 (필요 시 파생 클래스에서 "new"로 숨겨 커스터마이즈)
    protected static string ResourcesPath => $"Config/{typeof(T).Name}";

    public static T Instance
    {
        get
        {
            if (_instance) return _instance;
            if (_searched) return _instance; // 이전 탐색 결과 유지

#if UNITY_EDITOR
            // 1) 프로젝트 전체에서 타입으로 검색
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids != null && guids.Length > 0)
            {
                if (guids.Length > 1)
                    Debug.LogWarning($"[ScriptableSingleton<{typeof(T).Name}>] 동일 타입 에셋이 {guids.Length}개입니다. 첫 번째만 사용합니다. 1개만 유지하세요.");

                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _instance = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            // 2) 못 찾았으면 런타임과 동일한 경로 시도(빌드 동작과 일치)
            if (_instance == null)
            {
                _instance = Resources.Load<T>(ResourcesPath);
                if (_instance == null)
                {
                    Debug.LogWarning(
                        $"[ScriptableSingleton<{typeof(T).Name}>] 에셋을 찾지 못했습니다.\n" +
                        $"- CreateAssetMenu를 통해 **하나만** 생성하세요.\n" +
                        $"- 빌드에서 Instance를 쓰려면 'Assets/Resources/{ResourcesPath}.asset' 위치에 두어야 합니다.\n" +
                        $"- 또는 씬/프리팹에서 직접 참조를 들고 사용하시고, 싱글톤 호출은 피하세요."
                    );
                }
            }
#else
            // 빌드/런타임: Resources 경로만 확인
            _instance = Resources.Load<T>(ResourcesPath);
            if (_instance == null)
            {
                Debug.LogWarning(
                    $"[ScriptableSingleton<{typeof(T).Name}>] 'Resources/{ResourcesPath}.asset' 에셋이 없습니다. " +
                    $"해당 경로에 에셋을 두거나, 런타임에서 Instance 사용을 피하세요."
                );
            }
#endif
            _searched = true;

            if (_instance != null)
                _instance.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            return _instance;
        }
    }

    protected virtual void OnEnable()
    {
        var self = (T)this;
        if (_instance == null) _instance = self;
        else if (_instance != self)
            Debug.LogWarning($"[ScriptableSingleton<{typeof(T).Name}>] 중복 인스턴스가 로드되었습니다: {name}. 동일 타입 에셋은 1개만 유지하세요.");
    }
}
