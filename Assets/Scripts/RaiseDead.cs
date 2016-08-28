using UnityEngine;
using System.Collections;
using Gameplay.Unit;

public class RaiseDead : MonoBehaviour {

	// Use this for initialization
	void Start () {
    
  }

  // Update is called once per frame
  void Update () {
	
	}

  void OnTriggerEnter(Collider other)
  {
    BaseEnemy be = other.GetComponent<BaseEnemy>();
    if(be) be.ChangeStateTo(BehaviorState.Patrolling);
  }

}
