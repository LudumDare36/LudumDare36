using System.Collections;
using Gameplay.Unit.Attack;
using Gameplay.Unit.Movement;
using UnityEngine;

namespace Gameplay.Unit
{
    [RequireComponent(typeof (PathAgentController))]
    public class BaseEnemy : BaseUnit
    {
        public delegate void BaseEnemyDelegate(BaseEnemy enemy);

        public event BaseEnemyDelegate EnemyDieEvent;

        [SerializeField]
        private TriggerVolume sightTriggerVolume;

        private Rigidbody rigidbody;
        private PlayerUnit currentTarget;
        private PathAgentController pathAgentController;
		private float minimumPosotionRange = -35.0f;
		private float maximumPosotionRange = 35.0f;
      public BehaviorState startState = BehaviorState.Idle;
      private BehaviorState state = BehaviorState.Idle;
        private Coroutine pushRoutine;

        protected override void Awake()
        {
            base.Awake();
            pathAgentController = GetComponent<PathAgentController>();
            rigidbody = GetComponent<Rigidbody>();

            pathAgentController.OnReachDestination += OnReachDestination;
			pathAgentController.OnFail += OnFail;

            sightTriggerVolume.OnTriggerEnterEvent += OnSightTriggerVolumeEnter;
            sightTriggerVolume.OnTriggerExitEvent += OnSightTriggerVolumeExit;
			sightTriggerVolume.OnTriggerStayEvent += OnSightTriggerVolumeStay;
        }

		public override void Initialize()
		{
			base.Initialize ();
			InitializeBehavior ();
		}

		public override void Initialize(int targetHealth, int targetPower, int targetArmor, int targetMoveSpeed, int targetMoney)
		{
			base.Initialize (targetHealth, targetPower, targetArmor, targetMoveSpeed, targetMoney);
			InitializeBehavior ();
		}

		protected void InitializeBehavior()
		{
			ChangeStateTo(BehaviorState.Idle);
			sightTriggerVolume.ClearContainingList ();
			ChangeStateTo(startState);
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();
            pathAgentController.OnReachDestination -= OnReachDestination;
			pathAgentController.OnFail -= OnFail;

            sightTriggerVolume.OnTriggerEnterEvent -= OnSightTriggerVolumeEnter;
            sightTriggerVolume.OnTriggerExitEvent -= OnSightTriggerVolumeExit;
			sightTriggerVolume.OnTriggerStayEvent -= OnSightTriggerVolumeStay;
        }

    private void OnSightTriggerVolumeExit(TriggerVolume volume, Collider collider)
    {
      if (collider.tag == "Player")
      {
          currentTarget = null;
        ChangeStateTo(BehaviorState.Patrolling);
      }
    }

    private void OnSightTriggerVolumeEnter(TriggerVolume volume, Collider collider)
        {
			if (collider.tag == "Player")
			{
				currentTarget = collider.GetComponent<PlayerUnit> ();
				ChangeStateTo (BehaviorState.SeekingTarget);
			}
        }

		private void OnSightTriggerVolumeStay(TriggerVolume volume, Collider collider)
		{
			if (currentTarget != null)
				return;
			
			if (collider.tag != "Player")
				return;
			else
			{
				currentTarget = collider.GetComponent<PlayerUnit> ();
				ChangeStateTo (BehaviorState.SeekingTarget);
			}
		}

        public void ChangeStateTo(BehaviorState targetState)
        {
            if (state == BehaviorState.Idle && targetState == BehaviorState.Patrolling)
            {
                SeekNewPosition();
            }
            else if (state == BehaviorState.Patrolling && targetState == BehaviorState.Attacking)
            {
                pathAgentController.Stop();
            }
            state = targetState;
        }

        private void SeekNewPosition()
        {
			Vector3 randomPosition = new Vector3(Random.Range(minimumPosotionRange, maximumPosotionRange), 0, Random.Range(minimumPosotionRange, maximumPosotionRange));
            pathAgentController.SetDestination(randomPosition);
        }

        private void OnReachDestination(Vector3 startPosition, Vector3 endPosition)
        {
            if (state == BehaviorState.Patrolling)
                SeekNewPosition();
        }

        public override void Hit(HitInformation hitInformation)
        {
            base.Hit(hitInformation);

            if(!gameObject.activeInHierarchy)
                return;

			pathAgentController.SetDestination (hitInformation.Shooter.transform.position);

            if (pushRoutine != null)
            {
                StopCoroutine(pushRoutine);
                pushRoutine = null;
            }
            //pushRoutine = StartCoroutine(PushBackRoutine(hitInformation));
        }

        private IEnumerator PushBackRoutine(HitInformation hitInformation)
        {
            Vector3 direction = hitInformation.HitPosition - hitInformation.Shooter.transform.position;
            rigidbody.isKinematic = false;
            rigidbody.AddForce(direction.normalized* 30, ForceMode.Impulse);
            yield return new WaitForSeconds(0.05f);
            rigidbody.isKinematic = true;
       }

        protected override void Die()
        {
            base.Die();
            SimplePool.Despawn(this.gameObject);
            DispatchEnemyDie();
        }

        private void DispatchEnemyDie()
        {
			if (EnemyDieEvent != null)
                EnemyDieEvent(this);
        }

        private void Update()
        {
            if (state == BehaviorState.SeekingTarget)
            {
				try
				{
					pathAgentController.SetDestination(currentTarget.transform.position);
				}
				catch
				{
					currentTarget = null;
					ChangeStateTo(BehaviorState.Patrolling);
					SeekNewPosition ();
				}
            }
				
			if (state == BehaviorState.Patrolling && GetComponent<NavMeshAgent> ().velocity.magnitude < 0.6)
			{
				ChangeStateTo(BehaviorState.Patrolling);
				SeekNewPosition ();
			}
        }

		private void OnFail(Vector3 startPosition, Vector3 currentPosition)
		{
			SeekNewPosition ();
		}
    }
}
