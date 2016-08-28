using UnityEngine;
using System.Collections;

public class movementcontroler : MonoBehaviour {
	


	
	// Update is called once per frame
	void Update () {
		transform.GetComponent<Animator>().
			SetFloat("Forward", Input.GetAxis("Vertical"));

		transform.GetComponent<Rigidbody>().MovePosition(
			(transform.position+(Camera.main.transform.forward
                * (Time.deltaTime * (Input.GetAxis("Vertical") * 9f)))));

        transform.forward
            = Camera.main.transform.forward;

        Camera.main.transform.Rotate(0f, (
            Input.GetAxis("Horizontal")*90f) * Time.deltaTime, 0f);

        Camera.main.transform.position
            = Vector3.Lerp(Camera.main.transform.position, (transform.position
                -(transform.forward*8f)+(Vector3.up*3f)), Time.deltaTime);
	}
}
