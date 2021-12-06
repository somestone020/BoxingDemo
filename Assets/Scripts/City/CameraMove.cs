using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//功能:主摄像机的开场移动效果
//挂载 Main Camera

public class CameraMove : MonoBehaviour
{
    public float speed = 3;

    private Transform cameraTrans;
	void Start ()
    {
        cameraTrans = Camera.main.transform;

    }
	
	void Update ()
    {
        if(cameraTrans.localPosition.z <= 50)
            cameraTrans.Translate(Vector3.forward * speed * Time.deltaTime); 
	}


}
