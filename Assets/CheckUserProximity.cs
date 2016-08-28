using UnityEngine;
using System.Collections;

public class CheckUserProximity : MonoBehaviour {
    public Transform userAvatar;

	// Use this for initialization
	void Start () {
        userAvatar = Camera.main.transform;
        StartCoroutine(CheckForAvatarProximity());
    }

    IEnumerator CheckForAvatarProximity()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (Vector3.Distance(transform.position,userAvatar.position)>90f)
            {
                transform.GetComponent<ParticleSystem>().Stop();

                yield return new WaitWhile(()
                    => Vector3.Distance(transform.position, userAvatar.position) > 90f);
            }
            else
            {
                transform.GetComponent<ParticleSystem>().Play();
            }
        }
    }
}
