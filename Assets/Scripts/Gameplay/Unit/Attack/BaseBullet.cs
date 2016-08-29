using UnityEngine;
using System.Collections;

namespace Gameplay.Unit.Attack
{
    public struct HitInformation
    {
        public BaseUnit Shooter;
        public BaseBullet Bullet;
        public BaseWeapon Weapon;
        public Vector3 HitPosition;
    }

    public class BaseBullet : MonoBehaviour
    {
        protected BaseWeapon baseWeapon;
        protected WeaponDefinition weaponDefinition;
        protected LayerMask layerMask;

        public virtual void Initialize(BaseWeapon targetBaseWeapon)
        {
            baseWeapon = targetBaseWeapon;
            weaponDefinition = baseWeapon.GetWeaponDefinition();
            layerMask = baseWeapon.GetWeaponLayerMask();
        }

        protected virtual void DestroyBullet(float afterSeconds)
        {
            this.StartCoroutine(DestroyBulletAfterSecondsCoroutine(afterSeconds));
        }

        private IEnumerator DestroyBulletAfterSecondsCoroutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SimplePool.Despawn(gameObject);
        }
		
        protected void ApplyEffect(IHitByBullet[] affectedObjects, Vector3 point)
        {
            for (int i = 0; i < affectedObjects.Length; i++)
            {
                IHitByBullet affectedObject = affectedObjects[i];

                HitInformation hitInfo = new HitInformation
                {
                    Weapon = baseWeapon,
                    Bullet = this,
                    Shooter = baseWeapon.Owner,
                    HitPosition = point
                };

                affectedObject.Hit(hitInfo);
            }
        }
    }
}