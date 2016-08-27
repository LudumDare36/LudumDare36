using UnityEngine;
using System.Collections;

public class Sediment : MonoBehaviour {
  public enum State
  {
    PLACED,
    DROP,
    SEDIMENT
  };
  public double Delay = 1;
  public State state = State.PLACED;
  private double cooldown;

  // Use this for initialization
  void Start ()
  {
    cooldown = Time.time + Delay;
    ChangeState(state, true);
	}

  void ChangeState(State s, bool startup = false)
  {
    if (!startup)
      Debug.Log("target " + gameObject.name + " " + state + " => " + s);
    switch ((state = s))
    {
      case State.SEDIMENT:
        Destroy(GetComponent<Rigidbody>());
        gameObject.AddComponent<NavMeshObstacle>().carving = true;
        break;
    }
  }

  // Update is called once per frame
  void Update ()
  {
    if (state == State.DROP && GetComponent<Rigidbody>().IsSleeping())
      ChangeState(State.SEDIMENT);
  }

  void OnCollisionEnter(Collision collision)
  {
    if (cooldown < Time.time && state == State.PLACED)
    {
      Debug.Log("collision " + gameObject.name + " with " + collision.other.name);
      ChangeState(State.DROP);
    }
  }
}
