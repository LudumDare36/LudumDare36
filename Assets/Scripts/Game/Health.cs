using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {

  public float health;

	// Use this for initialization
	void Start () {
    health = GetComponent<Rigidbody>().mass;
  }
	
	// Update is called once per frame
	void Update () {
	
	}

  void OnCollisionEnter(Collision collision)
  {
    if (collision.rigidbody) {
      health -= Mathf.Max(0, collision.relativeVelocity.magnitude/2.0f - 10);
      Debug.Log(name + " health " + health + "("+ collision.relativeVelocity.magnitude + ")");
      if (health <= 0)
      {
        Destroy(transform.gameObject);
      }
    }
  }


}
