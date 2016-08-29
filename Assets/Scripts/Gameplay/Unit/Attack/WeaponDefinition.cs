using System;
using UnityEngine;

namespace Gameplay.Unit.Attack
{
    [Serializable]
    public class WeaponDefinition
    {
        [SerializeField]
        private float cooldown;
        [SerializeField]
        private int damage;
        [SerializeField]
        private float range;

        public WeaponDefinition(float cooldown, float range, int damage)
        {
            this.cooldown = cooldown;
            this.range = range;
            this.damage = damage;
        }

        public float GetCooldown()
        {
            return cooldown;
        }

        public float GetRange()
        {
            return range;
        }

		public void ChangeDamage(int newDamage)
		{
			damage = newDamage;
		}

        public int GetDamage()
        {
            return damage;
        }
    }
}