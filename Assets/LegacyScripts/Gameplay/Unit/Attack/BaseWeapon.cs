using UnityEngine;


namespace Gameplay.Unit.Attack
{
    public class BaseWeapon : MonoBehaviour
    {
        [SerializeField]
        protected LayerMask hitLayerMask;
        [SerializeField]
        protected BaseBullet bullet;
        [SerializeField]
        protected int maxBulletsPreload = 10;
        [SerializeField]
        protected WeaponDefinition currentWeaponDefinition = new WeaponDefinition(0.1f, 30, 5);
        [SerializeField]
        protected Transform bulletExitPoint;

        private BaseUnit owner;
        private float lastShootTime = float.MinValue;

        public BaseUnit Owner
        {
            get { return owner; }
        }

        protected virtual void Awake()
        {
            //SimplePool.Preload(bullet.gameObject, maxBulletsPreload);
        }

        public virtual bool IsOnCooldown()
        {
            return !(lastShootTime < Time.time);
        }

        public virtual void Shoot()
        {
            lastShootTime = Time.time + currentWeaponDefinition.GetCooldown();
            //BaseBullet bulletClone = SimplePool.Spawn(bullet.gameObject).GetComponent<BaseBullet>();
            //bulletClone.transform.SetParent(transform);
			BaseBullet bulletClone = Instantiate(bullet.gameObject).GetComponent<BaseBullet>();
			bulletClone.transform.position = bulletExitPoint.position;
			bulletClone.transform.forward = bulletExitPoint.forward;
            bulletClone.Initialize(this);
        }

		public WeaponDefinition GetWeaponDefinition()
        {
            return currentWeaponDefinition;
        }

        public LayerMask GetWeaponLayerMask()
        {
            return hitLayerMask;
        }

        public Transform GetExitPoint()
        {
            return bulletExitPoint;
        }

        public void Initialize(BaseUnit baseUnit)
        {
            owner = baseUnit;
			owner.AttributePool.GetAttribute (Gameplay.Attribute.AttributeType.Power).OnAttributeChange += OnPowerChange;
			currentWeaponDefinition.ChangeDamage ((int) owner.AttributePool.GetAttribute(Gameplay.Attribute.AttributeType.Power).CurrentValue);
        }

		private void OnPowerChange (float prevValue, float currentValue)
		{
			currentWeaponDefinition.ChangeDamage ((int) currentValue);
		}
    }
}
