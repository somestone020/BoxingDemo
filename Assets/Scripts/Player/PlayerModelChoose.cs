using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerModelChoose : MonoBehaviour 
{
    private GameObject player;
    private float speed = 1;
    private UIMyButton button;
    private UIMyButton button2;
    private bool isRotate = false;
    private int rotateDic = 0;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        button = transform.Find("Rotate/Right").GetComponent<UIMyButton>();
        button2 = transform.Find("Rotate/Left").GetComponent<UIMyButton>();
        Debug.Log(button);
        button.OnLongPointerDown.AddListener((dic) => { isRotate = true; rotateDic = (int)dic; });
        button.OnLongPointerUp.AddListener((dic) => { isRotate = false; rotateDic = 0; });
        button2.OnLongPointerDown.AddListener((dic) => { isRotate = true; rotateDic = (int)dic; });
        button2.OnLongPointerUp.AddListener((dic) => { isRotate = false; rotateDic = 0; });
    }

    // Update is called once per frame
    void Update()
    {
        if (isRotate)
        {
            RotatePlayerToRight();
        }   
    }

    public void RotatePlayerToRight()
    {
        player.transform.Rotate(0,rotateDic * speed,0, Space.Self);
    }

    public void RotatePlayerToLeft(EventSystem eventSystem)
    {

    }

}
