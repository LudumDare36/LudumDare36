using UnityEngine;
using System.Collections;

public class Hero : MonoBehaviour {

  Rigidbody rigid;
  Animator anim;
  public float speed = 50;
  Vector3 vel;
  public int weaponCount = 0;
  public int weaponEquipped = 0;
  int prevWeaponCount = -1;
  int prevWeaponEquipped = -1;

  void Start () {
    rigid = GetComponent<Rigidbody>();
    anim = GetComponentInChildren<Animator>();
    UpdateWeapon();
  }

  void FixedUpdate() {
    vel = new Vector3(Input.GetAxis("Vertical"), 0, -Input.GetAxis("Horizontal")) * speed;

    if (vel.magnitude > 0.2)
    {
      transform.rotation =
        Quaternion.Lerp(
          transform.rotation,
          Quaternion.LookRotation(vel, Vector3.up),
          0.1f
        );
    }

    vel.y = rigid.velocity.y;
    rigid.AddForce((vel - rigid.velocity), ForceMode.VelocityChange);

    foreach(Animator anim in GetComponentsInChildren<Animator>())
    {
      anim.SetFloat("vel", rigid.velocity.magnitude);
    }

    Camera.main.transform.parent.position = transform.position;
  }

  void Update()
  {
    anim.SetInteger("attack", Input.GetButton("Fire1") ? 1 : Input.GetButton("Fire2") ? 2 : 0);
    UpdateWeapon();
  }

  void UpdateWeapon()
  {
    return;
    if (prevWeaponCount != weaponCount || prevWeaponEquipped != weaponEquipped)
    {
      prevWeaponCount = weaponCount;
      prevWeaponEquipped = weaponEquipped;
      for(int i=0; i<transform.childCount; i++)
      {
        GameObject kid = transform.GetChild(i).gameObject;
        bool on = kid.name == "hero_" + weaponEquipped + "_" + weaponCount;
        foreach (Renderer rend in kid.GetComponentsInChildren<Renderer>()) {
          rend.enabled = on;
        }
      }
    }
  }

  void OnCollisionStay(Collision collision)
  {
    foreach (ContactPoint contact in collision.contacts)
    {
      Debug.DrawRay(contact.point, contact.normal, Color.white);
      if (contact.otherCollider.gameObject.tag == "Wall")
        rigid.AddForce(
          (contact.normal + (vel - transform.position).normalized),
          ForceMode.VelocityChange);
    }

  }

}
