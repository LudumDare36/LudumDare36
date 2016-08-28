﻿using UnityEngine;
using com.ootii.Helpers;
using com.ootii.Geometry;

namespace com.ootii.Actors
{
    /// <summary>
    /// The SpiderIADriver is a NavMesh based driver. While the spider animations
    /// don't use root-motion, we will set animator parameters at the end of the Update().
    /// </summary>
    public class GhostAIDriver : NavMeshDriver
    {
        /// <summary>
        /// Determines if the actor wanders from the current position or his start postion
        /// </summary>
        public bool WanderFromCurrentPosition = false;

        /// <summary>
        /// Radius in which the actor will randomly wander
        /// </summary>
        public Vector3 WanderRadius = Vector3.zero;

        /// <summary>
        /// Store the start position
        /// </summary>
        private Vector3 mStartPosition = Vector3.zero;

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            mStartPosition = transform.position;
        }

        /// <summary>
        /// Event function for when we arrive at the destination. In this instance,
        /// we're going to find a random position and move there.
        /// </summary>
        protected override void OnArrived()
        {
            bool lIsPositionValid = false;
            Vector3 lNewPosition = Vector3.zero;

            for (int i = 0; i < 20; i++)
            {
                lNewPosition = GetRandomPosition();
                if (Vector3.Distance(lNewPosition, transform.position) < 2f) { continue; }

                if (lNewPosition.x < -45 || lNewPosition.x > 45) { lNewPosition = new Vector3(12, 0, 24); }
                if (lNewPosition.z < -45 || lNewPosition.z > 45) { lNewPosition = new Vector3(12, 0, 24); }

                Collider[] lColliders = null;
                int lHits = RaycastExt.SafeOverlapSphere(lNewPosition, mActorController.OverlapRadius * 2f, out lColliders, -1, transform);

                // Ensure the position isn't someplace we can't get to
                if (lColliders == null || lHits == 0)
                {
                    lIsPositionValid = true;
                }
                else if (lHits == 1 && lColliders[0] is TerrainCollider)
                {
                    lIsPositionValid = true;
                }

                if (lIsPositionValid) { break; }
            }

            // If we have a valid position, set it
            if (lIsPositionValid)
            {
                TargetPosition = lNewPosition;
            }
        }

        /// <summary>
        /// Grabs a random
        /// </summary>
        /// <returns></returns>
        private Vector3 GetRandomPosition()
        {
            Vector3 lRandom = WanderRadius;

            float lRandomNumber = (float)NumberHelper.Randomizer.NextDouble();
            lRandom.x = (WanderRadius.x * 2f * lRandomNumber) - WanderRadius.x;

            lRandomNumber = (float)NumberHelper.Randomizer.NextDouble();
            lRandom.y = (WanderRadius.y * 2f * lRandomNumber) - WanderRadius.y;

            lRandomNumber = (float)NumberHelper.Randomizer.NextDouble();
            lRandom.z = (WanderRadius.z * 2f * lRandomNumber) - WanderRadius.z;

            return lRandom + (WanderFromCurrentPosition ? transform.position : mStartPosition);
        }
    }
}
