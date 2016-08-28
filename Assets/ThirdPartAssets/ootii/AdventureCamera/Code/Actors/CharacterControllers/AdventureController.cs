// Tim Tryzbiak - ootii, LLC

using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.Actors
{
    /// <summary>
    /// Character controller built specifically for the
    /// Adventure Camera Rig. 
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AdventureController : MonoBehaviour
    {
        /// <summary>
        /// Keeps us from having to reallocate over and over
        /// </summary>
        private static Vector3 sVector3A = new Vector3();

        /// <summary>
        /// GameObject that owns the IInputSource we really want
        /// </summary>
        public GameObject _InputSourceOwner = null;
        public GameObject InputSourceOwner
        {
            get { return _InputSourceOwner; }
            set { _InputSourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of the input that we'll use to control
        /// the character movement, rotations, and animations.
        /// </summary>
        [NonSerialized]
        protected IInputSource mInputSource = null;
        public IInputSource InputSource
        {
            get { return mInputSource; }
            set { mInputSource = value; }
        }

        /// <summary>
        /// Used to access the camera's position and rotation
        /// </summary>
        public Transform _CameraRig = null;
        public Transform CameraRig
        {
            get { return _CameraRig; }
            set { _CameraRig = value; }
        }

        /// <summary>
        /// Abstracts access to getting and setting the camera mode
        /// </summary>
        private IBaseCameraRig mCameraRig = null;
        public int CameraRigMode
        {
            get
            {
                if (mCameraRig == null) { return 0; }
                return mCameraRig.Mode;
            }

            set
            {
                if (mCameraRig == null) { return; }
                mCameraRig.Mode = value;
            }
        }

        /// <summary>
        /// Determines we'll use gravity to pull the character down
        /// </summary>
        public bool _UseGravity = true;
        public bool UseGravity
        {
            get { return _UseGravity; }
            set { _UseGravity = value; }
        }

        /// <summary>
        /// Determines if the controller can go into the stance
        /// </summary>
        public bool _MeleeStanceEnabled = true;
        public bool MeleeStanceEnabled
        {
            get { return _MeleeStanceEnabled; }
            set { MeleeStanceEnabled = value; }
        }

        /// <summary>
        /// Determines if the controller can go into the stance
        /// </summary>
        public bool _TargetingStanceEnabled = true;
        public bool TargetingStanceEnabled
        {
            get { return _TargetingStanceEnabled; }
            set { _TargetingStanceEnabled = value; }
        }

        /// <summary>
        /// Max speed multiplier to apply when in the targeting
        /// stance
        /// </summary>
        [HideInInspector]
        public float _TargetingStanceMovementSpeedMultiplier = 1.0f;
        public float TargetingStanceMovementSpeedMultiplier
        {
            get { return _TargetingStanceMovementSpeedMultiplier; }
            set { _TargetingStanceMovementSpeedMultiplier = value; }
        }

        /// <summary>
        /// The current stance of the player
        /// </summary>
        [HideInInspector]
        protected int mStance = 0;
        public int Stance
        {
            get { return mStance; }
            set { mStance = value; }
        }

        /// <summary>
        /// Animator that the controller works with
        /// </summary>
        [HideInInspector]
        protected Animator mAnimator = null;
        public Animator Animator
        {
            get { return mAnimator; }
        }

        /// <summary>
        /// The current state of the controller including speed, direction, etc.
        /// </summary>
        protected AdventureControllerState mState = new AdventureControllerState();
        public AdventureControllerState State
        {
            get { return mState; }
            set { mState = value; }
        }

        /// <summary>
        /// The previous state of the controller including speed, direction, etc.
        /// </summary>
        protected AdventureControllerState mPrevState = new AdventureControllerState();
        public AdventureControllerState PrevState
        {
            get { return mPrevState; }
            set { mPrevState = value; }
        }

        /// <summary>
        /// Angles at which we limit forward rotation
        /// </summary>
        protected float mForwardHeadingLimit = 80f;
        public float ForwardHeadingLimit
        {
            get { return mForwardHeadingLimit; }
            set { mForwardHeadingLimit = value; }
        }

        /// <summary>
        /// Angles at which we limit backward rotation
        /// </summary>
        protected float mBackwardsHeadingLimit = 50f;
        public float BackwardsHeadingLimit
        {
            get { return mBackwardsHeadingLimit; }
            set { mBackwardsHeadingLimit = value; }
        }

        /// <summary>
        /// Current animator state
        /// </summary>
        protected AnimatorStateInfo mStateInfo;

        /// <summary>
        /// Current animator transition
        /// </summary>
        protected AnimatorTransitionInfo mTransitionInfo;

        /// <summary>
        /// This state is used to gather the current state information. We don't set it
        /// in the 'mState' property until we've cleaned it up and are done with it. This
        /// property is meant to be temporary.
        /// </summary>
        protected AdventureControllerState mTempState = new AdventureControllerState();

        /// <summary>
        /// Unity character controller that is our base
        /// </summary>
        private CharacterController mCharController = null;
        public CharacterController CharController
        {
            get { return mCharController; }
        }

        /// <summary>
        /// The speed value when the trend started. This way we can
        /// measure overall acceleration or deceleration
        /// </summary>
        private float mSpeedTrendStart = 0f;

        /// <summary>
        /// The current speed trend decreasing, static, increasing (-1, 0, or 1)
        /// </summary>
        private int mSpeedTrendDirection = 0;

        /// <summary>
        /// Add a delay before we update the mecanim parameters. This way we can
        /// give a chance for things like speed to settle.
        /// </summary>
        private float mMecanimUpdateDelay = 0f;

        /// <summary>
        /// Track the last stance so we can go back to it
        /// </summary>
        private int mPrevStance = 0;

        /// <summary>
        /// Velocity created by the root motion of animation
        /// </summary>
        private Vector3 mRootMotionVelocity = Vector3.zero;

        /// <summary>
        /// Rotational velocity created by the root motion of animation
        /// </summary>
        private Quaternion mRootMotionAngularVelocity = Quaternion.identity;

        /// <summary>
        /// Accumulated velocity due to gravity's acceleration
        /// </summary>
        private Vector3 mGravitationalVelocity = Vector3.zero;

        /// <summary>
        /// Called right before the first frame update
        /// </summary>
        public void Start()
        {
            // Object that will provide access to the keyboard, mouse, etc
            if (_InputSourceOwner == null)
            {
                IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
                if (lInputSources != null && lInputSources.Length > 0)
                {
                    mInputSource = lInputSources[0];
                    _InputSourceOwner = ((MonoBehaviour)lInputSources[0]).gameObject;
                }
                else
                {
                    UnityInputSource lInputSource = Component.FindObjectOfType<UnityInputSource>();
                    if (lInputSource != null)
                    {
                        mInputSource = lInputSource;
                        _InputSourceOwner = lInputSource.gameObject;
                    }
                }
            }
            else
            {
                mInputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner);
            }

            // If the input source is still null, see if we can grab a local input source
            if (mInputSource == null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(gameObject); }

            // Grab the character controller we are tied to
            mCharController = GetComponent<CharacterController>();

            // Grab the camera rig
            if (_CameraRig == null)
            {
                Camera lCamera = Component.FindObjectOfType<Camera>();
                if (lCamera != null) { _CameraRig = lCamera.transform; }
            }

            mCameraRig = ExtractCameraRig(_CameraRig);

            // Load the animator and grab all the state info
            mAnimator = GetComponent<Animator>();
            LoadAnimatorData();
        }

        /// <summary>
        /// Called every frame to perform processing. We only use
        /// this function if it's not called by another component.
        /// </summary>
        protected void Update()
        {
            if (mAnimator == null) { return; }
            if (mCameraRig == null) { return; }
            if (Time.deltaTime == 0f) { return; }

            // Store the state we're in
            mStateInfo = mAnimator.GetCurrentAnimatorStateInfo(0);
            mTransitionInfo = mAnimator.GetAnimatorTransitionInfo(0);

            if (mCameraRig.Mode == 2)
            {
                if (mStance != 2)
                {
                    mPrevStance = mStance;
                    mStance = 2;
                }
            }
            else
            {
                if (mStance == 2)
                {
                    mStance = mPrevStance;
                }
            }

            // Determine the stance we're in
            if (mInputSource != null && mInputSource.IsPressed(KeyCode.LeftControl))
            {
                //if (mStance != 2)
                //{
                //    mPrevStance = mStance;
                //    mStance = 2;

                //    mPrevRigMode = CameraRigMode;

                //    // Start the transition process
                //    //_CameraRig.TransitionToMode(EnumCameraMode.FIRST_PERSON);
                //    CameraRigMode = EnumCameraMode.FIRST_PERSON;
                //}
            }
            else if (mStance == 2)
            {
                //mStance = mPrevStance;

                ////_CameraRig.TransitionToMode(mPrevRigMode);
                //CameraRigMode = mPrevRigMode;
            }
            else if (mInputSource != null && mInputSource.IsJustPressed(KeyCode.T))
            {
                mPrevStance = mStance;
                if (mStance == 0)
                {
                    mStance = 1;
                    ((AdventureRig)mCameraRig).AnchorOrbitsCamera = false;
                }
                else if (mStance == 1)
                {
                    mStance = 0;
                    ((AdventureRig)mCameraRig).AnchorOrbitsCamera = true;
                }

                mCameraRig.Mode = 0;
            }

            // Grab the direction and speed of the input relative to our current heading
            StickToWorldspace(this.transform, _CameraRig.transform, ref mTempState);

            // Ensure some of the other values are set correctly
            mTempState.Acceleration = mState.Acceleration;
            mTempState.InitialHeading = mState.InitialHeading;

            // Ranged movement allows for slow forward, backwards, and strafing
            if (mStance == 2)
            {
                //CameraRigMode = EnumCameraMode.FIRST_PERSON;

                mTempState.Speed *= _TargetingStanceMovementSpeedMultiplier;

                // Change our heading if needed
                if (mTempState.Speed == 0)
                {
                    if (IsInBackwardsState)
                    {
                        mTempState.InitialHeading = 2;
                    }
                    else
                    {
                        mTempState.InitialHeading = 0;
                    }
                }
                else if (mTempState.Speed != 0 && mState.Speed == 0)
                {
                    float lInitialAngle = Mathf.Abs(mTempState.FromCameraAngle);
                    if (lInitialAngle < mForwardHeadingLimit) { mTempState.InitialHeading = 0; }
                    else if (lInitialAngle > 180f - mBackwardsHeadingLimit) { mTempState.InitialHeading = 2; }
                    else { mTempState.InitialHeading = 1; }
                }

                // Ensure we're always facing forward
                //mYAxisRotationAngle = NumberHelper.GetHorizontalAngle(transform.forward, _CameraRig.transform.forward);
                //mTempState.FromAvatarAngle = 0f;


            }
            // Combat movement allows for forward, backwards, strafing, and pivoting
            else if (mStance == 1)
            {
                // Determine our initial heading
                if (mTempState.Speed == 0)
                {
                    if (IsInBackwardsState)
                    {
                        mTempState.InitialHeading = 2;
                    }
                    else
                    {
                        mTempState.InitialHeading = 0;
                    }
                }
                else if (mTempState.Speed != 0 && mState.Speed == 0)
                {
                    float lInitialAngle = Mathf.Abs(mTempState.FromCameraAngle);
                    if (lInitialAngle < mForwardHeadingLimit) { mTempState.InitialHeading = 0; }
                    else if (lInitialAngle > 180f - mBackwardsHeadingLimit) { mTempState.InitialHeading = 2; }
                    else { mTempState.InitialHeading = 1; }
                }

                // Ensure if we've been heading forward that we don't allow the
                // avatar to rotate back facing the player
                if (mTempState.InitialHeading == 0)
                {
                    //CameraRigMode = EnumCameraMode.THIRD_PERSON_FOLLOW;

                    // Force the input to make us go forwards
                    if (mTempState.Speed > 0.1f && (mTempState.FromCameraAngle < -90 || mTempState.FromCameraAngle > 90))
                    {
                        mTempState.InputY = 1;
                    }

                    // If no forward rotation is allowed, this is easy
                    if (mForwardHeadingLimit == 0f)
                    {
                        mTempState.FromAvatarAngle = 0f;
                    }
                    // Respect the foward rotation limits
                    else
                    {
                        // Test if our rotation reaches the max from the camera. We use the camera since
                        // the avatar itself rotates and this limit is relative.
                        if (mTempState.FromCameraAngle < -mForwardHeadingLimit) { mTempState.FromCameraAngle = -mForwardHeadingLimit; }
                        else if (mTempState.FromCameraAngle > mForwardHeadingLimit) { mTempState.FromCameraAngle = mForwardHeadingLimit; }

                        // If we have reached a limit, we need to adjust the avatar angle
                        if (Mathf.Abs(mTempState.FromCameraAngle) == mForwardHeadingLimit)
                        {
                            // Flip the angle if we're crossing over the axis
                            if (Mathf.Sign(mTempState.FromCameraAngle) != Mathf.Sign(mState.FromCameraAngle))
                            {
                                mTempState.FromCameraAngle = -mTempState.FromCameraAngle;
                            }

                            // Only allow the avatar to rotate the heading limit, taking into account the angular
                            // difference between the camera and the avatar
                            mTempState.FromAvatarAngle = mTempState.FromCameraAngle + NumberHelper.GetHorizontalAngle(transform.forward, _CameraRig.transform.forward);
                        }
                    }
                }
                else if (mTempState.InitialHeading == 2)
                {
                    //CameraRigMode = EnumCameraMode.THIRD_PERSON_FIXED;

                    // Force the input to make us go backwards
                    if (mTempState.Speed > 0.1f && (mTempState.FromCameraAngle > -90 && mTempState.FromCameraAngle < 90))
                    {
                        mTempState.InputY = -1;
                    }

                    // Ensure we don't go beyond our boundry
                    if (mBackwardsHeadingLimit != 0f)
                    {
                        float lBackwardsHeadingLimit = 180f - mBackwardsHeadingLimit;

                        // Test if our rotation reaches the max from the camera. We use the camera since
                        // the avatar itself rotates and this limit is relative.
                        if (mTempState.FromCameraAngle <= 0 && mTempState.FromCameraAngle > -lBackwardsHeadingLimit) { mTempState.FromCameraAngle = -lBackwardsHeadingLimit; }
                        else if (mTempState.FromCameraAngle >= 0 && mTempState.FromCameraAngle < lBackwardsHeadingLimit) { mTempState.FromCameraAngle = lBackwardsHeadingLimit; }

                        // If we have reached a limit, we need to adjust the avatar angle
                        if (Mathf.Abs(mTempState.FromCameraAngle) == lBackwardsHeadingLimit)
                        {
                            // Only allow the avatar to rotate the heading limit, taking into account the angular
                            // difference between the camera and the avatar
                            mTempState.FromAvatarAngle = mTempState.FromCameraAngle + NumberHelper.GetHorizontalAngle(transform.forward, _CameraRig.transform.forward);
                        }

                        // Since we're moving backwards, we need to flip the movement angle.
                        // If we're not moving and simply finishing an animation, we don't
                        // want to rotate at all.
                        if (mTempState.Speed == 0)
                        {
                            mTempState.FromAvatarAngle = 0f;
                        }
                        else if (mTempState.FromAvatarAngle <= 0)
                        {
                            mTempState.FromAvatarAngle += 180f;
                        }
                        else if (mTempState.FromAvatarAngle > 0)
                        {
                            mTempState.FromAvatarAngle -= 180f;
                        }
                    }
                }
                else if (mTempState.InitialHeading == 1)
                {
                    //CameraRigMode = EnumCameraMode.THIRD_PERSON_FIXED;

                    // Move out of the sidestep if needed
                    if (mTempState.InputY > 0.1)
                    {
                        mTempState.InitialHeading = 0;
                    }
                    else if (mTempState.InputY < -0.1)
                    {
                        mTempState.InitialHeading = 2;
                    }

                    // We need to be able to rotate our avatar so it's facing
                    // in the direction of the camera
                    if (mTempState.InitialHeading == 1)
                    {
                        mTempState.FromCameraAngle = 0f;
                        mTempState.FromAvatarAngle = mTempState.FromCameraAngle + NumberHelper.GetHorizontalAngle(transform.forward, _CameraRig.transform.forward);
                    }
                }
            }

            // Determine the acceleration. We test this agains the 'last-last' speed so
            // that we are averaging out one frame.
            //mLastAcceleration = mAcceleration;
            mPrevState.Acceleration = mState.Acceleration;

            // Determine the trend so we can figure out acceleration
            if (mTempState.Speed == mState.Speed)
            {
                if (mSpeedTrendDirection != 0)
                {
                    mSpeedTrendDirection = 0;
                }
            }
            else if (mTempState.Speed < mState.Speed)
            {
                if (mSpeedTrendDirection != 1)
                {
                    mSpeedTrendDirection = 1;
                    if (mMecanimUpdateDelay <= 0f) { mMecanimUpdateDelay = 0.2f; }
                }

                // Acceleration needs to stay consistant for mecanim
                mTempState.Acceleration = mTempState.Speed - mSpeedTrendStart;
            }
            else if (mTempState.Speed > mState.Speed)
            {
                if (mSpeedTrendDirection != 2)
                {
                    mSpeedTrendDirection = 2;
                    if (mMecanimUpdateDelay <= 0f) { mMecanimUpdateDelay = 0.2f; }
                }

                // Acceleration needs to stay consistant for mecanim
                mTempState.Acceleration = mTempState.Speed - mSpeedTrendStart;
            }

            // Shuffle the states to keep us from having to reallocated
            AdventureControllerState lTempState = mPrevState;
            mPrevState = mState;
            mState = mTempState;
            mTempState = lTempState;

            // Apply the movement and rotation
            ApplyMovement();
            ApplyRotation();

            // Delay a bit before we update the speed if we're accelerating
            // or decelerating.
            mMecanimUpdateDelay -= Time.deltaTime;
            if (mMecanimUpdateDelay <= 0f)
            {
                mAnimator.SetFloat("Speed", mState.Speed); //, 0.05f, Time.deltaTime);

                mSpeedTrendStart = mState.Speed;
            }

            // Update the direction relative to the avatar
            mAnimator.SetFloat("Avatar Direction", mState.FromAvatarAngle);

            // At this point, we never use angular speed. Rotation is done
            // in the ApplyRotation() function. Angular speed currently only effects
            // locomotion.
            mAnimator.SetFloat("Angular Speed", 0f);

            // The stance determins if we're in exploration or combat mode.
            mAnimator.SetInteger("Stance", mStance);

            // The direction from the camera
            mAnimator.SetFloat("Camera Direction", mState.FromCameraAngle); //, 0.05f, Time.deltaTime);

            // The raw input from the UI
            mAnimator.SetFloat("Input X", mState.InputX); //, 0.25f, Time.deltaTime);
            mAnimator.SetFloat("Input Y", mState.InputY); //, 0.25f, Time.deltaTime);
        }

        /// <summary>
        /// Determines the actual position of the avatar based on root motion,
        /// gravity, and any custom velocity.
        /// </summary>
        public void ApplyMovement()
        {
            // Using Move doesn't apply gravity. We need to do this on our own.
            // Velocity caused by gravity increases due to gravitational acceleration.
            // We'll accumulate that velocity here.
            if (mCharController.isGrounded)
            {
                mGravitationalVelocity = (UnityEngine.Physics.gravity * Time.deltaTime);
            }
            else
            {
                mGravitationalVelocity += (UnityEngine.Physics.gravity * Time.deltaTime);
            }

            // Velocity from root motion is applied, but can be modified first
            Vector3 lVelocity = (transform.rotation * mRootMotionVelocity);

            // Apply the accumulation of the gravitational velocity
            if (_UseGravity) { lVelocity += mGravitationalVelocity; }

            // Use the new velocity to move the avatar. We're going to counter-act
            // the controller's gravity so we can manage it ourselves
            mCharController.Move(lVelocity * Time.deltaTime);
        }

        /// <summary>
        /// Determines the rotation of the avatar based on root motion,
        /// input direction, and any custom rotation.
        /// </summary>
        public void ApplyRotation()
        {
            // First apply the root motion rotation. Before we apply it, we need
            // to move it from a velocity to a rotation delta
            Quaternion lRMRotation = mRootMotionAngularVelocity;
            lRMRotation.x *= Time.deltaTime;
            lRMRotation.y *= Time.deltaTime;
            lRMRotation.z *= Time.deltaTime;
            lRMRotation.w *= Time.deltaTime;
            transform.rotation *= lRMRotation;

            // When in targeting mode, we move slower pivot in place
            //if (mInputSource != null && mInputSource.IsPressed(KeyCode.LeftControl))
            if (mStance == 2)
            {
                // This is actually overkill as the camera will force the rotation
                // of the avatar at the end of it's LateUpdate()
                //float lAngularSpeed = Mathf.Lerp(0, mYAxisRotationAngle * 5.0f, 1.0f);
                //transform.Rotate(transform.up, lAngularSpeed * Time.deltaTime);
                if (mInputSource.IsViewingActivated)
                {
                    float lYaw = mInputSource.ViewX * 360f * Time.deltaTime;
                    transform.Rotate(transform.up, lYaw);
                }
            }
            // In any other mode, we have more freedome to rotate the avatar
            // outside the direction of the camera
            else
            {
                // If we're moving forward, rotate the avatar to face in the direction of the camera.
                if (IsInForwardState)
                {
                    if (mForwardHeadingLimit == 0)
                    {
                        Vector3 lForward = _CameraRig.transform.forward;
                        lForward.y = 0;
                        lForward.Normalize();
                    }
                    else
                    {
                        // Ensure we're not in a pivot. The pivot will do the rotating. The exception is when
                        // we're coming out of a pivot.
                        if (!IsInPivotTransitionState)
                        {
                            bool lIsOrbiting = false; // _CameraRig.IsOrbiting;
                            if (lIsOrbiting && mState.InputY > 0.5f)
                            {
                                // If the player is orbiting and moving forward,
                                // force the avatar towards the direction of the stick
                                transform.Rotate(transform.up, mState.FromAvatarAngle);
                            }
                            else
                            {
                                // Augment the root motion rotation by adding some rotation if the player is moving to the right and the
                                // avatar is turning right or if the player is moving to the left and the avatar is turning left.
                                //float lAngularSpeed = Mathf.Lerp(0, mState.FromAvatarAngle * 5f, 0.9f);
                                transform.Rotate(transform.up, mState.FromAvatarAngle * 10f * Time.deltaTime);
                            }
                        }
                    }
                }

                // If we're moving backwards, we may need to rotate to face the camera
                if (IsInBackwardsState)
                {
                    if (mBackwardsHeadingLimit == 0)
                    {
                        Vector3 lForward = _CameraRig.transform.forward;
                        lForward.y = 0;
                        lForward.Normalize();
                    }
                    else
                    {
                        bool lIsOrbiting = false; // _CameraRig.IsOrbiting;
                        if (lIsOrbiting && mState.InputY < -0.5f)
                        {
                            transform.Rotate(transform.up, mState.FromAvatarAngle);
                        }
                        else
                        {
                            float lAngularSpeed = Mathf.Lerp(0f, mState.FromAvatarAngle * 2f, 0.9f);
                            transform.Rotate(transform.up, lAngularSpeed * Time.deltaTime);
                        }
                    }
                }

                // If we're moving sideways, we may need to rotate to face the camera
                if (IsInStrafeState)
                {
                    bool lIsOrbiting = false; // _CameraRig.IsOrbiting;
                    if (lIsOrbiting)
                    {
                        transform.Rotate(transform.up, mState.FromAvatarAngle);
                    }
                    else
                    {
                        float lAngularSpeed = Mathf.Lerp(0f, mState.FromAvatarAngle, 0.9f);
                        transform.Rotate(transform.up, lAngularSpeed * Time.deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Called to apply root motion manually. The existance of this
        /// stops the application of any existing root motion since we're
        /// essencially overriding the function. 
        /// 
        /// This function is called right after FixedUpdate() whenever
        /// FixedUpdate() is called. It is not called if FixedUpdate() is not
        /// called.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (Time.deltaTime == 0f) { return; }

            // Store the root motion as a velocity per second. We also
            // want to keep it relative to the avatar's forward vector (for now)
            mRootMotionVelocity = Quaternion.Inverse(transform.rotation) * (mAnimator.deltaPosition / Time.deltaTime);

            // Store the rotation as a velocity per second.
            mRootMotionAngularVelocity = mAnimator.deltaRotation;
            mRootMotionAngularVelocity.x /= Time.deltaTime;
            mRootMotionAngularVelocity.y /= Time.deltaTime;
            mRootMotionAngularVelocity.z /= Time.deltaTime;
            mRootMotionAngularVelocity.w /= Time.deltaTime;
        }

        /// <summary>
        /// Test to see if we're currently in the locomotion state
        /// </summary>
        public bool IsInForwardState
        {
            get
            {
                int lStateHash = 0;

                lStateHash = mStateInfo.fullPathHash;

                if (lStateHash == mForwardIdle2WalkStateID ||
                    lStateHash == mForwardIdle2JogStateID ||
                    lStateHash == mForwardIdle2RunStateID ||
                    lStateHash == mForwardWalkStateID ||
                    lStateHash == mForwardJogStateID ||
                    lStateHash == mForwardRunStateID ||
                    lStateHash == mForwardWalk2IdleStateID ||
                    lStateHash == mForwardJog2IdleStateID ||
                    lStateHash == mForwardRun2IdleStateID ||
                    lStateHash == mForwardRun2BJogStateID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently in the locomotion state
        /// </summary>
        public bool IsInBackwardsState
        {
            get
            {
                int lStateHash = 0;

                lStateHash = mStateInfo.fullPathHash;

                if (lStateHash == mBackwardsIdle2WalkStateID ||
                    lStateHash == mBackwardsIdle2JogStateID ||
                    lStateHash == mBackwardsWalkStateID ||
                    lStateHash == mBackwardsJogStateID ||
                    lStateHash == mBackwardsWalk2IdleStateID ||
                    lStateHash == mBackwardsJog2IdleStateID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently in the locomotion state
        /// </summary>
        public bool IsInStrafeState
        {
            get
            {
                int lStateHash = 0;

                lStateHash = mStateInfo.fullPathHash;

                if (lStateHash == mSidewaysIdle2WalkLeftStateID ||
                    lStateHash == mSidewaysWalkLeftStateID ||
                    lStateHash == mSidewaysWalkLeft2IdleStateID ||
                    lStateHash == mSidewaysIdle2WalkRightStateID ||
                    lStateHash == mSidewaysWalkRightStateID ||
                    lStateHash == mSidewaysWalkRight2IdleStateID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently pivoting
        /// </summary>
        public bool IsInPivotState
        {
            get
            {
                int lStateHash = 0;

                lStateHash = mStateInfo.fullPathHash;

                if (lStateHash == mForwardRunLeft135StateID ||
                    lStateHash == mForwardRunLeft180StateID ||
                    lStateHash == mForwardRunRight135StateID ||
                    lStateHash == mForwardRunRight180StateID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently pivoting
        /// </summary>
        public bool IsInPivotTransitionState
        {
            get
            {
                int lStateHash = 0;
                int lTransitionHash = 0;

                lStateHash = mStateInfo.fullPathHash;
                lTransitionHash = mTransitionInfo.fullPathHash;

                if (lTransitionHash == mForwardRun2RunLeft135TransitionID ||
                    lStateHash == mForwardRunLeft135StateID ||
                    lTransitionHash == mForwardRunLeft1352RunTransitionID ||
                    lTransitionHash == mForwardRun2RunLeft180TransitionID ||
                    lStateHash == mForwardRunLeft180StateID ||
                    lTransitionHash == mForwardRunLeft1802RunTransitionID ||
                    lTransitionHash == mForwardRun2RunRight135TransitionID ||
                    lStateHash == mForwardRunRight135StateID ||
                    lTransitionHash == mForwardRunRight1352RunTransitionID ||
                    lTransitionHash == mForwardRun2RunRight180TransitionID ||
                    lStateHash == mForwardRunRight180StateID ||
                    lTransitionHash == mForwardRunRight1802RunTransitionID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently infront of the camera
        /// </summary>
        public bool IsInForwardPivotState
        {
            get
            {
                int lStateHash = 0;

                lStateHash = mStateInfo.fullPathHash;

                if (lStateHash == mForwardRunLeft180StateID ||
                    lStateHash == mForwardRunRight180StateID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Test to see if we're currently infront of the camera
        /// </summary>
        public bool IsInSidePivotState
        {
            get
            {
                int lStateHash = 0;
                int lTransitionHash = 0;

                lStateHash = mStateInfo.fullPathHash;
                lTransitionHash = mTransitionInfo.fullPathHash;

                if (lTransitionHash == mForwardRun2RunLeft135TransitionID ||
                    lStateHash == mForwardRunLeft135StateID ||
                    lTransitionHash == mForwardRunLeft1352RunTransitionID ||
                    lTransitionHash == mForwardRun2RunRight135TransitionID ||
                    lStateHash == mForwardRunRight135StateID ||
                    lTransitionHash == mForwardRunRight1352RunTransitionID
                    )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// This function is used to convert the game control stick value to
        /// speed and direction values for the player.
        /// </summary>
        protected void StickToWorldspace(Transform rController, Transform rCamera, ref AdventureControllerState rState)
        {
            if (mInputSource == null) { return; }

            // Grab the movement, but create a bit of a dead zone
            float lHInput = mInputSource.MovementX;
            float lVInput = mInputSource.MovementY;

            // Get out early if we can simply this
            if (lVInput == 0f && lHInput == 0f)
            {
                rState.Speed = 0f;
                rState.FromAvatarAngle = 0f;
                rState.InputX = 0f;
                rState.InputY = 0f;

                return;
            }

            // Determine the relative speed
            rState.Speed = Mathf.Sqrt((lHInput * lHInput) + (lVInput * lVInput));

            // Create a simple vector off of our stick input and get the speed
            sVector3A.x = lHInput;
            sVector3A.y = 0f;
            sVector3A.z = lVInput;

            // Direction of the avatar
            Vector3 lControllerForward = rController.forward;
            lControllerForward.y = 0f;
            lControllerForward.Normalize();

            // Direction of the camera
            Vector3 lCameraForward = rCamera.forward;
            lCameraForward.y = 0f;
            lCameraForward.Normalize();

            // Create a quaternion that gets us from our world-forward to our camera direction.
            // FromToRotation creates a quaternion using the shortest method which can sometimes
            // flip the angle. LookRotation will attempt to keep the "up" direction "up".
            //Quaternion rToCamera = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(lCameraForward));
            Quaternion rToCamera = Quaternion.LookRotation(lCameraForward);

            // Transform joystick from world space to camera space. Now the input is relative
            // to how the camera is facing.
            Vector3 lMoveDirection = rToCamera * sVector3A;

            rState.FromCameraAngle = NumberHelper.GetHorizontalAngle(lCameraForward, lMoveDirection);
            rState.FromAvatarAngle = NumberHelper.GetHorizontalAngle(lControllerForward, lMoveDirection);

            // Set the direction of the movement in ranges of -1 to 1
            rState.InputX = lHInput;
            rState.InputY = lVInput;

            //Debug.DrawRay(new Vector3(rController.position.x, rController.position.y + 2f, rController.position.z), lControllerForward, Color.gray);
            //Debug.DrawRay(new Vector3(rController.position.x, rController.position.y + 2f, rController.position.z), lMoveDirection, Color.green);
        }

        /// <summary>
        /// Find the first valid camera rig associated with the motion controller
        /// </summary>
        /// <param name="rCamera"></param>
        /// <returns></returns>
        private IBaseCameraRig ExtractCameraRig(Transform rCamera)
        {
            if (rCamera == null) { return null; }

            Transform lParent = rCamera;
            while (lParent != null)
            {
                IBaseCameraRig[] lRigs = InterfaceHelper.GetComponents<IBaseCameraRig>(lParent.gameObject);
                if (lRigs != null && lRigs.Length > 0)
                {
                    for (int i = 0; i < lRigs.Length; i++)
                    {
                        MonoBehaviour lComponent = (MonoBehaviour)lRigs[i];
                        if (lComponent.enabled && lComponent.gameObject.activeSelf)
                        {
                            return lRigs[i];
                        }
                    }
                }

                lParent = lParent.parent;
            }

            return null;
        }

        /// <summary>
        /// State ids representing "locomotion"
        /// </summary>
        protected int mForwardIdle2WalkStateID = 0;
        protected int mForwardIdle2JogStateID = 0;
        protected int mForwardIdle2RunStateID = 0;
        protected int mForwardWalkStateID = 0;
        protected int mForwardJogStateID = 0;
        protected int mForwardRunStateID = 0;
        protected int mForwardWalk2IdleStateID = 0;
        protected int mForwardJog2IdleStateID = 0;
        protected int mForwardRun2IdleStateID = 0;
        protected int mForwardRun2BJogStateID = 0;

        protected int mForwardRun2RunLeft135TransitionID = 0;
        protected int mForwardRunLeft135StateID = 0;
        protected int mForwardRunLeft1352RunTransitionID = 0;
        protected int mForwardRun2RunLeft180TransitionID = 0;
        protected int mForwardRunLeft180StateID = 0;
        protected int mForwardRunLeft1802RunTransitionID = 0;
        protected int mForwardRun2RunRight135TransitionID = 0;
        protected int mForwardRunRight135StateID = 0;
        protected int mForwardRunRight1352RunTransitionID = 0;
        protected int mForwardRun2RunRight180TransitionID = 0;
        protected int mForwardRunRight180StateID = 0;
        protected int mForwardRunRight1802RunTransitionID = 0;

        protected int mBackwardsIdle2WalkStateID = 0;
        protected int mBackwardsIdle2JogStateID = 0;
        protected int mBackwardsWalkStateID = 0;
        protected int mBackwardsJogStateID = 0;
        protected int mBackwardsWalk2IdleStateID = 0;
        protected int mBackwardsJog2IdleStateID = 0;

        protected int mSidewaysIdle2WalkLeftStateID = 0;
        protected int mSidewaysWalkLeftStateID = 0;
        protected int mSidewaysWalkLeft2IdleStateID = 0;
        protected int mSidewaysIdle2WalkRightStateID = 0;
        protected int mSidewaysWalkRightStateID = 0;
        protected int mSidewaysWalkRight2IdleStateID = 0;
        
        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public virtual void LoadAnimatorData()
        {
            mForwardIdle2WalkStateID = Animator.StringToHash("Base Layer.Forward-SM.Idle2Walk");
            mForwardIdle2JogStateID = Animator.StringToHash("Base Layer.Forward-SM.Idle2Jog");
            mForwardIdle2RunStateID = Animator.StringToHash("Base Layer.Forward-SM.Idle2Run");
            mForwardWalkStateID = Animator.StringToHash("Base Layer.Forward-SM.WalkForward");
            mForwardJogStateID = Animator.StringToHash("Base Layer.Forward-SM.JogForward");
            mForwardRunStateID = Animator.StringToHash("Base Layer.Forward-SM.RunForward");
            mForwardWalk2IdleStateID = Animator.StringToHash("Base Layer.Forward-SM.Walk2Idle");
            mForwardJog2IdleStateID = Animator.StringToHash("Base Layer.Forward-SM.Jog2Idle");
            mForwardRun2IdleStateID = Animator.StringToHash("Base Layer.Forward-SM.Run2Idle");

            mForwardRun2RunLeft135TransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunForward -> Base Layer.Forward-SM.RunLeft135");
            mForwardRunLeft135StateID = Animator.StringToHash("Base Layer.Forward-SM.RunLeft135");
            mForwardRunLeft1352RunTransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunLeft135 -> Base Layer.Forward-SM.RunForward");
            mForwardRun2RunLeft180TransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunForward -> Base Layer.Forward-SM.RunLeft180");
            mForwardRunLeft180StateID = Animator.StringToHash("Base Layer.Forward-SM.RunLeft180");
            mForwardRunLeft1802RunTransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunLeft180 -> Base Layer.Forward-SM.RunForward");
            mForwardRun2RunRight135TransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunForward -> Base Layer.Forward-SM.RunRight135");
            mForwardRunRight135StateID = Animator.StringToHash("Base Layer.Forward-SM.RunRight135");
            mForwardRunRight1352RunTransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunRight135 -> Base Layer.Forward-SM.RunForward");
            mForwardRun2RunRight180TransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunForward -> Base Layer.Forward-SM.RunRight180");
            mForwardRunRight180StateID = Animator.StringToHash("Base Layer.Forward-SM.RunRight180");
            mForwardRunRight1802RunTransitionID = Animator.StringToHash("Base Layer.Forward-SM.RunRight180 -> Base Layer.Forward-SM.RunForward");

            mBackwardsIdle2WalkStateID = Animator.StringToHash("Base Layer.Backwards-SM.Idle2BWalk");
            mBackwardsIdle2JogStateID = Animator.StringToHash("Base Layer.Backwards-SM.Idle2BJog");
            mBackwardsWalkStateID = Animator.StringToHash("Base Layer.Backwards-SM.WalkBackwards");
            mBackwardsJogStateID = Animator.StringToHash("Base Layer.Backwards-SM.JogBackwards");
            mBackwardsWalk2IdleStateID = Animator.StringToHash("Base Layer.Backwards-SM.BWalk2Idle");
            mBackwardsJog2IdleStateID = Animator.StringToHash("Base Layer.Backwards-SM.BJog2Idle");
            mSidewaysIdle2WalkLeftStateID = Animator.StringToHash("Base Layer.Strafe-Left-SM.Idle2SWalk");
            mSidewaysWalkLeftStateID = Animator.StringToHash("Base Layer.Strafe-Left-SM.SWalkLeft");
            mSidewaysWalkLeft2IdleStateID = Animator.StringToHash("Base Layer.Strafe-Left-SM.SWalk2Idle");
            mSidewaysIdle2WalkRightStateID = Animator.StringToHash("Base Layer.Strafe-Right-SM.Idle2SWalk");
            mSidewaysWalkRightStateID = Animator.StringToHash("Base Layer.Strafe-Right-SM.SWalkRight");
            mSidewaysWalkRight2IdleStateID = Animator.StringToHash("Base Layer.Strafe-Right-SM.SWalk2Idle");
        }
    }
}

