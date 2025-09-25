using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class OrderUI : MonoBehaviour
{
    [SerializeField] private List<Button> buttons = new();
    [SerializeField] private Dictionary<Button,Action> OrderButtonActionDict = new();
    [SerializeField] private Sprite defaultBurronSprite;
    public void Awake()
    {
        for (int i = 0; i < buttons.Count; ++i)
        {
            int index = i; // <-- 로컬 복사
            OrderButtonActionDict.Add(buttons[i], null);
            buttons[i].onClick.AddListener(() => OnClickButton(index));
        }
    }

    public void SetButton(Button button, Action action, Sprite buttonSprite = null)
    {
        button.image.sprite = buttonSprite == null ? defaultBurronSprite : buttonSprite;
        OrderButtonActionDict[button] = action;
        //button.onClick.AddListener(() => OrderButtonActionDict[button]?.Invoke());

    }
    
    public void SetButton(int buttonNumber, Action action, Sprite buttonSprite = null)
    {
        SetButton(buttons[buttonNumber], action, buttonSprite);
    }

    private void OnClickButton(int buttonNumber)
    {
        OrderButtonActionDict[buttons[buttonNumber]]?.Invoke();
    }

}
