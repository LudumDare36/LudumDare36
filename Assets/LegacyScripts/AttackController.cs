using UnityEngine;
using System.Collections;
using Xft;

public class AttackController : MonoBehaviour {
	public bool avatarIsAttacking = false;
	public SfxHandler attackSFXPlayer;
	public Weapon[] weapons;

	// Use this for initialization
	void Start () {
		avatarIsAttacking = false;
		StartCoroutine(CaptureAttackInput());
		StartCoroutine(SwitchAttackColliders());
	}


	IEnumerator SwitchAttackColliders()
	{
		while (true)
		{
			yield return new WaitForFixedUpdate();
			yield return new WaitUntil(()
				=> avatarIsAttacking == true);

			for (int i = 0; i < weapons.Length; i++)
			{
				weapons[i].GetComponentInChildren<BoxCollider>().enabled = true;
				weapons[i].GetComponent<XWeaponTrail>().enabled = true;
			}
		}
	}

	IEnumerator CaptureAttackInput()
	{
		while (true)
		{
			yield return new WaitForFixedUpdate();
			yield return new WaitUntil(()
			   => ((Input.GetMouseButtonDown(1))
					//&& (avatarIsGrounded == true)
						&& (avatarIsAttacking == false)
						));

			avatarIsAttacking = true;
			GetComponent<Animator>().
				SetTrigger("Attack" + Random.Range(1, 2));

			attackSFXPlayer.PlayAttackSFX();

			yield return new WaitForSeconds(0.666f);
			avatarIsAttacking = false;
		}
	}
}
