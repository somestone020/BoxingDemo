using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CityPlayerMove : MonoBehaviour
{
    private GameObject player;
    private Rigidbody db;
    public float speed = 4;
    public float rotateSpeed = 20;
    private Animator animator;
    private GameObject cameraObj;
    private Vector3 direction;
    private bool isMover = false;
    private float cameraDev = 45f;
    public KeyCode[] keyCode;
    private bool isCity;

    private int isRunEnded = 0;

    private void Awake()
    {
       
    }

    void Start()
    {
        CityManager.onTouchScreenEvent += OnTouchScreenEvent;
        player = gameObject;
        db = player.GetComponent<Rigidbody>();
        animator = gameObject.transform.GetChild(0).GetComponent<Animator>();
        cameraObj = Camera.main.gameObject;
        isCity = SceneManager.GetActiveScene().name == "01_Game";
    }
    private void OnDisable()
    {
        CityManager.onTouchScreenEvent -= OnTouchScreenEvent;
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Escape)) // 返回键
        {
            GameObject.FindObjectOfType<UIManager>().ShowMenu("MainMenu");
        }
        if (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Home)) // Home键
        {
            //Code
        }
        if (!isCity && isMover) PlayerMoverToJoystick();
        else PlayMoverToKey();
        
    }

    private void PlayMoverToKey()
    {
        float x = 0;
        float z = 0;
        float x1 = 0;
        float z1 = 0;
        for (int i = 0; i < keyCode.Length; i++)
        {
            if (Input.GetKeyDown(keyCode[i]))
            {
                animator.SetBool("Run", true);
                isRunEnded++;
            }
            if (Input.GetKeyUp(keyCode[i]))
            {
                isRunEnded--;
            }
            if (Input.GetKey(keyCode[i]))
            {
                Vector3 cameraForward = cameraObj.transform.forward.normalized;
                Vector3 cameraRight = cameraObj.transform.right.normalized;
                if (keyCode[i].ToString() == "W") { z = cameraForward.z; x1 = cameraForward.x; }
                else if (keyCode[i].ToString() == "S") { z = -cameraForward.z; x1 = -cameraForward.x; }
                else if (keyCode[i].ToString() == "A") { x = -cameraRight.x; z1 = -cameraRight.z; }
                if (keyCode[i].ToString() == "D") { x = cameraRight.x; z1 = cameraRight.z; }
            }
        }
        Vector3 nor = new Vector3(x + x1, 0, z + z1).normalized;
        db.velocity = nor * speed;
        player.transform.LookAt(transform.position + nor);

        if (isRunEnded <= 0)
        {
            //animator.SetBool("Idle", true);
            animator.SetBool("Run", false);
        }
    }


    private void OnTouchScreenEvent(Vector3 dir)
    {
        if (dir != Vector3.zero) 
        {
            direction = dir;
            isMover = true;
            animator.SetBool("Run", true);
        }
        else
        {
            isMover = false;
            direction = Vector3.zero;
            animator.SetBool("Run", false);
        }
    }

    
    private void PlayerMoverToJoystick()
    {
        var cameraPos = GetCameraForward();
        Vector3 vecYao = new Vector3(direction.x, 0, direction.y);
        float angle = GetCameraAngle(Vector3.forward, vecYao);
        Vector3 moveVec = RotateRound(cameraPos, Vector3.up, angle);

        //Debug.Log(pos2 + " -- " + angle + " -- " + vecCor2 + " -- " + vec2 + " -- " + vecYao);
        db.MovePosition(transform.position + moveVec.normalized * Time.deltaTime * speed);
        transform.LookAt(transform.position + moveVec.normalized);
    }

    public Vector3 RotateRound(Vector3 position, Vector3 axis, float angle)
    {
        return Quaternion.AngleAxis(angle, axis) * position;
    }

    //计算相机的正前方
    private Vector3 GetCameraForward()
    {
        float anglesY = cameraObj.transform.rotation.eulerAngles.y;
        var anglesPos = Quaternion.Euler(0f, anglesY - cameraDev, 0f) * cameraObj.transform.position;
        var normalizedPos = new Vector3(anglesPos.x, 0, anglesPos.z).normalized;
        return normalizedPos;
    }

    //两个向量的夹角
    private float GetCameraAngle(Vector3 vec1,Vector3 vec2)
    {
        float angle = Vector3.Angle(vec1, vec2);
        Vector3 vecCor2 = Vector3.Cross(vec1, vec2);
        if (vecCor2.y < 0) angle = 360 - angle;

        return angle;
    }
}
