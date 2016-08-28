using System;

namespace com.ootii.Cameras
{
	/// <summary>
	/// Defines the different camera states that are available
	/// </summary>
	public class EnumCameraMode
	{
		/// <summary>
		/// The camera follows the player by orbiting to 
		/// thier view. In this mode, the avatar will actually
		/// orbit around the camera when moving left or right
		/// </summary>
		public const int THIRD_PERSON_FOLLOW = 0;

		/// <summary>
		/// The camera keeps the specified position in relation
		/// to the avatar. When the avatar moves left or right, 
		/// the camera moves left or right.
		/// </summary>
		public const int THIRD_PERSON_FIXED = 1;

        /// <summary>
        /// Camera behaves as if its positioned at the eyes of the avatar.
        /// </summary>
        public const int FIRST_PERSON = 2;
		public const int FIRST_PERSON_FIXED = 2;
	}
}

