using UnityEngine;
using System.Collections;

public class Hero : MonoBehaviour {

  Rigidbody rigid;
  Animator anim;
  public float speed = 50;
  Vector3 vel;
  public int weaponCount = 0;
  public int weaponHand = 0;
  public int weaponPack = 0;
  int prevWeaponHand = -1;
  int prevWeaponPack = -1;

  public static readonly string[] weaponHandStr = { null, "PL porrete:pCube1", "golem_gun" };
  public static readonly string[] weaponPackStr = { null, "armas:porrete:pCube1", "PL cannon:pCylinder2" };

  void Start() {
    rigid = GetComponent<Rigidbody>();
    anim = GetComponentInChildren<Animator>();
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

    foreach (Animator anim in GetComponentsInChildren<Animator>())
    {
      anim.SetFloat("vel", rigid.velocity.magnitude);
    }

    Camera.main.transform.parent.position = transform.position;
  }

  void Update()
  {
    int attack = 0;
    if (Input.GetButton("Fire1"))
    {
      if (weaponCount >= 1)
      {
        attack = 1;
        weaponHand = 1;
        if (weaponCount >= 2)
          weaponPack = 2;
      }
    }
    else if (Input.GetButton("Fire2"))
    {
      if (weaponCount >= 2)
      {
        attack = 2;
        weaponHand = 2;
        weaponPack = 1;
      }
    }
    anim.SetInteger("attack", attack);
    UpdateWeapon();
  }

  void WeaponDisplay(string[] list, int show)
  {
    for (int j = 1; j < list.Length; j++)
    {
      GameObject root = GameObject.Find(list[j]); // FIXME transform.Find did not work
      bool enabled = show == j;
      MeshRenderer own = root.GetComponent<MeshRenderer>();
      if (own)
        own.enabled = enabled;
      foreach (MeshRenderer kid in root.GetComponentsInChildren<MeshRenderer>())
        kid.enabled = enabled;
    }
  }

  void UpdateWeapon()
  {
    if (prevWeaponHand != weaponHand || prevWeaponPack != weaponPack)
    {
      prevWeaponHand = weaponHand;
      prevWeaponPack = weaponPack;

      WeaponDisplay(weaponHandStr, weaponHand);
      WeaponDisplay(weaponPackStr, weaponPack);
    }
  }

  void OnCollisionEnter(Collision collision)
  {
    if (collision.gameObject.tag == "Skeleton")
    {
      if (weaponCount == 0) weaponCount = 1; // club
      if (weaponHand == 0) weaponHand = 1;
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
