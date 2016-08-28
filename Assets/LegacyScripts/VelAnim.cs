using UnityEngine;
using System.Collections;

public class VelAnim : MonoBehaviour {
  private Vector3 last;
  private Animator animator;

  // Use this for initialization
  void Start () {
    last = transform.position;
    animator = GetComponentInChildren<Animator>();
  }

  // Update is called once per frame
  void Update () {
    Vector3 vel = (transform.position - last) / Time.deltaTime;
    last = transform.position;
    Debug.Log("vel " + name + " " + vel.magnitude);
    animator.SetFloat("vel", vel.magnitude);
  }
}
