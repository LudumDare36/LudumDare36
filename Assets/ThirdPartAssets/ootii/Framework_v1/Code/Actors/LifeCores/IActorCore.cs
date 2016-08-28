using UnityEngine;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Foundation for PC/NPCs that have a heart-beat and whose life needs to be managed. An
    /// actor is typically something that is alive, can be hurt, and can die.
    /// </summary>
    public interface IActorCore
    {
        /// <summary>
        /// Transform that is the actor
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Determines if the actior is alive
        /// </summary>
        bool IsAlive { get; set; }

        /// <summary>
        /// Called when the actor takes damage. This allows the actor to respond
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        /// <returns>Determines if the damage was applied</returns>
        bool OnDamaged(float rDamageValue, int rDamageType = 0, float rAttackAngle = 0f, Transform rBone = null);

        /// <summary>
        /// Tells the actor to die and triggers any effects or animations
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        void OnDeath(float rDamageValue, int rDamageType = 0, float rAttackAngle = 0f, Transform rBone = null);
    }
}
