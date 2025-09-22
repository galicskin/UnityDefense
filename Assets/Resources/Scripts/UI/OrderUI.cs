using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
public class OrderUI : MonoBehaviour
{
    [SerializeField] private Dictionary<Button,Action> OrderButtonActionDict = new();

    public void SetButton(Button button, Action action)
    {
        OrderButtonActionDict[button] = action;
        button.onClick.AddListener(() => action?.Invoke());
    }
    
}
