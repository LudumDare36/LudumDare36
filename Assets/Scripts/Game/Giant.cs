using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Giant : Mob
{

  public new Vector3 sleepLocalPos = Vector3.back * 0.6f;
  public new Quaternion sleepRotation = Quaternion.LookRotation(Vector3.forward);


  public override void FixedUpdate () {
    base.FixedUpdate();
    if (sleep)
      return;

    // TODO fire long range

  }
   
}
