using UnityEngine;
using System.Collections;
using Gameplay.Attribute;

namespace Gameplay.Unit.Attack
{
    public class BaseEnemyAttack : MonoBehaviour
    {

        [SerializeField]
        private TriggerVolume attackTriggerVolume;
        [SerializeField]
        private float attackCooldown = 0.7f;
        [SerializeField]
        private float damage;

        private BaseEnemy enemy;
        private float attackTime;
        private Attribute.Attribute playerHealth;
		private Attribute.Attribute playerArmor;

        private void Awake()
        {
            enemy = GetComponent<BaseEnemy>();
            attackTriggerVolume.OnTriggerStayEvent += OnAttackTriggerVolumeStay;
            attackTriggerVolume.OnTriggerExitEvent += OnAttackTriggerVolumeExit;
        }

		private void Start()
		{
			damage = enemy.AttributePool.GetAttribute (AttributeType.Power).CurrentValue;
		}

        private void OnDestroy()
        {
            attackTriggerVolume.OnTriggerStayEvent -= OnAttackTriggerVolumeStay;
            attackTriggerVolume.OnTriggerExitEvent -= OnAttackTriggerVolumeExit;
        }

        private void OnAttackTriggerVolumeStay(TriggerVolume volume, Collider collider1)
        {
			if (collider1.tag == "Player") {
				PlayerUnit playerUnit = collider1.GetComponent<PlayerUnit> ();
				playerHealth = playerUnit.AttributePool.GetAttribute (AttributeType.Health);
				playerArmor = playerUnit.AttributePool.GetAttribute (AttributeType.Armor);
				ExecuteAttack ();
			}
        }

        private void ExecuteAttack()
        {
            if(Time.time < attackTime)
                return;

            attackTime = Time.time + attackCooldown;

			float hitDamage = damage - playerArmor.CurrentValue;
			hitDamage = Mathf.Clamp (hitDamage, 0, playerHealth.CurrentValue);
			
			playerHealth.ChangeValue(-hitDamage);
        }

        private void OnAttackTriggerVolumeExit(TriggerVolume volume, Collider collider1)
        {
            //enemy.ChangeStateTo(BehaviorState.Patrolling);
        }
    }
}
