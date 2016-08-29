using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Giant : MonoBehaviour
{
  public float warmupTime = 2;
  public float chargeTime = 20;
  float timestamp;
  public GameObject shot;
  bool fired = true;

  void Start()
  {
    timestamp = Time.time;
  }

  void OnTriggerStay(Collider other)
  {
    if (other.tag == "Player")
    {
      if (timestamp + chargeTime < Time.time)
      {
        timestamp = Time.time;
        GetComponentInChildren<Animator>().Play("attack");
        fired = false;
      }

      if (timestamp + warmupTime < Time.time) {
        if (!fired)
        {
          GameObject s = (GameObject)Instantiate(shot, transform.position + transform.forward * 7, Quaternion.identity);
          s.GetComponent<Rigidbody>().velocity = transform.forward * 100;
          Destroy(s, 0.25f);
          fired = true;
        }

      }

    }
  }

  void OnDestroy()
  {
    GameObject.FindGameObjectWithTag("Player").GetComponent<Hero>().weaponCount = 2;
  }


}
