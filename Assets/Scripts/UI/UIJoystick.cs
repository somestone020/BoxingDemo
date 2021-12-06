using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RectTransform))]
public class UIJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
	
	public RectTransform handle;
	public float radius = 40f;
	public float autoReturnSpeed = 8f;
	private bool returnToStartPos;
	private RectTransform parentRect;
	private InputManager inputmanager;
	private CityManager cityManager;
	private bool isCity = false;

	void OnEnable(){
		returnToStartPos = true;
		handle.transform.SetParent(transform);
		parentRect = GetComponent<RectTransform>();
		isCity = SceneManager.GetActiveScene().name == "01_Game";
        
	}

	void Start()
    {
		if (isCity && inputmanager == null)
		{
			inputmanager = GameObject.FindObjectOfType<InputManager>();
		}
		else if (!isCity)
		{
			cityManager = GameObject.FindObjectOfType<CityManager>();
			if (cityManager.inputType != INPUTTYPE.TOUCHSCREEN)
			{
				transform.parent.gameObject.SetActive(false);
            }
            else
            {
				for (int i = 1; i < transform.parent.childCount; i++)
				{
					transform.parent.GetChild(i).gameObject.SetActive(false);
				}
			}
		}
		
	}

	void Update() {
		

		//return to start position
		if (returnToStartPos) {
			if (handle.anchoredPosition.magnitude > Mathf.Epsilon) {
				handle.anchoredPosition -= new Vector2 (handle.anchoredPosition.x * autoReturnSpeed, handle.anchoredPosition.y * autoReturnSpeed) * Time.deltaTime;
				if (isCity)
				{
					inputmanager.OnTouchScreenJoystickEvent(Vector2.zero);
				}
				else 
				{ 
					cityManager.OnTouchScreenEvent(Vector3.zero); 
				}
			} else {
				returnToStartPos = false;
			}
		}
	}

	//return coordinates
	public Vector2 Coordinates {
		get	{
			if (handle.anchoredPosition.magnitude < radius){
				return handle.anchoredPosition / radius;
			}
			return handle.anchoredPosition.normalized;
		}
	}

	//touch down
	void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
		returnToStartPos = false;
		var handleOffset = GetJoystickOffset(eventData);
		handle.anchoredPosition = handleOffset;
		if (isCity && inputmanager != null)
		{
			inputmanager.OnTouchScreenJoystickEvent(handleOffset.normalized);
		}
		else
		{
			cityManager.OnTouchScreenEvent(handleOffset.normalized);
		}
	}

	//touch drag
	void IDragHandler.OnDrag(PointerEventData eventData) {
		var handleOffset = GetJoystickOffset(eventData);
		handle.anchoredPosition = handleOffset;
		if (isCity && inputmanager != null)
		{
			inputmanager.OnTouchScreenJoystickEvent(handleOffset.normalized);
		}
		else
		{
			cityManager.OnTouchScreenEvent(handleOffset.normalized);
		}
	}

	//touch up
	void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
		returnToStartPos = true;
        if (!isCity)
        {
			cityManager.OnTouchScreenEvent(Vector3.zero);
		}
	}

	//get offset
	private Vector2 GetJoystickOffset(PointerEventData eventData) {
		
		Vector3 globalHandle;
		if (RectTransformUtility.ScreenPointToWorldPointInRectangle (parentRect, eventData.position, eventData.pressEventCamera, out globalHandle)) {
			handle.position = globalHandle;
		}

		var handleOffset = handle.anchoredPosition;
		if (handleOffset.magnitude > radius) {
			handleOffset = handleOffset.normalized * radius;
			handle.anchoredPosition = handleOffset;
		}
		return handleOffset;
	}
}