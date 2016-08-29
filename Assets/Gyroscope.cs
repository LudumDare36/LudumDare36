using UnityEngine;
using System.Collections;

public class Gyroscope : MonoBehaviour {
	void FixedUpdate ()
	{
		transform.localPosition
			= Vector3.zero;

		transform.rotation
			= new Quaternion(0f,transform.rotation.y,0f,transform.rotation.w);
	}
}
