using UnityEngine;
using System.Collections;
using Gameplay.Unit;
using Gameplay.Unit.Movement;

public class EnemyHit : MonoBehaviour {

  public double HitPoints = 100.0;

	void Start () {
  }
	
	void Update () {
	
	}

  void OnCollisionEnter(Collision collision)
  {
    if (HitPoints <= 0) return;

    foreach (ContactPoint contact in collision.contacts)
    {
      Rigidbody rb2 = contact.otherCollider.GetComponent<Rigidbody>();
      if (!rb2) continue;
      Debug.DrawRay(contact.point, -contact.normal * collision.relativeVelocity.magnitude, Color.white);
      HitPoints -= collision.relativeVelocity.magnitude * rb2.mass;
      Debug.Log(gameObject.name + " collided, hp " + HitPoints);
      if (HitPoints <= 0 )
      {
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        if(ps) ps.enableEmission = false;

        GetComponent<NavMeshAgent>().enabled = false;
        Destroy(GetComponent<BaseEnemy>());
        Destroy(GetComponent<BaseMovement>());
        Destroy(GetComponent<PathAgentController>());
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 2000;
        rb.AddForce(collision.relativeVelocity * rb2.mass * 100, ForceMode.Impulse);
        return;
      }
    }
  }
}
