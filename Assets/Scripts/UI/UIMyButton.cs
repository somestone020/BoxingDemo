using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEditor;

public class UIMyButton : Button
{
    public RotatedDrection rotatedDrection = RotatedDrection.Null;

    [Serializable]
    public class LongButtonEvent : UnityEvent<RotatedDrection> { }

    [SerializeField]
    public LongButtonEvent OnLongPointerDown;
    [SerializeField]
    public LongButtonEvent OnLongPointerUp;
    [SerializeField]
    public LongButtonEvent OnLongPointerClick;
    [SerializeField]
    public LongButtonEvent OnLongPointerExit;
    [SerializeField]
    public LongButtonEvent OnLongPointerEnter;

    public enum RotatedDrection
    {
        Null = 0,
        Right = -1,
        Left = 1,

    }

    protected override void Start()
    {
        base.Start();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (OnLongPointerClick != null) OnLongPointerClick.Invoke(rotatedDrection);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (OnLongPointerEnter != null) OnLongPointerEnter.Invoke(rotatedDrection);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if(OnLongPointerDown != null) OnLongPointerDown.Invoke(rotatedDrection);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (OnLongPointerUp != null) OnLongPointerUp.Invoke(rotatedDrection);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (OnLongPointerExit != null) OnLongPointerExit.Invoke(rotatedDrection);
    }
}

[CustomEditor(typeof(UIMyButton))]
public class LocalizationTextEditor : UnityEditor.UI.ButtonEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UIMyButton component = (UIMyButton)target;
        EditorGUILayout.LabelField("RotatedDrection");
        component.rotatedDrection = (UIMyButton.RotatedDrection)EditorGUILayout.EnumPopup("Drection", component.rotatedDrection);
    }
}
