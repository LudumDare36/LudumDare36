using UnityEngine;
using UnityEngine.Networking;

namespace Gameplay.Unit.Attack
{
    public class AimController : MonoBehaviour
    {
        private BaseWeapon currentWeapon;
        private BaseUnit baseUnit;

        public BaseWeapon CurrentWeapon
        {
            get { return currentWeapon; }
        }

        private void Start()
        {
            baseUnit = GetComponent<BaseUnit>();
            currentWeapon = GetComponentInChildren<BaseWeapon>();
            currentWeapon.Initialize(baseUnit);
        }

        private void Update()
        {
            //if(!isLocalPlayer)
            //    return;

            if (Input.GetMouseButton(0))
            {
                if(currentWeapon.IsOnCooldown())
                    return;

                currentWeapon.Shoot();
            }
        }
    }
}
