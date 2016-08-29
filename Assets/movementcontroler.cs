using UnityEngine;
using System.Collections;

public class movementcontroler : MonoBehaviour
{
	Transform avatarGyro;
    Transform avatarForwardGyro;
    Transform viewPortGyro;

	void Start()
	{
		avatarGyro = transform.root.
			GetComponentInChildren<Gyroscope>().transform;

		viewPortGyro = Camera.main.transform.root.
			GetComponentInChildren<Gyroscope>().transform;

        avatarForwardGyro   = transform.root.
            GetComponentInChildren<ForwardGyro>().transform;
    }

	// Update is called once per frame
	void FixedUpdate ()
	{
		transform.GetComponent<Animator>().
			SetFloat("Forward", Mathf.Abs(Input.GetAxis("Vertical")
				+ Input.GetAxis("Horizontal")));


		if (Input.GetAxis("Vertical") != 0 
			|| Input.GetAxis("Horizontal") != 0)
		{
			transform.GetComponent<Rigidbody>().
				MovePosition(transform.position + (avatarGyro.forward * 9f * Time.smoothDeltaTime));
		}

        transform.forward
			= Vector3.Lerp(transform.forward,(
                avatarForwardGyro.forward
                //viewPortGyro.transform.forward 
                    * Input.GetAxis("Vertical"))
                        + (avatarForwardGyro.right * Input.GetAxis("Horizontal")),
                            Time.smoothDeltaTime * 18f);

    }
}
