using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Require these components when using this script
[RequireComponent(typeof (Animator))]
[RequireComponent(typeof (CapsuleCollider))]
[RequireComponent(typeof (Rigidbody))]
public class EssentialControlScript : MonoBehaviour
{
	public float AnimationSpeed = 1.0f;
	public float TurnSpeed = 2.0f;
	public float FallHeight = 3.0f;
	public bool RunIsToggle = true;
	public bool RunAfterFall = false;
	
	private Animator anim = null;
	private Rigidbody thisRB = null;
	private AnimatorStateInfo currentBaseState;
	private GameObject thisGameObject = null;
	public GameMode Mode = GameMode.RPG;

	static int idleState = Animator.StringToHash("Idle.Idle");
	static int walkJumpState = Animator.StringToHash("Base Layer.Walk Jump");
	static int runJumpState = Animator.StringToHash("Base Layer.Run Jump");
	static int idleJumpState = Animator.StringToHash("Base Layer.Idle Jump");	
	static int fallState = Animator.StringToHash("Base Layer.Fall");

	private bool rightMouseDown = false;
	private bool leftMouseDown = false;

	void Start ()
	{
		// initialising reference variables
		thisGameObject = gameObject;
		anim = GetComponent<Animator>();
		thisRB = thisGameObject.GetComponent<Rigidbody>();
		if (anim == null)
		{
			Debug.LogError("The game object on the Character Tracker has no animator.");
			return;
		}
	}
	
	
	void Update ()
	{
		if (!Input.GetButton("Mouse 0"))
		{
			leftMouseDown = false;
		}
		if (!Input.GetButton("Mouse 1"))
		{
			rightMouseDown = false;
		}

		
		//if the user has the right mouse button down and no direction buttons pressed, direction should respond to Mouse X movement
		float s = Input.GetAxis("Strafe");
		anim.SetFloat("Strafe", s);

		//Rotate character in place by horizontal axis. choose whether to do this permanently
		float h = Input.GetAxis("Horizontal");
		if (rightMouseDown || Mode == GameMode.FPS)
		{
			anim.SetFloat("Strafe", anim.GetFloat("Strafe") + h);
		}
		else
		{
			anim.SetFloat("Direction", h);	
			thisGameObject.transform.Rotate(0.0f, h * TurnSpeed, 0.0f, Space.World);
		}

		// if the user has both mouse buttons pressed, should move forward, otherwise respond to vertical input axis			
		if (leftMouseDown && rightMouseDown)
		{
			anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), 1, Time.deltaTime * 10));
		}
		else
		{
			float v = Input.GetAxis("Vertical");
			anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), v, Time.deltaTime * 10));
		}

		anim.speed = AnimationSpeed;
		currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
		
		//do things we need to do based on what state we are in
		if (currentBaseState.nameHash == idleState)
		{
			if (Input.GetAxis("Vertical") < 0.1 || !RunAfterFall)
			{
				anim.SetBool("Run", false);
			}
		}
		else if (currentBaseState.nameHash == walkJumpState)
		{				
			anim.SetBool("Jump", false);
		}
		else if (currentBaseState.nameHash == runJumpState)
		{				
			anim.SetBool("Jump", false);
		}
		else if (currentBaseState.nameHash == idleJumpState)
		{
			anim.SetBool("Jump", false);
		}
		else if (currentBaseState.nameHash == fallState)
		{
			if (!isFalling())
			{
				//we have landed
				anim.SetBool("Fall", false);
				return; //nothing else to do this frame
			}
		}
		//first, check if we are falling
		if (isFalling())
		{
			//we are falling
			anim.SetBool("Fall", true);
			return; //return because we can't do anything while falling
		}
		
		//handle input
		if (Input.GetButtonDown("Jump"))
		{
			anim.SetBool("Jump", true);
		}

		if (RunIsToggle)
		{
			if (Input.GetButtonDown("Run"))
			{
				anim.SetBool("Run", !anim.GetBool("Run"));
			}
		}
		else
		{
			if (Input.GetButton("Run"))
			{
				anim.SetBool("Run", true);
			}
			else
			{
				anim.SetBool("Run", false);
			}
		}
	}

	private bool isFalling()
	{
		RaycastHit rayInfo;
		Vector3 raySource = new Vector3(thisGameObject.transform.position.x, thisGameObject.transform.position.y + 0.2f, thisGameObject.transform.position.z);
		Physics.Raycast(raySource, Vector3.down, out rayInfo, FallHeight);
		Debug.DrawRay(raySource, Vector3.down * FallHeight, Color.yellow);
		if (rayInfo.collider == null)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public void OnPointerDown(BaseEventData eventdata)
	{
		PointerEventData ped = eventdata as PointerEventData;
		
		if (ped.button == PointerEventData.InputButton.Left) leftMouseDown = true;
		if (ped.button == PointerEventData.InputButton.Right) rightMouseDown = true;
	}	
}
