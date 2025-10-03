using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSwitch : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform handle;     // Handle Rect
    [SerializeField] private RectTransform background; // Background Rect (선택)
    [SerializeField] private Image backgroundImage;    // 배경 색상 바꿀 용
    [SerializeField] private Image handleImage;        // 핸들(선택)

    [Header("Visuals")]
    [SerializeField] private float animDuration = 0.15f;
    [SerializeField] private float handlePadding = 2f; // 양 끝 여백
    [SerializeField] private Color onColor = new Color(0.25f, 0.75f, 1f); // ON 배경색
    [SerializeField] private Color offColor = new Color(0.75f, 0.75f, 0.75f); // OFF 배경색
    [SerializeField] private Color onHandleColor = Color.white;
    [SerializeField] private Color offHandleColor = Color.white;

    private Toggle _toggle;
    private Coroutine _animCo;
    private void Awake()
    {
        if (!_toggle) _toggle = GetComponent<Toggle>();
        //_toggle.onValueChanged.RemoveAllListeners();
    }

    private void OnEnable()
    {
        if (!_toggle) _toggle = GetComponent<Toggle>();
        if (_toggle != null)
        { 
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        SetVisual(_toggle.isOn, true);
    }

    private void OnDisable()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (_animCo != null) StopCoroutine(_animCo);
        _animCo = StartCoroutine(Animate(isOn));
    }

    private IEnumerator Animate(bool isOn)
    {
        // 시작/목표 값 계산
        Vector2 startPos = handle.anchoredPosition;
        Vector2 endPos = GetHandlePos(isOn);

        Color startBg = backgroundImage ? backgroundImage.color : Color.white;
        Color targetBg = isOn ? onColor : offColor;

        Color startHandle = handleImage ? handleImage.color : Color.white;
        Color targetHandle = isOn ? onHandleColor : offHandleColor;

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime; // UI는 보통 unscaled 추천
            float k = Mathf.Clamp01(t / animDuration);

            handle.anchoredPosition = Vector2.Lerp(startPos, endPos, Smooth(k));
            if (backgroundImage) backgroundImage.color = Color.Lerp(startBg, targetBg, k);
            if (handleImage) handleImage.color = Color.Lerp(startHandle, targetHandle, k);

            yield return null;
        }

        SetVisual(isOn, true);
        _animCo = null;
    }

    private void SetVisual(bool isOn, bool instant)
    {
        if (instant)
        {
            handle.anchoredPosition = GetHandlePos(isOn);
            if (backgroundImage) backgroundImage.color = isOn ? onColor : offColor;
            if (handleImage) handleImage.color = isOn ? onHandleColor : offHandleColor;
        }
        else
        {
            if (_animCo != null) StopCoroutine(_animCo);
            _animCo = StartCoroutine(Animate(isOn));
        }
    }

    private Vector2 GetHandlePos(bool isOn)
    {
        float width = ((RectTransform)transform).rect.width;       // 전체 스위치 폭
        float handleWidth = handle.rect.width;
        float leftX = handlePadding + handleWidth * 0.5f; //- ((RectTransform)transform).pivot.x * width;
        float rightX = width - handlePadding - handleWidth * 0.5f; //- ((RectTransform)transform).pivot.x * width;

        float x = isOn ? rightX : leftX;
        return new Vector2(x, handle.anchoredPosition.y);
    }

    // 살짝 부드러운 가속/감속
    private float Smooth(float x) => x * x * (3f - 2f * x);
}
