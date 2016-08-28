﻿using System;
using System.Collections;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Attributes;
using com.ootii.Helpers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Determines the capabilities of the actor and provides access to
    /// core specific functionality.
    /// </summary>
    public class ActorCore : MonoBehaviour, ILifeCore, IActorCore
    {
        /// <summary>
        /// GameObject that owns the IAttributeSource we really want
        /// </summary>
        public GameObject _AttributeSourceOwner = null;
        public GameObject AttributeSourceOwner
        {
            get { return _AttributeSourceOwner; }
            set { _AttributeSourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of the attributes that control our health
        /// </summary>
        [NonSerialized]
        protected IAttributeSource mAttributeSource = null;
        public IAttributeSource AttributeSource
        {
            get { return mAttributeSource; }
            set { mAttributeSource = value; }
        }

        /// <summary>
        /// Transform that is the actor
        /// </summary>
        public Transform Transform
        {
            get { return gameObject.transform; }
        }

        /// <summary>
        /// Determines if the actor is actually alive
        /// </summary>
        public bool _IsAlive = true;
        public virtual bool IsAlive
        {
            get { return _IsAlive; }
            set { _IsAlive = value; }
        }

        /// <summary>
        /// Attribute identifier that represents the health attribute
        /// </summary>
        public string _HealthID = "HEALTH";
        public string HealthID
        {
            get { return _HealthID; }
            set { _HealthID = value; }
        }

        /// <summary>
        /// Motion name to use when damage is taken
        /// </summary>
        public string _DamagedMotion = "Bow_Damaged";
        public string DamagedMotion
        {
            get { return _DamagedMotion; }
            set { _DamagedMotion = value; }
        }

        /// <summary>
        /// Motion name to use when death occurs
        /// </summary>
        public string _DeathMotion = "Bow_Death";
        public string DeathMotion
        {
            get { return _DeathMotion; }
            set { _DeathMotion = value; }
        }

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected void Awake()
        {
            // Object that will provide access to attributes
            if (_AttributeSourceOwner != null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(_AttributeSourceOwner);
            }

            // If the input source is still null, see if we can grab a local input source
            if (AttributeSource == null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(gameObject);
                if (AttributeSource != null) { _AttributeSourceOwner = gameObject; }
            }
        }

        /// <summary>
        /// Called when the actor takes damage. This allows the actor to respond.
        /// Damage Type 0 = Physical melee
        /// Damage Type 1 = Physical ranged
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <returns>Determines if the damage was applied</returns>
        public virtual bool OnDamaged(float rDamageValue, int rDamageType = 0, float rAttackAngle = 0f, Transform rBone = null)
        {
            if (!IsAlive) { return true; }

            float lRemainingHealth = 0f;
            if (AttributeSource != null)
            {
                lRemainingHealth = AttributeSource.GetAttributeValue(HealthID) - rDamageValue;
                AttributeSource.SetAttributeValue(HealthID, lRemainingHealth);
            }

            if (lRemainingHealth <= 0f)
            {
                OnDeath(rDamageValue, rDamageType, rAttackAngle, rBone);
            }
            else
            {
                MotionController lMC = gameObject.GetComponent<MotionController>();
                if (lMC != null)
                {
                    lMC.ActivateMotion(DamagedMotion, (int)rAttackAngle);
                }
            }

            return true;
        }

        /// <summary>
        /// Tells the actor to die and triggers any effects or animations
        /// Damage Type 0 = Physical melee
        /// Damage Type 1 = Physical ranged
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        public virtual void OnDeath(float rDamageValue = 0, int rDamageType = 0, float rAttackAngle = 0f, Transform rBone = null)
        {
            IsAlive = false;

            if (AttributeSource != null)
            {
                AttributeSource.SetAttributeValue(HealthID, 0f);
            }

            StartCoroutine(InternalDeath(rDamageValue, rDamageType, rAttackAngle, rBone));
        }

        /// <summary>
        /// Coroutine to play the death animation and disable the actor after a couple of seconds
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        /// <returns></returns>
        protected virtual IEnumerator InternalDeath(float rDamageValue = 0, int rDamageType = 0, float rAttackAngle = 0f, Transform rBone = null)
        {
            MotionController lMC = gameObject.GetComponent<MotionController>();
            if (lMC != null)
            {
                // Wait for any transition to finish
                while (lMC.ActiveMotion != null && lMC.ActiveMotion.MotionLayer.AnimatorTransitionID != 0)
                {
                    yield return null;
                }

                // Trigger the death animation
                lMC.ActivateMotion(DeathMotion, (int)rAttackAngle);
                yield return new WaitForSeconds(3.5f);

                // Shut down the MC
                lMC.enabled = false;
                lMC.ActorController.enabled = false;

                Collider[] lColliders = gameObject.GetComponents<Collider>();
                for (int i = 0; i < lColliders.Length; i++)
                {
                    lColliders[i].enabled = false;
                }
            }
        }
    }
}
