using UnityEngine;
using Xft;

public class Weapon : MonoBehaviour
{
    void OnTriggerEnter(Collider _col)
    {
        if (_col.transform.root.gameObject
                != transform.root.gameObject
                    && _col.GetComponent<HealthSystem>()
                            != null)
        {
            _col.GetComponent<HealthSystem>().ApplyDamage();
            GetComponent<BoxCollider>().enabled = false;
            GetComponent<XWeaponTrail>().enabled = false;
        }
    }
}