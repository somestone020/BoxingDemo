using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//挂载点：Camera
[RequireComponent(typeof(BoxCollider))]
public class CameraFollowTool1 : MonoBehaviour
{
    [Header("摄像机跟随的目标")]
    public GameObject target;
    [Header("摄像机与目标的水平距离")]
    public float horizontalDistance = 10.0f;
    [Header("摄像机与目标的垂直距离")]
    public float verticalDistance = 5.0f;
    [Header("摄像机与目标高度差值")]
    public float hightDifference = 2f;
    [Header("摄像机跟随的高度阻尼")]
    public float heightDamping = 2.0f;
    [Header("摄像机跟随的旋转阻尼")]
    public float rotationDamping = 3.0f;

    private MeshRenderer mR;
    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");
    }

    private void LateUpdate()
    {
        if (!target) return;

        //摄像机将要到达的高度和角度
        float angleForCameraWillTo = target.transform.eulerAngles.y;
        float heightForCameraWillTo = target.transform.position.y + verticalDistance;
        //摄像机当前的高度和角度
        float angleForCameraCurrent = transform.eulerAngles.y;
        float heightForCameraCurrent = transform.position.y;

        //使用线性插值计算摄像机每帧的旋转角度和移动距离
        angleForCameraCurrent = Mathf.LerpAngle(angleForCameraCurrent, angleForCameraWillTo, rotationDamping * Time.deltaTime);
        heightForCameraCurrent = Mathf.Lerp(heightForCameraCurrent, heightForCameraWillTo, heightDamping * Time.deltaTime);
        //从角度值转换为四元数
        Quaternion cameraCurrentRotation = Quaternion.Euler(0, angleForCameraCurrent, 0);
        Vector3 targetPos = target.transform.position;
        targetPos -= cameraCurrentRotation * Vector3.forward * horizontalDistance;
        targetPos = new Vector3(targetPos.x, heightForCameraCurrent, targetPos.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.time);

        //计算射线的方向
        Vector3 targetRayDirection = target.transform.position;
        //方向（单位向量）
        Vector3 targetRayDirectionNor = (target.transform.position - transform.position).normalized;
        float cmaeraAngleForY = transform.eulerAngles.y;
        targetRayDirection -= targetRayDirectionNor * cmaeraAngleForY;
        //在场景中画出这条射线
        Debug.DrawLine(target.transform.position, targetRayDirection, Color.red);
        //target朝着这个方向发射射线
        RaycastHit hit;
        if (Physics.Linecast(target.transform.position, targetRayDirection, out hit))
        {
            mR = hit.collider.GetComponent<MeshRenderer>();
            if (hit.collider.gameObject.tag != gameObject.tag)
            {
                //transform.position = hit.point;
                //mR = hit.collider.GetComponent<MeshRenderer>();
                if (mR != null)
                {
                    Color color = hit.collider.GetComponent<MeshRenderer>().material.color;
                    color.a = 0.2f;
                    hit.collider.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
                }
            }
            else
            {
                //Debug.Log(mR.name);
                //Color color = mR.material.color;
                //color.a = 1f;
                //mR.material.SetColor("_Color", color);
            }
        }

        transform.LookAt(new Vector3(target.transform.position.x, target.transform.position.y + hightDifference, target.transform.position.z));
    }
}
