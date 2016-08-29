using UnityEngine;
using System.Collections;

public class movementcontroler : MonoBehaviour
{

	// Update is called once per frame
	void FixedUpdate ()
	{
		transform.GetComponent<Animator>().
			SetFloat("Forward", Input.GetAxis("Vertical"));

        transform.GetComponent<Rigidbody>().MovePosition(
            (transform.position + ((transform.forward 
                * ((Input.GetAxis("Vertical") * 9f)))
                    * Time.smoothDeltaTime)));

        transform.Rotate(0f, ((Input.GetAxis("Horizontal") 
            * Time.smoothDeltaTime) * 180f), 0f);
        
	}
}
