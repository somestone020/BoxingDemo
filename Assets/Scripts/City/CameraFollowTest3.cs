using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//功能:摄像机的跟随移动
//挂载 Main Camera

public class CameraFollowTest3 : MonoBehaviour {

    public float speed = 5;
    public float cameraDis = 1.5f;
    private Transform player;
    private Vector3 offsetPos;
    private float valueMouse;
    private float speedRotate = 5;
    private bool isUseMouse;
    private int touchId = -1;
    public Text text;
    private Vector2[] oldPositionDouble = new Vector2[3];
    private List<int> testIndex = new List<int>();
    //private int[] tempPosition = new int[2];
    //private List<int> touchArray = new List<int>();
    //private Vector2 oldPositionSingle;
    private TouchState touchState = TouchState.NULL;

    private enum TouchState
    {
        NULL,
        SINGLE,
        DOUBLE
    }

    void Start()
    {
        text = GameObject.Find("Canvas").transform.Find("TouchScreenControls/VirtualJoystick/Text").GetComponent<Text>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        transform.LookAt(new Vector3(player.position.x, player.position.y + cameraDis, player.position.z));
        offsetPos = transform.position - player.position;
#if UNITY_IOS || UNITY_ANDROID && !UNITY_EDITOR
        isUseMouse = false;
#else
        isUseMouse = true;
#endif
    }
    

    void Update()
    {
        transform.position = offsetPos + player.position;
        if (isUseMouse)
        {
            CameraMoveThroughMouse();
        }
        else if (Input.touchCount > 0 && Input.touchCount <= 3)
        {
            GameraMoveThroughTouch(Input.touchCount);
        }
    }

    private void GameraMoveThroughTouch(int touchCount)
    {
        int testId = 0;
        for (int i = 0; i < touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                CheckTouchAmongJoystick(Input.GetTouch(i),i);
            }
        }
        
        if(touchState == TouchState.DOUBLE)
        {
            testIndex.Clear();
            for (int j = 0; j < touchCount; j++)
            {
                if(Input.GetTouch(j).fingerId != touchId)
                {
                    testIndex.Add(j);
                }
                else if(touchCount == 3)
                {
                    testId = j;
                }
            }
            ScaleCamera(testIndex);
           
        }
        else if(touchState == TouchState.SINGLE)
        {
            int index = 0;
            for (int j = 0; j < touchCount; j++)
            {
                if (Input.GetTouch(j).fingerId != touchId)
                {
                    index = j;
                    
                }
                else if(touchCount == 2)
                {
                    testId = j;
                }
            }
            MoveCamera(index);
            
        }
        if(touchId != -1 && Input.GetTouch(testId).fingerId == touchId && Input.GetTouch(testId).phase == TouchPhase.Ended)
        {
            touchId = -1;
        }
        RemoveEndedTouch();
       

    }


    private void CheckTouchAmongJoystick(Touch touch,int index)
    {
        bool pointer = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        int count = Input.touchCount;
        if (pointer && touchId == -1)
        {
            touchId = touch.fingerId;
        }
        if (touchId != -1) 
        { 
            if (count == 3) touchState = TouchState.DOUBLE;
            else if (count == 2) touchState = TouchState.SINGLE; 
            else touchState = TouchState.NULL;
        }
        else
        {
            if (count == 2) touchState = TouchState.DOUBLE;
            else if(count == 1) touchState = TouchState.SINGLE;
            else touchState = TouchState.NULL;

        }
        oldPositionDouble[touch.fingerId] = touch.position;
        text.text = "坐标 tID:" + touchId + " fID: " + touch.fingerId + "  Length: " + Input.touches.Length + " index:" + index + "  State:" + touchState;
    }


    private void ScaleCamera(List<int> testIndex)
    {
        Touch touch1 = Input.GetTouch(testIndex[0]);
        Touch touch2 = Input.GetTouch(testIndex[1]);
        var tempPosition1 = touch1.position;
        var tempPosition2 = touch2.position;


        float currentTouchDistance = Vector3.Distance(tempPosition1, tempPosition2);
        float lastTouchDistance = Vector3.Distance(oldPositionDouble[touch1.fingerId], oldPositionDouble[touch2.fingerId]);

        valueMouse = offsetPos.magnitude;
        valueMouse -= (currentTouchDistance - lastTouchDistance) * speed * Time.deltaTime * 0.5f;
        valueMouse = Mathf.Clamp(valueMouse, 3, 16);
        offsetPos = offsetPos.normalized * valueMouse;

        text.text = "坐标 aa:" + tempPosition1 + " bb: " + tempPosition2 + " cc: " + oldPositionDouble[0] + "  dd :" + oldPositionDouble[1];

        oldPositionDouble[touch1.fingerId] = tempPosition1;
        oldPositionDouble[touch2.fingerId] = tempPosition2;
        
    }

    private void MoveCamera(int index)
    {
        Touch testTouch = Input.GetTouch(index);
        Vector2 testPos = testTouch.position;
        Vector2 oldPos = oldPositionDouble[testTouch.fingerId];
        float moveX = testPos.x - oldPos.x;
        float moveY = testPos.y - oldPos.y;
        transform.RotateAround(player.position, player.up, moveX * speedRotate * Time.deltaTime);

        Vector3 tempPos = transform.position;
        Quaternion tempQua = transform.rotation;
        transform.RotateAround(player.position, -transform.right, moveY * speedRotate * Time.deltaTime);
        if (transform.eulerAngles.x >= 80 || transform.eulerAngles.x <= 10)
        {
            transform.position = tempPos;
            transform.rotation = tempQua;
        }

        offsetPos = transform.position - player.position;
        oldPositionDouble[testTouch.fingerId] = testPos;
        text.text = "坐标 Test:" + testPos + "  old: " + oldPos + "  Index:" + index;
    }

    private void RemoveEndedTouch()
    {
        
    }


    private void CameraMoveThroughMouse()
    {
        CameraView();
        //摄像机左右旋转
        if (Input.GetMouseButton(1))
        {

            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");
            transform.RotateAround(player.position, player.up, moveX * speedRotate);

            Vector3 tempPos = transform.position;
            Quaternion tempQua = transform.rotation;
            transform.RotateAround(player.position, -transform.right, moveY * speedRotate);
            if (transform.eulerAngles.x >= 80 || transform.eulerAngles.x <= 10)
            {
                transform.position = tempPos;
                transform.rotation = tempQua;
            }

            offsetPos = transform.position - player.position;
            //Debug.Log("相机  方向" + transform.forward + "  角度" + transform.rotation.eulerAngles);
        }
    }

    //视野调整
    private void CameraView()
    {
        valueMouse = offsetPos.magnitude;
        valueMouse -= Input.GetAxis("Mouse ScrollWheel") * speed;
        valueMouse = Mathf.Clamp(valueMouse, 3, 16);
        offsetPos = offsetPos.normalized * valueMouse;
        
    }

}
