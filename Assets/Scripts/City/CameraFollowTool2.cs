using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//挂载点：Camera

public class CameraFollowTool2 : MonoBehaviour
{
    [Header("摄像机跟随的目标")]
    public Transform target;
    public const float HOR_DISTANCE_DEAFULT = 10.0f;
    [Header("摄像机与目标的水平距离")]
    public float horizontalDistance = 8.0f;
    [Header("摄像机与目标的垂直距离")]
    public float verticalDistance = 5.0f;

    private Vector3 positionForCameraWillTo;

    void LateUpdate()
    {
        if (!target) return;

        horizontalDistance = HOR_DISTANCE_DEAFULT;
        positionForCameraWillTo = new Vector3(target.position.x, target.position.y + verticalDistance, target.position.z);
        Quaternion cameraRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 cameraPositon = positionForCameraWillTo + (cameraRotation * Vector3.back * horizontalDistance);
        RaycastHit[] hits = Physics.RaycastAll(new Ray(positionForCameraWillTo, (cameraPositon - positionForCameraWillTo).normalized));
        
        if (hits.Length > 0)
        {
            RaycastHit stand = hits[0];
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance < stand.distance)
                {
                    stand = hit;
                }
            }
            Debug.Log(stand.point + " " + stand.collider.gameObject.tag);
            string tag = stand.collider.gameObject.tag;
            horizontalDistance = Vector3.Distance(stand.point, positionForCameraWillTo);
            if (horizontalDistance > HOR_DISTANCE_DEAFULT)
            {
                horizontalDistance = HOR_DISTANCE_DEAFULT;
            }

        }
        cameraPositon = positionForCameraWillTo + (cameraRotation * Vector3.back * horizontalDistance);
        transform.position = Vector3.Lerp(transform.position, cameraPositon, 0.3f); ;
        Debug.DrawLine(target.transform.position, transform.position, Color.red);
        transform.LookAt(target.position);
    }
}
