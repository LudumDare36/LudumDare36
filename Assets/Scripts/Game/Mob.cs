using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mob : MonoBehaviour {

  public GameObject follow;
  public GameObject hunt;
  private Vector3 next;
  private float deadline;
  private float speed = 10.0f;
  public float runSpeed = 10.0f;
  public float walkSpeed = 5.0f;
  public float thickness = 0.5f;
  public float closeEnough = 2.5f;
  private Rigidbody rigid;
  private Animator anim;
  private CapsuleCollider capsule;
  private Vector3 awakeLocalPos;
  public bool sleep = true;
  private bool prevSleep;

  void Start () {
    next = transform.position;
    deadline = 0;
    anim = GetComponentInChildren<Animator>();
    rigid = GetComponent<Rigidbody>();
    capsule = GetComponent<CapsuleCollider>();
    awakeLocalPos = anim.transform.localPosition;
    UpdateSleep();
  }

  void UpdateSleep()
  {
    prevSleep = sleep;
    anim.enabled = !sleep;
    if (sleep)
    {
      rigid.isKinematic = true;
      anim.transform.localPosition = Vector3.back * 1.6f;
      transform.rotation = Quaternion.LookRotation(Vector3.up);
    }
    else
    {
      rigid.isKinematic = false;
      anim.transform.localPosition = awakeLocalPos;
    }
  }


  Vector3 Around(Vector3 pos, float min, float max = -1)
  {
    if (max == -1) max = min;
    speed = walkSpeed;
    float radius = Random.Range(min, max);
    float angle = Mathf.Lerp(0, Mathf.PI * 2, Random.value);
    return pos + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
  }

  Vector3 LastSeen(List<Vector3> path)
  {
    if (path != null)
    {
      for (int i = path.Count - 1; i > 0; i--) {
        Vector3 dir = path[i] - transform.position;
        //if (!Physics.Raycast(, dir, dir.magnitude))
        if (!Physics.CapsuleCast(transform.position, transform.position, thickness, dir.normalized, dir.magnitude))
          return path[i];
      }
    }
    return Around(transform.position, 2);
  }


  void FixedUpdate () {
    if (prevSleep != sleep) UpdateSleep();
    if (sleep)
      return;

    float time = Time.time;
    if (Vector3.Distance(transform.position, next) < 1 
      || (hunt && Vector3.Distance(hunt.transform.position, transform.position) < closeEnough) 
      || deadline < time)
    {
      deadline = time + 3;
      Vector3 goal;
      if (hunt) { speed = runSpeed; goal = hunt.transform.position; }
      else if (follow) goal = Around(follow.transform.position, 3, 10);
      else goal = Around(transform.position, 5, 20);

      if (Vector3.Distance(transform.position, goal) < closeEnough)
        next = goal;
      else {
        next = LastSeen(PathFind(transform.position, goal));
      }
    }

    Debug.DrawLine(next, next+Vector3.up*3, Color.red, 1, false);
    Vector3 dir = next - transform.position;
    float dist = dir.magnitude;
    Vector3 vel = Mathf.Clamp(dist, walkSpeed, speed) * dir.normalized;
    rigid.AddForce((vel - rigid.velocity) * rigid.mass);

    transform.rotation =
      Quaternion.Lerp(
        transform.rotation,
        Quaternion.LookRotation(dir, Vector3.up),
        0.05f
      );

    anim.SetFloat("vel", rigid.velocity.magnitude);
    
  }

  void OnTriggerEnter(Collider other)
  {
    switch (other.tag) {
      case "Player":                 hunt   = other.gameObject; deadline = 0; break;
      case "Giant":   sleep = false; follow = other.gameObject; deadline = 0; break;
    }
  }

  void OnTriggerLeave(Collider other)
  {
    switch (other.tag) {
      case "Player": hunt   = null; deadline = 0; break;
      case "Giant":  follow = null; deadline = 0; break;
    }
  }

  void OnCollisionStay(Collision collision)
  {
    // TODO if player damage

    foreach (ContactPoint contact in collision.contacts)
    {
      Debug.DrawRay(contact.point, contact.normal, Color.white);
      if (contact.otherCollider.gameObject.tag == "Wall")
        rigid.AddForce(
          (contact.normal + (next - transform.position).normalized) * 0.5f, 
          ForceMode.VelocityChange);
    }

  }

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.gameObject.tag == "Player")
    {
      sleep = false;
      anim.SetBool("attack", true);
    }
  }

  void OnCollisionExit(Collision collision)
  {
    if(collision.gameObject.gameObject.tag == "Player")
      anim.SetBool("attack", false);
  }

  List<Vector3> PathFind(Vector3 start, Vector3 goal)
  {
    Physics.queriesHitTriggers = false;

    List<List<Vector3>> queue = new List<List<Vector3>>();
    HashSet<Vector2> visited = new HashSet<Vector2>();

    int tries = 100;
    queue.Add(new List<Vector3>{ start });
    while(queue.Count > 0 && tries-->0)
    {
      // pop shortest path
      List<Vector3> path = queue[0];
      queue.RemoveAt(0);

      Vector3 last = path[path.Count - 1];

      // try to add next paths
      for (int y = -1; y <= +1; y++)
      for (int x = -1; x <= +1; x++)
      {
        Vector3 next = last + (new Vector3(x, 0, y));
        //Debug.DrawLine(last, next-new Vector3(0,0.1f,0), Color.cyan);

        Vector2 p = new Vector2(Mathf.Floor(next.x), Mathf.Floor(next.z));
        if (visited.Contains(p))
          continue;
        visited.Add(p);

        RaycastHit hitForward;
        if (!Physics.Linecast(last, next, out hitForward))
        {
          RaycastHit hitDown;
          if (Physics.Raycast(next, Vector3.down, out hitDown))
          {
            next.y = hitDown.point.y + 1; // FIXME height
            //Debug.DrawLine(last, next + new Vector3(0, 0.1f, 0), Color.cyan);
            RaycastHit hitPassage;
            Vector3 dir = next - last;
            if (!Physics.CapsuleCast(last, last, thickness, dir.normalized, out hitPassage, dir.magnitude))
            {
              List<Vector3> next_path = path.GetRange(0, path.Count);
              next_path.Add(next);
              if (Vector3.Distance(next, goal) < closeEnough) {
                for(int j=0; j< next_path.Count-1; j++)
                  Debug.DrawLine(next_path[j], next_path[j+1], Color.red, 5);
                return next_path;
              }
              for (int j = 0; j < next_path.Count - 1; j++)
                Debug.DrawLine(next_path[j], next_path[j + 1], Color.blue, 2);
              queue.Add(next_path);
            }
            else
              Debug.DrawLine(next, next + new Vector3(0,1,0.2f), Color.magenta, 2, false);
          }
          else
            Debug.DrawLine(next, next + new Vector3(0.2f, 1, 0), Color.cyan, 2, false);
        }
        else
        Debug.DrawLine(next, next + new Vector3(0.2f, 1, 0.2f), Color.red, 2, false);

      }

      queue.Sort(delegate (List<Vector3> a, List<Vector3> b) {
        return a.Count.CompareTo(b.Count) * 2 
        + (int)(Vector3.Distance(a[a.Count - 1], goal) - Vector3.Distance(b[b.Count - 1], goal));
      });
    }

    return null;
  }
   
}
