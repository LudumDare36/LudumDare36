using System;

namespace com.ootii.Actors
{
    /// <summary>
    /// Stores information about the controller state such as
    /// angle, speed, etc. By keeping the information here, we
    /// can trace current state and previous state.
    /// </summary>
    public struct AdventureControllerState
    {
        /// <summary>
        /// Determines if there was an initial heading change. If so,
        /// we may act differently for a bit.
        /// </summary>
        public int InitialHeading;

        /// <summary>
        /// Current velocity of the character
        /// </summary>
        public float Speed;

        /// <summary>
        /// Current acceleration of the character
        /// </summary>
        public float Acceleration;

        /// <summary>
        /// Current direction of the controller which represents left(-1) to right(1)
        /// </summary>
        public float InputX;

        /// <summary>
        /// Current direction of the controller which represents forward(1) to back(-1)
        /// </summary>
        public float InputY;

        /// <summary>
        /// Relative angle (in degrees) from current heading of the avatar
        /// </summary>
        public float FromAvatarAngle;

        /// <summary>
        /// Relative angle (in degrees) from current heading of the camera
        /// </summary>
        public float FromCameraAngle;
    }
}

