using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//功能:摄像机的跟随移动
//挂载 Main Camera

public class CameraFollowTest : MonoBehaviour {

    public float speed = 5;
    public float cameraDis = 1.5f;
    private Transform player;
    private Vector3 offsetPos;
    private float valueMouse;
    private float speedRotate = 5;
    private bool isUseMouse;
    private Vector2 lastSingleTouchPosition;
    private bool isSingleFinger;
    private int touchId = -1;
    public Text text;
    private Vector2[] oldPositionArray = new Vector2[3];
    private TouchState touchState = TouchState.NULL;
    private float[] oldDoubleDistance = new float[3];

    private List<Vector2> oldPositionDouble;
    private List<Touch> touchArray;

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
        else if (Input.touchCount > 0)
        {
            GameraMoveThroughTouch();
        }

    }

    private void GameraMoveThroughTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
            {
                CheckTouchAmongJoystick(Input.GetTouch(i));
            }
            if (Input.GetTouch(i).phase == TouchPhase.Moved && touchId != Input.GetTouch(i).fingerId)
            {

                if (touchState == TouchState.DOUBLE)
                {
                    Vector2 testPos = Input.GetTouch(i).position;
                    valueMouse = offsetPos.magnitude;
                    float lastTouchDistance = Vector3.Distance(testPos, oldPositionArray[Input.GetTouch(i).fingerId]);
                    valueMouse -= 0.1f;
                    valueMouse = Mathf.Clamp(valueMouse, 3, 16);
                    offsetPos = offsetPos.normalized * valueMouse;
                    oldPositionArray[Input.GetTouch(i).fingerId] = Input.GetTouch(i).position;

                    text.text = "坐标 x:" + lastTouchDistance + " y: " + valueMouse + " old: " + oldDoubleDistance[Input.GetTouch(i).fingerId] + "\n";
                    oldDoubleDistance[Input.GetTouch(i).fingerId] = lastTouchDistance;
                }
                else if (touchState == TouchState.SINGLE)
                {
                    Vector2 testPos = Input.GetTouch(i).position;
                    Vector2 oldPos = oldPositionArray[Input.GetTouch(i).fingerId];
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
                    oldPositionArray[Input.GetTouch(i).fingerId] = Input.GetTouch(i).position;
                    text.text = "坐标 x:" + moveX + " y: " + moveY + "  old: " + oldPos + "\n";
                }
            }
            if (touchId == Input.GetTouch(i).fingerId && Input.GetTouch(i).phase == TouchPhase.Ended)
            {
                touchId = -1;
            }
        }

    }

    private void CheckTouchAmongJoystick(Touch touch)
    {
        
        bool pointer = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        
        if (pointer)
        {
            touchId = touchId == -1 ? touch.fingerId : touchId;
            oldPositionArray[touch.fingerId] = touch.position;
            oldDoubleDistance[touch.fingerId] = 0;
        }
        else
        {
            oldPositionArray[touch.fingerId] = touch.position;
            oldDoubleDistance[touch.fingerId] = 0;
           

        }
        text.text = "state " + touchState + " count: " + Input.touchCount + "  id: " + touch.fingerId + "\n";

    }


    private void CameraMoveThroughMouse()
    {
        CameraView();
        //摄像机左右旋转
        if (Input.GetMouseButton(1))
        {
            oldPositionDouble.Add(Vector2.zero);
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
