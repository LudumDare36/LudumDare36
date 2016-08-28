using System;
using UnityEngine;
using System.Collections;

namespace Gameplay.Unit.Movement
{
    public class PathAgentController : BaseMovement
    {
        public delegate void PathResultDelegate(Vector3 startPosition, Vector3 endPosition);

        public event PathResultDelegate OnReachDestination;
        public event PathResultDelegate OnFail;

        private int areaMask;
        private bool reachDestination;

        private Vector3 startPosition;
        private Vector3 destinationPosition;

		private Coroutine checkDestination;

        protected override void Awake()
        {
            base.Awake();
            areaMask = navMeshAgent.areaMask;
        }

        protected override void OnMoveSpeedAttributeChange(float prevValue, float currentValue)
        {
            base.OnMoveSpeedAttributeChange(prevValue, currentValue);
            navMeshAgent.speed = moveSpeedValue;
        }

        public void Stop()
        {
            navMeshAgent.Stop();

            if (checkDestination != null)
            {
                StopCoroutine(checkDestination);
                checkDestination = null;
            }
        }

        public void SetDestination(Vector3 targetDestination)
        {
            NavMeshHit navMeshHit;
            NavMesh.SamplePosition(targetDestination, out navMeshHit, 2.0f, areaMask);

            if (!navMeshHit.hit)
				return;

            reachDestination = false;

            startPosition = navMeshHit.position;
            navMeshAgent.SetDestination(navMeshHit.position);

            if (checkDestination != null)
            {
                StopCoroutine(checkDestination);
                checkDestination = null;
            }

            checkDestination = StartCoroutine(CheckDestination());
        }

        private IEnumerator CheckDestination()
        {
            while (!reachDestination)
            {
                if(!enabled)
                    continue;

				if (!navMeshAgent.pathPending)
                {
					if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                    {
                        if (navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete)
                        {
                            if (!navMeshAgent.hasPath || Mathf.Abs(navMeshAgent.velocity.sqrMagnitude) < float.Epsilon)
                            {
                                reachDestination = true;

                                destinationPosition = transform.position;
                                DispatchReachPosition();
                            }
                        }
                        else
                        {
							reachDestination = true;

							DispatchFail();
                        }
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void WarpPosition(Vector3 targetPosition)
        {
            NavMeshHit navMeshHit;
            NavMesh.SamplePosition(targetPosition, out navMeshHit, 2.0f, areaMask);

            if (!navMeshHit.hit)
                return;

            navMeshAgent.Warp(navMeshHit.position);
        }

        private void DispatchReachPosition()
        {
            if (OnReachDestination != null)
                OnReachDestination(startPosition, destinationPosition);
        }

        private void DispatchFail()
        {
            if (OnFail != null)
                OnFail(startPosition, transform.position);
        }
    }
}