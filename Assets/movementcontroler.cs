using UnityEngine;
using System.Collections;

public class movementcontroler : MonoBehaviour {
	


	
	// Update is called once per frame
	void Update () {
		transform.GetComponent<Animator>().
			SetFloat("Forward", Input.GetAxis("Vertical"));

		transform.GetComponent<Rigidbody>().MovePosition(
			(transform.position+(Camera.main.transform.forward
                *(Time.deltaTime * Input.GetAxis("Vertical")))));
	}
}
