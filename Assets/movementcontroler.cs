using UnityEngine;
using System.Collections;

public class movementcontroler : MonoBehaviour
{
    Transform avatarGyro;
    Transform viewPortGyro;

    void Start()
    {
        avatarGyro = transform.root.
            GetComponentInChildren<Gyroscope>().transform;

        viewPortGyro = Camera.main.transform.root.
            GetComponentInChildren<Gyroscope>().transform;
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



        if (Input.GetAxis("Horizontal") != 0
                && Input.GetAxis("Vertical") == 0)
        {
            transform.forward
                = Vector3.Lerp(transform.forward,
                    (Camera.main.transform.right * Input.GetAxis("Horizontal")),
                        Time.smoothDeltaTime * 18f);
        }

        if (Input.GetAxis ("Vertical") > 0 
                && Input.GetAxis("Horizontal") == 0)
        {
            transform.rotation
            = new Quaternion(transform.rotation.x,
                Mathf.Lerp(transform.rotation.y, Camera.main.transform.rotation.y, Time.smoothDeltaTime * 18f),
                    transform.rotation.z, transform.rotation.w);
        }
        
            
    }
}
