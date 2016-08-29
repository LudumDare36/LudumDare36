using UnityEngine;
using Gameplay.Attribute;
using UnityEngine.Networking;

namespace Gameplay.Unit.Movement
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class BaseMovement : MonoBehaviour
    {
        protected NavMeshAgent navMeshAgent;
        protected BaseUnit baseUnit;

        protected float moveSpeedValue;


        private Attribute.Attribute moveSpeedAttribute;

        protected virtual void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            baseUnit = GetComponent<BaseUnit>();
        }

        public virtual void Initialize()
        {
            moveSpeedAttribute = baseUnit.AttributePool.GetAttribute(AttributeType.MoveSpeed);
            moveSpeedAttribute.OnAttributeChange += OnMoveSpeedAttributeChange;
            OnMoveSpeedAttributeChange(0, moveSpeedAttribute.CurrentValue);
        }

        private void OnDisable()
        {
            //moveSpeedAttribute.OnAttributeChange -= OnMoveSpeedAttributeChange;
        }

        protected virtual void OnMoveSpeedAttributeChange(float prevValue, float currentValue)
        {
            moveSpeedValue = currentValue;
        }
    }
}
