using UnityEngine;

namespace Gameplay.Unit.Movement
{
    //[RequireComponent(typeof(BaseUnit))]
    public class PlayerControlledMovement : BaseMovement
    {
        [SerializeField]
        private LayerMask groundLayer;

        private Vector3 playerInput = Vector3.zero;
        private Quaternion mouseRotation = Quaternion.identity;

        private void CheckInput()
        {
            playerInput = Vector3.forward * Input.GetAxis("Vertical") + Vector3.right * Input.GetAxis("Horizontal");

            /*Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;

            if (Physics.Raycast(mouseRay, out hit, 100, groundLayer.value))
            {
                Vector3 diff = hit.point - transform.position;
                diff.y = 0;

                mouseRotation = Quaternion.LookRotation(diff);
            }*/
        }

        private void Update()
        {
            //if(!isLocalPlayer)
            //    return;
            CheckInput();
            Move();
            Turn();

        }

        private void Turn()
        {
			if (playerInput != Vector3.zero)
				GetComponent<Rigidbody> ().rotation = Quaternion.LookRotation(playerInput);
        }

        private void Move()
        {
			Vector3 finalSpeed = playerInput * GetComponent<PlayerUnit>().AttributePool.GetAttribute(Gameplay.Attribute.AttributeType.MoveSpeed).CurrentValue * Time.fixedDeltaTime;
			navMeshAgent.Move(finalSpeed);
        }
    }
}

