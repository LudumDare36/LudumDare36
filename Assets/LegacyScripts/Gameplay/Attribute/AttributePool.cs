using UnityEngine;
using System.Collections.Generic;

namespace Gameplay.Attribute
{
    public class AttributePool : MonoBehaviour
    {
        private Dictionary<AttributeType, Attribute> attributes = new Dictionary<AttributeType, Attribute>();

        private void Awake()
        {
            Attribute[] childAttributes = transform.GetComponentsInChildren<Attribute>();
            attributes = new Dictionary<AttributeType, Attribute>(childAttributes.Length);
            for (int i = 0; i < childAttributes.Length; i++)
            {
                Attribute childAttribute = childAttributes[i];
                if (attributes.ContainsKey(childAttribute.AttributeType))
                {
					Destroy(childAttribute.gameObject);
					//Debug.Log(gameObject.transform.parent.name + ": Duplicated attribute " + childAttribute.AttributeType, this);
                    continue;
                }
                attributes.Add(childAttribute.AttributeType, childAttribute);
            }
        }

        public Attribute GetAttribute(AttributeType targetAttributeType)
        {
            if (!attributes.ContainsKey(targetAttributeType))
            {
				GameObject attributeObject = new GameObject ();
				attributeObject.transform.SetParent (transform);
				attributeObject.AddComponent<Attribute> ();
				Attribute attribute = attributeObject.GetComponent<Attribute> ();
				attribute.SetAttributeType (targetAttributeType);
				attribute.SetAttributePool (this);

				attributes.Add (targetAttributeType, attribute);
//*/
				//Debug.Log(gameObject.transform.parent.name + ": Can't found any attribute of type: " + targetAttributeType, this);
                //return null;
            }
            return attributes[targetAttributeType];
        }
    }
}
