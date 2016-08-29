using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public enum GameMode
{
	RPG, FPS
}

public class CameraManager : MonoBehaviour 
{
	public GameObject CharacterObject = null;
	public Transform NoClipTarget = null;
	public Transform HorizontalJoint = null;
	public Transform VerticalJoint = null;
	public Transform MainCamera = null;
	public bool FindRenderers = true;
	public List<Renderer> Renderers = null;
	public float FadeDistance = 1.0f;
	public float HideDistance = 0.8f;

	public GameMode Mode = GameMode.RPG;
	public Image Crosshair = null;

	public float MouseSensitivity = 4.0f;
	public float BottomRotationExtent = 60f;
	public float TopRotationExtent = -40f;
	public float SnapSpeed = 7.0f;
	public float NoClipSpeed = 56.0f;
	public float ZoomSpeed = 7.0f;
	public float ZoomAmount = 5.0f;
	public float ForwardZoomExtent = 1.0f;
	public float BackwardZoomExtent = 10.0f;

	private GameObject thisGameObject = null;

	private Animator anim = null;

	private bool rightMouseDown = false;
	private bool leftMouseDown = false;

	private Vector3 desiredCameraPosition = Vector3.zero;

	private float forwardZoomZ = 0.0f;
	private float backwardZoomZ = 0.0f;

	private Quaternion camWorldRotation;
	private bool camWorldRotationSet = false;

	// Use this for initialization
	void Start () 
	{
		thisGameObject = gameObject;

		if (CharacterObject == null)
		{
			Debug.LogError("No Character Game Object has been assigned to the Camera Manager");
		}
		anim = CharacterObject.GetComponent<Animator>();

		desiredCameraPosition = MainCamera.transform.localPosition;
		forwardZoomZ = desiredCameraPosition.z + ForwardZoomExtent;
		backwardZoomZ = desiredCameraPosition.z - BackwardZoomExtent;

		if (FindRenderers)
		{
			Renderers.Clear();
			Renderer[] rends = CharacterObject.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < rends.Length ; i++)
			{
				Renderers.Add(rends[i]);
			}
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (anim == null || MainCamera == null) return;

		if (!Input.GetButton("Mouse 0"))
		{
			leftMouseDown = false;
		}
		if (!Input.GetButton("Mouse 1"))
		{
			rightMouseDown = false;
		}
		
		if (Mode == GameMode.FPS)
		{
			Screen.lockCursor = true;
			Cursor.visible = false;
			if (Crosshair != null) Crosshair.enabled = true;
			
			//put camera horizontal rotation to zero
			HorizontalJoint.transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
			thisGameObject.transform.localRotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
			camWorldRotationSet = false;

			//orbit the camera vertically with Y mouse movement
			float v = TranslateAngle(VerticalJoint.rotation.eulerAngles.x) * -1;
			v += Input.GetAxis("Mouse Y") * MouseSensitivity;
			v = Mathf.Clamp (v, TopRotationExtent, BottomRotationExtent);
			
			VerticalJoint.transform.localEulerAngles = new Vector3(-v, 0, 0);

			//rotate the character horizontally with X mouse movement
			float h = MouseSensitivity * Input.GetAxis("Mouse X");
			CharacterObject.transform.Rotate(0.0f, h, 0.0f, Space.World);

			//set the camera position to the location of the character
			MainCamera.localPosition = new Vector3(MainCamera.localPosition.x, MainCamera.localPosition.y, 0.0f);
			
			//turn character rendering off
			foreach(Renderer rend in Renderers)
			{
				rend.enabled = false;
			}
		}
		else if (Mode == GameMode.RPG)
		{
			if (Crosshair != null) Crosshair.enabled = false;
			if (rightMouseDown || leftMouseDown)
			{
				Screen.lockCursor = true;
				Cursor.visible = false;
				
				//orbit the camera vertically with Y mouse movement
				float v = TranslateAngle(VerticalJoint.rotation.eulerAngles.x) * -1;
				v += Input.GetAxis("Mouse Y") * MouseSensitivity;
				v = Mathf.Clamp (v, TopRotationExtent, BottomRotationExtent);
				
				VerticalJoint.transform.localEulerAngles = new Vector3(-v, 0, 0);			
			}
			
			if (rightMouseDown)
			{
				if (anim.GetBool("Fall"))
				{
					if (camWorldRotationSet)
					{
						thisGameObject.transform.rotation = camWorldRotation;
					}
					else
					{
						camWorldRotation = thisGameObject.transform.rotation;
						camWorldRotationSet = true;
					}
					
					//orbit the camera horizontally with X mouse movement
					float h = MouseSensitivity * Input.GetAxis("Mouse X");
					HorizontalJoint.transform.Rotate(0.0f, h, 0.0f, Space.Self);
				}
				else
				{
					//put camera horizontal rotation back
					HorizontalJoint.transform.localRotation = Quaternion.Lerp(HorizontalJoint.transform.localRotation, Quaternion.Euler(0.0f, 0.0f, 0.0f), 
					                                                          Time.deltaTime * SnapSpeed);
					thisGameObject.transform.localRotation = Quaternion.Lerp(thisGameObject.transform.localRotation, Quaternion.Euler(0.0f, 0.0f, 0.0f), 
					                                                         Time.deltaTime * SnapSpeed);
					camWorldRotationSet = false;
					
					//rotate the character horizontally with X mouse movement
					float h = MouseSensitivity * Input.GetAxis("Mouse X");
					CharacterObject.transform.Rotate(0.0f, h, 0.0f, Space.World);
				}
			}
			else if (leftMouseDown) 
			{
				if (camWorldRotationSet)
				{
					thisGameObject.transform.rotation = camWorldRotation;
				}
				else
				{
					camWorldRotation = thisGameObject.transform.rotation;
					camWorldRotationSet = true;
				}
				
				//orbit the camera horizontally with X mouse movement
				float h = MouseSensitivity * Input.GetAxis("Mouse X");
				HorizontalJoint.transform.Rotate(0.0f, h, 0.0f, Space.Self);
				
			}
			else
			{
				Screen.lockCursor = false;
				Cursor.visible = true;
				
				//put camera horizontal rotation back if moving
				if ((anim.GetFloat("Speed") > 0.1f || anim.GetFloat("Speed") < -0.1f) ||
				    (Input.GetAxis("Horizontal") > 0.1f || Input.GetAxis("Horizontal") < -0.1f) ||
				    (Input.GetAxis("Strafe") > 0.1f || Input.GetAxis("Strafe") < -0.1f))
				{
					HorizontalJoint.transform.localRotation = Quaternion.Lerp(HorizontalJoint.transform.localRotation, Quaternion.Euler(0.0f, 0.0f, 0.0f), 
					                                                          Time.deltaTime * SnapSpeed);
					thisGameObject.transform.localRotation = Quaternion.Lerp(thisGameObject.transform.localRotation, Quaternion.Euler(0.0f, 0.0f, 0.0f), 
					                                                         Time.deltaTime * SnapSpeed);
					camWorldRotationSet = false;
				}
				
			}
			
			//set the ideal location of the camera based on zoom level
			float dp = desiredCameraPosition.z;
			dp += Input.GetAxis("Mouse ScrollWheel") * ZoomAmount;
			dp = Mathf.Clamp(dp, backwardZoomZ, forwardZoomZ);
			desiredCameraPosition =  new Vector3(desiredCameraPosition.x, desiredCameraPosition.y, dp);
			
			//figure out of there is anything obstructing the view from the desired camera location
			Vector3 rayHeading = VerticalJoint.transform.TransformPoint(desiredCameraPosition) - NoClipTarget.position;
			float distance = rayHeading.magnitude;
			Vector3 direction = rayHeading / distance;
			RaycastHit rayInfo;
			Physics.SphereCast(NoClipTarget.position, 0.3f, direction, out rayInfo, distance);
			Debug.DrawRay(NoClipTarget.position, direction * distance, Color.yellow);
			
			//If there is something blocking the view of the camera, move the camera in front of it, otherwise move the camera to the
			//desired location
			if (rayInfo.collider == null)
			{
				MainCamera.localPosition =  Vector3.Lerp(MainCamera.localPosition, desiredCameraPosition, Time.deltaTime * ZoomSpeed);
			}
			else
			{
				Vector3 noClipDistanceVector = rayInfo.point - VerticalJoint.transform.TransformPoint(desiredCameraPosition);
				Vector3 noClipPoint = VerticalJoint.transform.TransformPoint(new Vector3(desiredCameraPosition.x, desiredCameraPosition.y, 
				                                                                         desiredCameraPosition.z + noClipDistanceVector.magnitude));
				
				MainCamera.position =  Vector3.Lerp(MainCamera.position, noClipPoint, Time.deltaTime * NoClipSpeed);
			}
			
			//Do the camera fade based on camera proximity
			
			float camDistance = (VerticalJoint.position - MainCamera.position).magnitude;
			
			foreach(Renderer rend in Renderers)
			{
				if (camDistance < HideDistance)
				{
					rend.enabled = false;
				}
				else if (camDistance < FadeDistance)
				{
					rend.enabled = true;
					float alpha = 1.0f - (FadeDistance - camDistance) / (FadeDistance - HideDistance);
					if (rend.material.color.a != alpha)
					{
						rend.material.color = new Color (rend.material.color.r, rend.material.color.g, rend.material.color.b, alpha);
					}
				}
				else
				{
					rend.enabled = true;
					if (rend.material.color.a != 1.0f)
					{
						rend.material.color = new Color (rend.material.color.r, rend.material.color.g, rend.material.color.b, 1.0f);
					}
				}
			}
		}
	}

	//change 360 degree angles into 180 degree angles
	private float TranslateAngle(float angle)
	{
		angle = (angle > 180) ? angle - 360 : angle;
		return angle;
	}

	//this is called from the Screen GUI object
	public void OnPointerDown(BaseEventData eventdata)
	{
		PointerEventData ped = eventdata as PointerEventData;

		if (ped.button == PointerEventData.InputButton.Left) leftMouseDown = true;
		if (ped.button == PointerEventData.InputButton.Right) rightMouseDown = true;
	}
}
