using UnityEngine;

namespace Gameplay.Attribute
{
    public class Attribute : MonoBehaviour
    {
        public delegate void AttributeDelegate(float prevValue, float currentValue);

        public event AttributeDelegate OnAttributeChange;
        public event AttributeDelegate OnAttributeReset;
        public event AttributeDelegate OnAttributeOver;

        [SerializeField] private AttributeType attributeType;

        protected float maxValue = float.MaxValue;
        protected float currentValue;


        protected AttributePool AttributePool;

        public float Percent {get { return currentValue/maxValue; } }
        public float CurrentValue {get { return currentValue; } }
        public float MaxValue {get { return maxValue; } }
        public AttributeType AttributeType { get { return attributeType; } }

        public void Initialize(float initialValue, float maxValue)
        {
            AttributePool = GetComponentInParent<AttributePool>();
            SetMaxValue(maxValue);
            SetValue(initialValue);

            DispatchResetEvent(0, initialValue);
        }

		public void SetAttributePool(AttributePool attrPool)
		{
			AttributePool = attrPool;
		}

		public void SetAttributeType (AttributeType attributeType)
		{
			this.attributeType = attributeType;
		}

        public void SetMaxValue(float targetMaxValue)
        {
            float currentPercent = Percent;
            maxValue = targetMaxValue;
            SetValue(currentValue*currentPercent);
        }

        public virtual void SetValue(float initialValue)
        {
            float bkbCurrentValue = currentValue;
            currentValue = initialValue;
            currentValue = Mathf.Clamp(currentValue, 0, maxValue);
            DispatchChangeEvent(bkbCurrentValue, currentValue);
        }

        public virtual void ChangeValue(float targetValue)
        {
            float bkbCurrentValue = currentValue;
            currentValue += targetValue;
            currentValue = Mathf.Clamp(currentValue, 0, maxValue);

            DispatchChangeEvent(bkbCurrentValue, currentValue);

            if (currentValue <= 0)
                DispatchOverEvent(bkbCurrentValue, currentValue);
        }

		public virtual void UpgradeAttribute(float targetValue)
		{
			if (attributeType.Equals(AttributeType.Health))
				maxValue += targetValue;

			ChangeValue (targetValue);
		}

        protected void DispatchResetEvent(float prevValue, float currentValue)
        {
            if (OnAttributeReset != null)
                OnAttributeReset(prevValue, currentValue);
        }

        protected void DispatchChangeEvent(float prevValue, float currentValue)
        {
            this.name = attributeType + " - " + currentValue + "/" + maxValue + " (" + Percent + ")";

            if (OnAttributeChange != null)
                OnAttributeChange(prevValue, currentValue);
        }

        protected void DispatchOverEvent(float prevValue, float currentValue)
        {
            if (OnAttributeOver != null)
                OnAttributeOver(prevValue, currentValue);
        }
    }

}

