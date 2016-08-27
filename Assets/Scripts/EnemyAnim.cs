using UnityEngine;
using System.Collections;

public class EnemyAnim : MonoBehaviour {
  private NavMeshAgent navmeshagent;
  private Animator animator;

  // Use this for initialization
  void Start () {
    navmeshagent = GetComponent<NavMeshAgent>();
    animator = GetComponentInChildren<Animator>();
  }

  // Update is called once per frame
  void Update () {
    animator.SetFloat("vel", navmeshagent.velocity.magnitude);
  }
}
