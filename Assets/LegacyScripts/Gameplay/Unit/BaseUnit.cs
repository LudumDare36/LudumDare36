using System;
using UnityEngine;
using Gameplay.Attribute;
using Gameplay.Unit.Attack;
using Gameplay.Unit.Movement;
using UnityEngine.Networking;

namespace Gameplay.Unit
{
    [RequireComponent(typeof(BaseMovement))]
    public class BaseUnit : MonoBehaviour, IHitByBullet
    {
        private AttributePool attributePool;
        private BaseMovement baseMovement;
        protected HitInformation lastHitInformation;

        public AttributePool AttributePool
        {
            get { return attributePool; }
        }


        protected virtual void Awake()
        {
            baseMovement = GetComponent<BaseMovement>();
            attributePool = GetComponentInChildren<AttributePool>();
        }

        protected virtual void Start()
        {
            attributePool.GetAttribute(AttributeType.Health).OnAttributeOver += OnHealthOver;
			Initialize();
            baseMovement.Initialize();
        }

		//public virtual void OnStartLocalPlayer()
        //{
		//	Initialize();
        //}

        public virtual void Initialize()
        {
            attributePool.GetAttribute(AttributeType.MoveSpeed).Initialize(5, 10);
            attributePool.GetAttribute(AttributeType.Health).Initialize(100, 100);
			attributePool.GetAttribute(AttributeType.Power).Initialize(25, 1000);
			attributePool.GetAttribute(AttributeType.Armor).Initialize(0, 100);
			attributePool.GetAttribute (AttributeType.Money).Initialize (0, 1000000);
        }

		public virtual void Initialize(int targetHealth, int targetPower, int targetArmor, int targetMoveSpeed, int targetMoney)
        {
            attributePool.GetAttribute(AttributeType.MoveSpeed).Initialize(targetMoveSpeed, targetMoveSpeed);
			attributePool.GetAttribute(AttributeType.Health).Initialize(targetHealth, targetHealth);
			attributePool.GetAttribute(AttributeType.Power).Initialize(targetPower, targetPower);
			attributePool.GetAttribute(AttributeType.Armor).Initialize(targetArmor, targetArmor);
			attributePool.GetAttribute(AttributeType.Money).Initialize(targetMoney, targetMoney);
        }

        protected virtual void OnDestroy()
        {
            attributePool.GetAttribute(AttributeType.Health).OnAttributeOver -= OnHealthOver;
        }

        private void OnHealthOver(float prevValue, float currentValue)
        {
            Die();
        }

        protected virtual void Die()
        {

        }

        public virtual void Hit(HitInformation hitInformation)
        {
            lastHitInformation = hitInformation;
			int hitDamage = lastHitInformation.Weapon.GetWeaponDefinition ().GetDamage () - (int) attributePool.GetAttribute (AttributeType.Armor).CurrentValue;
			hitDamage = Mathf.Clamp (hitDamage, 0, lastHitInformation.Weapon.GetWeaponDefinition ().GetDamage ());
            attributePool.GetAttribute(AttributeType.Health)
				.ChangeValue(-hitDamage);
        }
    }
}
