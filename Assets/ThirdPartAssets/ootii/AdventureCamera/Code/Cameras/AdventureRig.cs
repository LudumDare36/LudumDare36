using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// The adventure camera rig creates a 3rd person
    /// camera similiar to what you'd find in Uncharted, Tomb Raider,
    /// and Assassin's Creed. It allows the player full motion in
    /// the view, but follows the player if they try to move out.
    /// </summary>
    [AddComponentMenu("ootii/Camera Rigs/Adventure Rig")]
    public class AdventureRig : BaseCameraRig
    {
        /// <summary>
        /// Provides a value for "numerical error"
        /// </summary>
        public const float EPSILON = 0.0001f;

        /// <summary>
        /// Extra spacing between the collision objects
        /// </summary>
        public const float COLLISION_BUFFER = 0.001f;

        // Keep us from going past the poles. The number is a bit
        // odd, but it matches Unity's limit for the LookRotation function.
        private const float MIN_PITCH = -87.4f;
        private const float MAX_PITCH =  87.4f;

        /// <summary>
        /// The type of the camera in order to help determine
        /// how the camera moves and rotates.
        /// </summary>
        public override int Mode
        {
            get { return _Mode; }

            set
            {
                _Mode = value;

                // Normal third person mode
                if (_Mode == 0)
                {
                    mOffsetEnd = _AnchorOffset;

                    mDistanceEnd = _AnchorDistance;

                    mActiveDistance = _AnchorDistance;
                    mActiveDistanceTime = _AnchorTime;
                    mActiveOffsetTime = _AnchorTime;

                    _Euler.y = _Transform.eulerAngles.y;
                    _EulerTarget.y = _Transform.eulerAngles.y;
                }
                // First person mode for aiming
                else if (_Mode == 2)
                {
                    mOffsetEnd = _AltAnchorOffset;

                    mDistanceEnd = _AltAnchorDistance;

                    mActiveDistance = _AltAnchorDistance;
                    mActiveDistanceTime = _AltAnchorTime;
                    mActiveOffsetTime = _AltAnchorTime;

                    mForceToFollowActor = true;
                    if (_AltForceActorToView) { ForceActorToView(1f); }

                    _Euler.y = 0f;
                    _EulerTarget.y = 0f;                    
                }

                // Reset the field of view
                mFieldOfViewTarget = mMaxFieldOfView;
            }
        }

        /// <summary>
        /// GameObject that owns the IInputSource we really want
        /// </summary>
        public GameObject _InputSourceOwner = null;
        public GameObject InputSourceOwner
        {
            get { return _InputSourceOwner; }

            set
            {
                _InputSourceOwner = value;

                // Object that will provide access to the keyboard, mouse, etc
                if (_InputSourceOwner != null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner); }
            }
        }

        /// <summary>
        /// Determines if we'll auto find the input source if one doesn't exist
        /// </summary>
        public bool _AutoFindInputSource = true;
        public bool AutoFindInputSource
        {
            get { return _AutoFindInputSource; }
            set { _AutoFindInputSource = value; }
        }

        /// <summary>
        /// Determines if we invert the pitch information we get from the input
        /// </summary>
        public bool _InvertPitch = true;
        public virtual bool InvertPitch
        {
            get { return _InvertPitch; }
            set { _InvertPitch = value; }
        }

        /// <summary>
        /// Transform that represents the anchor we want to follow
        /// </summary>
        public override Transform Anchor
        {
            get { return _Anchor; }

            set
            {
                if (_Anchor != null)
                {
                    // Stop listening to the old controller
                    ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                    if (lController != null) { lController.OnControllerPostLateUpdate -= OnControllerLateUpdate; }
                }

                _Anchor = value;
                if (_Anchor != null && this.enabled)
                {
                    // Start listening to the new controller
                    ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                    if (lController != null)
                    {
                        IsInternalUpdateEnabled = false;
                        IsFixedUpdateEnabled = false;
                        lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                    }
                }
            }
        }

        /// <summary>
        /// Offset from the anchor that the camera will be positioned
        /// </summary>
        public Vector3 _AnchorOffset = new Vector3(0f, 2f, 0f);
        public Vector3 AnchorOffset
        {
            get { return _AnchorOffset; }

            set
            {
                if (mOffsetEnd == _AnchorOffset)
                {
                    mOffsetEnd = value;
                }

                _AnchorOffset = value;
            }
        }

        /// <summary>
        /// Radius of the orbit
        /// </summary>
        public float _AnchorDistance = 3f;
        public float AnchorDistance
        {
            get { return _AnchorDistance; }

            set
            {
                if (value < 0f) { return; }
                if (_MinDistance > 0f && value < _MinDistance) { _MinDistance = value; }
                if (_MaxDistance > 0f && value >= _MaxDistance) { _MaxDistance = value * 1.5f; }

                if (mActiveDistance == _AnchorDistance)
                {
                    mActiveDistance = value;
                }

                if (mDistanceEnd == _AnchorDistance)
                {
                    mDistance = value;
                    mDistanceEnd = value;
                }
                else if (_Mode == 0)
                {
                    mDistance = Mathf.Min(mDistance, _AnchorDistance);
                    mDistanceEnd = Mathf.Min(mDistanceEnd, _AnchorDistance);
                }

                _AnchorDistance = value;
            }
        }

        /// <summary>
        /// Time (in seconds) to transition to the view
        /// </summary>
        public float _AnchorTime = 0.40f;
        public float AnchorTime
        {
            get { return _AnchorTime; }
            set { _AnchorTime = value; }
        }

        /// <summary>
        /// Determines if the anchor will attempt to move around the camera
        /// </summary>
        public bool _AnchorOrbitsCamera = true;
        public bool AnchorOrbitsCamera
        {
            get { return _AnchorOrbitsCamera; }
            set { _AnchorOrbitsCamera = value; }
        }

        /// <summary>
        /// Determines if we allow the camera to go to the alternate mode (mode 2)
        /// </summary>
        public bool _IsAltModeEnabled = true;
        public bool IsAltModeEnabled
        {
            get { return _IsAltModeEnabled; }
            set { _IsAltModeEnabled = value; }
        }

        /// <summary>
        /// Input alias to use to enter the alias view
        /// </summary>
        public string _AltActionAlias = "Fire3";
        public string AltActionAlias
        {
            get { return _AltActionAlias; }
            set { _AltActionAlias = value; }
        }

        /// <summary>
        /// Determines if the alternate alias is treated as a toggle
        /// </summary>
        public bool _AltActionAliasAsToggle = true;
        public bool AltActionAliasAsToggle
        {
            get { return _AltActionAliasAsToggle; }
            set { _AltActionAliasAsToggle = value; }
        }

        /// <summary>
        /// Offset from the anchor that the camera will be positioned
        /// </summary>
        public Vector3 _AltAnchorOffset = new Vector3(0.7f, 1.7f, 0f);
        public Vector3 AltAnchorOffset
        {
            get { return _AltAnchorOffset; }

            set
            {
                if (mOffsetEnd == _AltAnchorOffset)
                {
                    mOffsetEnd = value;
                }

                _AltAnchorOffset = value;
            }
        }

        /// <summary>
        /// Radius of the orbit
        /// </summary>
        public float _AltAnchorDistance = 1f;
        public float AltAnchorDistance
        {
            get { return _AltAnchorDistance; }

            set
            {
                if (value < 0f) { return; }
                if (_MinDistance > 0f && value < _MinDistance) { return; }
                if (_MaxDistance > 0f && value > _MaxDistance) { return; }

                if (mActiveDistance == _AltAnchorDistance)
                {
                    mActiveDistance = value;
                }

                if (mDistanceEnd == _AltAnchorDistance)
                {
                    mDistance = value;
                    mDistanceEnd = value;
                }
                else if (_Mode == 2)
                {
                    mDistance = Mathf.Min(mDistance, _AltAnchorDistance);
                    mDistanceEnd = Mathf.Min(mDistanceEnd, _AltAnchorDistance);
                }

                _AltAnchorDistance = value;
            }
        }

        /// <summary>
        /// Time (in seconds) to transition to the alternate view
        /// </summary>
        public float _AltAnchorTime = 0.15f;
        public float AltAnchorTime
        {
            get { return _AltAnchorTime; }
            set { _AltAnchorTime = value; }
        }

        /// <summary>
        /// Determines if the camera will force the actor to rotate to the
        /// view when the alternate view is enabled
        /// </summary>
        public bool _AltForceActorToView = true;
        public bool AltForceActorToView
        {
            get { return _AltForceActorToView; }
            set { _AltForceActorToView = value; }
        }

        /// <summary>
        /// Degrees per second the actor rotates
        /// </summary>
        public float _YawSpeed = 120f;
        public float YawSpeed
        {
            get { return _YawSpeed; }

            set
            {
                _YawSpeed = value;
                mDegreesYPer60FPSTick = _YawSpeed / 60f;
            }
        }

        /// <summary>
        /// Degrees per second the actor rotates
        /// </summary>
        public float _PitchSpeed = 90f;
        public float PitchSpeed
        {
            get { return _PitchSpeed; }

            set
            {
                _PitchSpeed = value;
                if (_Mode == 0) { mDegreesXPer60FPSTick = _PitchSpeed / 60f; }
            }
        }

        /// <summary>
        /// Degrees per second the actor rotates
        /// </summary>
        public float _AltPitchSpeed = 45f;
        public float AltPitchSpeed
        {
            get { return _AltPitchSpeed; }

            set
            {
                _AltPitchSpeed = value;
                if (_Mode != 0) { mDegreesXPer60FPSTick = _AltPitchSpeed / 60f; }
            }
        }

        /// <summary>
        /// Minimum pitch value allowed
        /// </summary>
        public float _MinPitch = -60f;
        public float MinPitch
        {
            get { return _MinPitch; }
            set { _MinPitch = Mathf.Max(value, MIN_PITCH); }
        }

        /// <summary>
        /// Maximum pitch value allowed
        /// </summary>
        public float _MaxPitch = 70f;
        public float MaxPitch
        {
            get { return _MaxPitch; }
            set { _MaxPitch = Mathf.Min(value, MAX_PITCH); }
        }

        /// <summary>
        /// Used to apply some smoothing to the mouse movement
        /// </summary>
        public float _OrbitSmoothing = 0.1f;
        public float OrbitSmoothing
        {
            get { return _OrbitSmoothing; }
            set { _OrbitSmoothing = value; }
        }

        /// <summary>
        /// Determines if we use the camera collider to avoid collisions
        /// </summary>
        public bool _IsCollisionsEnabled = false;
        public bool IsCollisionsEnabled
        {
            get { return _IsCollisionsEnabled; }
            set { _IsCollisionsEnabled = value; }
        }

        /// <summary>
        /// Determines if we use the camera collider to avoid collisions in alt mode
        /// </summary>
        public bool _IsAltCollisionsEnabled = false;
        public bool IsAltCollisionsEnabled
        {
            get { return _IsAltCollisionsEnabled; }
            set { _IsAltCollisionsEnabled = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public int _CollisionLayers = 1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Determines if we change the camera colliders's size to cover the near plane automatically
        /// </summary>
        public bool _AutoCollisionRadius = true;
        public bool AutoCollisionRadius
        {
            get { return _AutoCollisionRadius; }
            set { _AutoCollisionRadius = value; }
        }

        /// <summary>
        /// Multiplier to apply to the near plane's width when we automatically set the collision radius
        /// </summary>
        public float _AutoCollisionRadiusFactor = 1.5f;
        public float AutoCollisionRadiusFactor
        {
            get { return _AutoCollisionRadiusFactor; }
            set { _AutoCollisionRadiusFactor = value; }
        }

        /// <summary>
        /// When the collision radius isn't automatically set, the radius to use
        /// </summary>
        public float _CollisionRadius = 0.25f;
        public float CollisionRadius
        {
            get { return _CollisionRadius; }
            set { _CollisionRadius = value; }
        }

        /// <summary>
        /// Determines if we test the view for any objects that keep us from seeing the character
        /// </summary>
        public bool _IsObstructionsEnabled = true;
        public bool IsObstructionsEnabled
        {
            get { return _IsObstructionsEnabled; }
            set { _IsObstructionsEnabled = value; }
        }

        /// <summary>
        /// Determines if we test the view for any objects that keep us from seeing the character in targeting mode
        /// </summary>
        public bool _IsAltObstructionsEnabled = false;
        public bool IsAltObstructionsEnabled
        {
            get { return _IsAltObstructionsEnabled; }
            set { _IsAltObstructionsEnabled = value; }
        }

        /// <summary>
        /// Determines if we change the camera colliders's size to cover the near plane automatically
        /// </summary>
        public bool _AutoObstructionRadius = true;
        public bool AutoObstructionRadius
        {
            get { return _AutoObstructionRadius; }
            set { _AutoObstructionRadius = value; }
        }

        /// <summary>
        /// When the collision radius isn't automatically set, the radius to use
        /// </summary>
        public float _ObstructionRadius = 0.25f;
        public float ObstructionRadius
        {
            get { return _ObstructionRadius; }
            set { _ObstructionRadius = value; }
        }

        /// <summary>
        /// When there isn't a collision any more, the length of time before we
        /// start heading back to the normal posiont
        /// </summary>
        public float _RecoveryDelay = 0.25f;
        public float RecoveryDelay
        {
            get { return _RecoveryDelay; }
            set { _RecoveryDelay = value; }
        }

        /// <summary>
        /// Minimum distance the camera can get to the anchor
        /// </summary>
        public float _MinDistance = 0f;
        public float MinDistance
        {
            get { return _MinDistance; }
            set { _MinDistance = value; }
        }

        /// <summary>
        /// Maximum distance the camera can get from the anchor
        /// </summary>
        public float _MaxDistance = 5.0f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Determines if we fade the character when the camera is close
        /// </summary>
        public bool _IsFadeEnabled = true;
        public bool IsFadeEnabed
        {
            get { return _IsFadeEnabled; }

            set
            {
                _IsFadeEnabled = value;
                if (!_IsFadeEnabled) { SetAnchorAlpha(1f); }
            }
        }

        /// <summary>
        /// Distance at which we start the fading
        /// </summary>
        public float _FadeDistance = 0.4f;
        public float FadeDistance
        {
            get { return _FadeDistance; }
            set { _FadeDistance = value; }
        }

        /// <summary>
        /// Time it takes to fade
        /// </summary>
        public float _FadeSpeed = 0.25f;
        public float FadeSpeed
        {
            get { return _FadeSpeed; }
            set { _FadeSpeed = value; }
        }

        /// <summary>
        /// After fading is complete, determines if we disable the renderers. This is good
        /// when there are materials with no transparancies (which is required for fading)
        /// </summary>
        public bool _DisableRenderers = true;
        public bool DisableRenderers
        {
            get { return _DisableRenderers; }
            set { _DisableRenderers = value; }
        }

        /// <summary>
        /// Determines if we'll use the field of view ability
        /// </summary>
        public bool mIsFieldOfViewEnabled = false;
        public bool IsFieldOfViewEnabled
        {
            get { return mIsFieldOfViewEnabled; }

            set
            {
                mIsFieldOfViewEnabled = value;

                if (!mIsFieldOfViewEnabled)
                {
                    _FieldOfView = mMaxFieldOfView;
                    mFieldOfViewTarget = mMaxFieldOfView;

                    if (_Camera != null)
                    {
                        _Camera.fieldOfView = mMaxFieldOfView;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we'll use the field of view ability in alternate mode
        /// </summary>
        public bool mIsAltFieldOfViewEnabled = true;
        public bool IsAltFieldOfViewEnabled
        {
            get { return mIsAltFieldOfViewEnabled; }

            set
            {
                mIsAltFieldOfViewEnabled = value;

                if (!mIsAltFieldOfViewEnabled)
                {
                    _FieldOfView = mMaxFieldOfView;
                    mFieldOfViewTarget = mMaxFieldOfView;
                    _Camera.fieldOfView = mMaxFieldOfView;
                }
            }
        }

        /// <summary>
        /// Input alias to use to adjust the zoom
        /// </summary>
        public string _FieldOfViewActionAlias = "Mouse ScrollWheel";
        public string FieldOfViewActionAlias
        {
            get { return _FieldOfViewActionAlias; }
            set { _FieldOfViewActionAlias = value; }
        }

        /// <summary>
        /// Current field of view that we use to indicate the zoom level
        /// </summary>
        public float _FieldOfView = 0f;
        public float FieldOfView
        {
            get { return _FieldOfView; }

            set
            {
                // We set the target which will slowly move us in and out
                mFieldOfViewTarget = value;
            }
        }

        /// <summary>
        /// Max zoom (smallest FOV)
        /// </summary>
        public float _MinFieldOfView = 20f;
        public float MinFieldOfView
        {
            get { return _MinFieldOfView; }
            set { _MinFieldOfView = value; }
        }

        /// <summary>
        /// Initial FOV (default zoom)
        /// </summary>
        protected float mMaxFieldOfView = 60f;
        public float MaxFieldOfView
        {
            get { return mMaxFieldOfView; }
            set { mMaxFieldOfView = value; }
        }

        /// <summary>
        /// Factor per second to zoom in and out
        /// </summary>
        public float _FieldOfViewSpeed = -20f;
        public float FieldOfViewSpeed
        {
            get { return _FieldOfViewSpeed; }
            set { _FieldOfViewSpeed = value; }
        }

        /// <summary>
        /// Used to apply some smoothing to the mouse movement
        /// </summary>
        public float _FieldOfViewSmoothing = 0.1f;
        public virtual float FieldOfViewSmoothing
        {
            get { return _FieldOfViewSmoothing; }
            set { _FieldOfViewSmoothing = value; }
        }
        
        /// <summary>
        /// Keep track of the non-tilt yaw... this is the left/right angle of the camera from the anchor
        /// </summary>
        //public float _Yaw = 0f;
        public float LocalYaw
        {
            get { return mLocalEuler.y; }

            set
            {
                mFrameEuler.y = value - mLocalEuler.y;
                mLocalEuler.y = value;
            }
        }

        /// <summary>
        /// Keep track of the non-tilt pitch... this is the up/down angle of the camera from the anchor 
        /// </summary>
        //public float _Pitch = 0f;
        public float LocalPitch
        {
            get { return mLocalEuler.x; }

            set
            {
                value = Mathf.Clamp(value, _MinPitch, _MaxPitch);

                mFrameEuler.x = value - mLocalEuler.x;
                mLocalEuler.x = value;
            }
        }

        /// <summary>
        /// Determines how strong the shake is over the duration
        /// </summary>
        public AnimationCurve _ShakeStrength = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve ShakeStrength
        {
            get { return _ShakeStrength; }
            set { _ShakeStrength = value; }
        }

        /// <summary>
        /// Holds the yaw and pitch values for us
        /// </summary>
        protected Vector3 _Euler = Vector3.zero;
        public Vector3 Euler
        {
            get { return _Euler; }
        }

        /// <summary>
        /// Holds the yaw and pitch values we're trying to get to
        /// </summary>
        protected Vector3 _EulerTarget = Vector3.zero;
        public Vector3 EulerTarget
        {
            get { return _EulerTarget; }
        }

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesYPer60FPSTick = 1f;

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesXPer60FPSTick = 1f;

        /// <summary>
        /// We keep track of the tilt so we can make small changes to it as the actor rotates.
        /// This is safter than trying to do a full rotation all at once which can cause odd
        /// rotations as we hit 180 degrees.
        /// </summary>
        protected Quaternion mTilt = Quaternion.identity;

        /// <summary>
        /// Yaw and pitch values this frame
        /// </summary>
        protected Vector3 mFrameEuler = Vector3.zero;
        public Vector3 FrameEuler
        {
            get { return mFrameEuler; }
        }

        /// <summary>
        /// Yaw and pitch values relative to the anchor
        /// </summary>
        protected Vector3 mLocalEuler = Vector3.zero;

        /// <summary>
        /// Distance between the camera and anchor that should be 
        /// </summary>
        protected float mActiveDistance = 0f;

        /// <summary>
        /// Time for the transition
        /// </summary>
        protected float mActiveDistanceTime = 0.5f;

        /// <summary>
        /// Time for the transition
        /// </summary>
        protected float mActiveOffsetTime = 0.5f;

        /// <summary>
        /// Provides access to the keyboard, mouse, etc.
        /// </summary>
        protected IInputSource mInputSource = null;

        /// <summary>
        /// Determines if we'll continue to force the camera behind the actor
        /// </summary>
        protected bool mForceToFollowActor = false;
        public bool ForceToFollowAnchor
        {
            get { return mForceToFollowActor; }
        }

        /// <summary>
        /// Last time that a collision occured
        /// </summary>
        protected float mLastCollisionTime = 0f;

        /// <summary>
        /// Last distance of the collision
        /// </summary>
        protected float mLastCollisionDistance = 0f;

        /// <summary>
        /// Track the last collision vector that occured
        /// </summary>
        protected Vector3 mLastCollisionDirection = Vector3.zero;

        /// <summary>
        /// Represents the "pole" that the camera is attched to the anchor with. This pole
        /// is the direction from the anchor to the camera (in natural "up" space)
        /// </summary>
        protected Vector3 mToCameraDirection = Vector3.back;

        /// <summary>
        /// Used to manage transitioning from one offset to another
        /// </summary>
        protected Vector3 mOffset = Vector3.zero;
        protected Vector3 mOffsetStart = Vector3.zero;
        protected Vector3 mOffsetEnd = Vector3.zero;
        protected float mOffsetTime = 0.5f;
        protected float mOffsetElapsed = 0f;

        /// <summary>
        /// Distance values used to ease to the target distance over time
        /// </summary>
        protected float mDistance = 0f;
        public float ActualDistance { get { return mDistance; } }

        protected float mDistanceStart = 0f;

        public float mDistanceEnd = 0f;
        public float DistanceEnd { get { return mDistanceEnd; } }

        protected float mDistanceTime = 0.5f;
        protected float mDistanceElapsed = 0f;

        /// <summary>
        /// Alpha values used to ease to the target alpha over time
        /// </summary>
        protected float mAlpha = 1f;
        protected float mAlphaStart = 0f;
        protected float mAlphaEnd = 1f;
        protected float mAlphaElapsed = 0f;

        /// <summary>
        /// Defines how long we've been shaking for
        /// </summary>
        protected float mShakeElapsed = 0f;
        protected float mShakeDuration = 0f;
        protected float mShakeSpeedFactor = 1f;
        protected float mShakeRange = 0.05f;
        protected float mShakeStrengthX = 1f;
        protected float mShakeStrengthY = 1f;

        /// <summary>
        /// Smooth out the orbiting (pitch) of the camera
        /// </summary>
        private float mViewVelocityX = 0f;

        /// <summary>
        /// Smooth out the orbiting (yaw) of the camera
        /// </summary>
        private float mViewVelocityY = 0f;

        /// <summary>
        /// Smooth out the zooming of the camera
        /// </summary>
        private float mFieldOfViewTarget = 60f;
        public float FieldOfViewTarget
        {
            get { return mFieldOfViewTarget; }
            set { mFieldOfViewTarget = value; }
        }

        private float mFieldOfViewVelocity = 0f;

        /// <summary>
        /// Keeps us from reallocating the arrays over and over
        /// </summary>
        protected RaycastHit[] mRaycastHitArray = new RaycastHit[15];
        protected BodyShapeHit[] mBodyShapeHitArray = new BodyShapeHit[15];

        /// <summary>
        /// Use this for initialization
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (_FieldOfView > 0f) { _Camera.fieldOfView = _FieldOfView; }
            _FieldOfView = _Camera.fieldOfView;
            mMaxFieldOfView = _Camera.fieldOfView;
            mFieldOfViewTarget = _Camera.fieldOfView;

            mOffset = _AnchorOffset;
            mOffsetEnd = _AnchorOffset;

            mDistance = _AnchorDistance;
            mDistanceEnd = _AnchorDistance;
            mActiveDistance = _AnchorDistance;
            mLastCollisionDistance = _AnchorDistance;

            mAlpha = 1f;
            mAlphaEnd = 1f;

            if (_Anchor != null && this.enabled)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController != null)
                {
                    IsInternalUpdateEnabled = false;
                    IsFixedUpdateEnabled = false;
                    lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                }

                mTilt = QuaternionExt.FromToRotation(Vector3.up, _Anchor.up);

                Vector3 lEulerAngles = new Vector3(0f, (_Euler.y == 0f ? _Transform.eulerAngles.y : _Euler.y), (_Euler.x == 0f ? _Transform.eulerAngles.x : _Euler.x));
                _Transform.eulerAngles = lEulerAngles;

                _Euler.y = _Transform.eulerAngles.y;
                _Euler.x = _Transform.eulerAngles.x;

                _EulerTarget.y = _Transform.eulerAngles.y;
                _EulerTarget.x = _Transform.eulerAngles.x;
            }

            // Force the near plane distance
            if (_Camera != null)
            {
                if (_Camera.nearClipPlane == 0.3f)
                {
                    _Camera.nearClipPlane = 0.1f;
                }
            }

            // Determine the auto radius if needed
            if (_AutoCollisionRadius || _AutoObstructionRadius)
            {
                if (_Camera != null)
                {
                    float lPlaneWidth = GetNearPlaneWidth() + 0.1f;
                    if (_AutoCollisionRadius) { _CollisionRadius = lPlaneWidth; }
                    if (_AutoObstructionRadius) { _ObstructionRadius = lPlaneWidth; }
                }
            }

            // Object that will provide access to the keyboard, mouse, etc
            if (_InputSourceOwner != null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner); }

            // If the input source is still null, see if we can grab a local input source
            if (mInputSource == null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(gameObject); }

            // If that's still null, see if we can grab one from the scene. This may happen
            // if the MC was instanciated from a prefab which doesn't hold a reference to the input source
            if (_AutoFindInputSource && mInputSource == null)
            {
                IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
                for (int i = 0; i < lInputSources.Length; i++)
                {
                    GameObject lInputSourceOwner = ((MonoBehaviour)lInputSources[i]).gameObject;
                    if (lInputSourceOwner.activeSelf && lInputSources[i].IsEnabled)
                    {
                        mInputSource = lInputSources[i];
                        _InputSourceOwner = lInputSourceOwner;
                    }
                }
            }

            // Default the speed we'll use to rotate
            if (_Mode == 0)
            {
                mDegreesYPer60FPSTick = _YawSpeed / 60f;
                mDegreesXPer60FPSTick = _PitchSpeed / 60f;
            }
            else
            {
                mDegreesXPer60FPSTick = _AltPitchSpeed / 60f;
            }

            // Default the curve
            if (_ShakeStrength.keys.Length == 2)
            {
                if (_ShakeStrength.keys[0].value == 0f && _ShakeStrength.keys[0].value == 0f)
                {
                    _ShakeStrength.AddKey(0.5f, 1f);
                }
            }
        }

        /// <summary>
        /// Called when the component is enabled. This is also called after awake. So,
        /// we need to ensure we're not doubling up on the assignment.
        /// </summary>
        protected void OnEnable()
        {
            if (_Anchor != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController != null)
                {
                    if (lController.OnControllerPostLateUpdate != null) { lController.OnControllerPostLateUpdate -= OnControllerLateUpdate; }
                    lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                }
            }
        }

        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        protected void OnDisable()
        {
            if (_Anchor != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController != null && lController.OnControllerPostLateUpdate != null)
                {
                    lController.OnControllerPostLateUpdate -= OnControllerLateUpdate;
                }
            }
        }

        /// <summary>
        /// Force the camera to reset behind the player
        /// </summary>
        public void Reset()
        {
            _Euler = Vector3.zero;
            _EulerTarget = Vector3.zero;

            mOffset = _AnchorOffset;
            mOffsetEnd = _AnchorOffset;

            mDistance = _AnchorDistance;
            mDistanceEnd = _AnchorDistance;
            mActiveDistance = _AnchorDistance;
            mLastCollisionDistance = _AnchorDistance;

            if (_Transform != null && _Anchor != null)
            {
                _Transform.position = _Anchor.position + (_Anchor.rotation * _AnchorOffset) - (_Anchor.forward * _AnchorDistance);
            }
        }

        /// <summary>
        /// LateUpdate logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void RigLateUpdate(float rDeltaTime, int rUpdateIndex)
        {
            // Determine the mode based on the input
            if (_IsAltModeEnabled && mInputSource != null)
            {
                if (!mLockMode)
                {
                    if (_AltActionAliasAsToggle)
                    {
                        if (mInputSource.IsJustPressed(_AltActionAlias))
                        {
                            Mode = (Mode == 0 ? 2 : 0);
                        }
                    }
                    else
                    {
                        if (mInputSource.IsPressed(_AltActionAlias))
                        {
                            if (_Mode != 2) { Mode = 2; }
                        }
                        else
                        {
                            if (_Mode != 0) { Mode = 0; }
                        }
                    }
                }

                // Set the speed we'll use to rotate
                if (_Mode == 0)
                {
                    mDegreesYPer60FPSTick = _YawSpeed / 60f;
                    mDegreesXPer60FPSTick = _PitchSpeed / 60f;
                }
                else
                {
                    mDegreesXPer60FPSTick = _AltPitchSpeed / 60f;
                }
            }

            // Update the collider radius if we're meant to
            if (_AutoCollisionRadius || _AutoObstructionRadius)
            {
                if (_Camera != null)
                {
                    float lPlaneWidth = GetNearPlaneWidth() * _AutoCollisionRadiusFactor;
                    if (_AutoCollisionRadius) { _CollisionRadius = lPlaneWidth; }
                    if (_AutoObstructionRadius) { _ObstructionRadius = lPlaneWidth; }
                }
            }

            // Smooth any offset changes
            if (mOffset != mOffsetEnd)
            {
                mOffsetElapsed = mOffsetElapsed + Time.deltaTime;
                mOffset = Vector3Ext.SmoothStep(mOffsetStart, mOffsetEnd, mOffsetElapsed / mActiveOffsetTime);
            }
            else
            {
                // This occurs after we finish a transition. Once done,
                // reset the view flags
                if (_Mode == 0 && mOffsetStart != mOffset)
                {
                    mForceToFollowActor = false;

                    _Euler.y = _Transform.eulerAngles.y;
                    _EulerTarget.y = _Transform.eulerAngles.y;
                }

                // Reset the offset value
                mOffsetElapsed = 0f;
                mOffsetStart = mOffset;
                mActiveOffsetTime = mOffsetTime;
            }

            // Smooth any distance changes
            if (mDistance != mDistanceEnd)
            {
                mDistanceElapsed = mDistanceElapsed + Time.deltaTime;
                mDistance = NumberHelper.SmoothStep(mDistanceStart, mDistanceEnd, mDistanceElapsed / mActiveDistanceTime);
            }
            else
            {
                mDistanceElapsed = 0f;
                mDistanceStart = mDistance;
                mActiveDistanceTime = mDistanceTime;
            }

            // Smooth any FOV changes
            if ((Mathf.Abs(mFieldOfViewTarget - _FieldOfView) > 0.001f) ||
                (_Mode == 0 && mIsFieldOfViewEnabled) || 
                (_Mode != 0 && mIsAltFieldOfViewEnabled))
            {
                if (_FieldOfViewActionAlias.Length > 0)
                {
                    float lFrameFieldOfView = mInputSource.GetValue(_FieldOfViewActionAlias) * _FieldOfViewSpeed;
                    mFieldOfViewTarget = Mathf.Clamp(mFieldOfViewTarget + lFrameFieldOfView, _MinFieldOfView, mMaxFieldOfView);
                }

                if (Mathf.Abs(mFieldOfViewTarget - _FieldOfView) > 0.001f)
                {
                    _FieldOfView = (_FieldOfViewSmoothing <= 0f ? mFieldOfViewTarget : Mathf.SmoothDampAngle(_FieldOfView, mFieldOfViewTarget, ref mFieldOfViewVelocity, _FieldOfViewSmoothing));
                    _Camera.fieldOfView = _FieldOfView;
                }
            }

            // Update our tilt to match the anchor's tilt. We do the tilt in increments so we
            // don't have any crazy 180 degree tilts.
            Vector3 lTiltUp = mTilt.Up();
            Quaternion lTiltDelta = QuaternionExt.FromToRotation(lTiltUp, _Anchor.up);
            if (!QuaternionExt.IsIdentity(lTiltDelta))
            {
                mTilt = lTiltDelta * mTilt;
            }

            // Grab the angular difference between our up and the natural up. If there
            // is no difference, we can clean up the tilt some.
            float lTiltAngle = Vector3.Angle(mTilt.Up(), Vector3.up);
            if (lTiltAngle < 0.0001f)
            {
                if (!QuaternionExt.IsIdentity(mTilt))
                {
                    // The tilt can get out of whack when rotating 180-degrees and then
                    // 360-degrees. As a safety precaution, we'll use this to ensure we
                    // match the natural up whenever possible.
                    _Euler = _Euler + mTilt.eulerAngles;
                    mTilt = Quaternion.identity;
                }
            }

            // Default the new camera info
            Vector3 lNewAnchorPosition = _Anchor.position + (_Anchor.rotation * mOffset);
            Vector3 lNewCameraPosition = _Transform.position;
            Quaternion lNewCameraRotation = _Transform.rotation;

            // Process rotation if viewing is allowed
            if (mInputSource.IsViewingActivated)
            {
                if (mFrameEuler.y == 0f) { mFrameEuler.y = mInputSource.ViewX * mDegreesYPer60FPSTick; }
                if (mFrameEuler.x == 0f) { mFrameEuler.x = (_InvertPitch ? -1f : 1f) * mInputSource.ViewY * mDegreesXPer60FPSTick; }
            }

            // Grab the smoothed yaw
            _EulerTarget.y = _EulerTarget.y + mFrameEuler.y;
            mFrameEuler.y = (_OrbitSmoothing <= 0f ? _EulerTarget.y : Mathf.SmoothDampAngle(_Euler.y, _EulerTarget.y, ref mViewVelocityY, _OrbitSmoothing)) - Euler.y;
            _Euler.y = Euler.y + mFrameEuler.y;

            if (Mathf.Abs(mFrameEuler.y) < 0.001f && _Euler.y != _EulerTarget.y) { _Euler.y = _EulerTarget.y; }
            else if (Mathf.Abs(_EulerTarget.y - _Euler.y) < 0.001f) { _Euler.y = _EulerTarget.y; }

            if (Mathf.Abs(_EulerTarget.y - _Euler.y) < EPSILON) { mViewVelocityY = 0f; }

            // Grab the smoothed pitch
            _EulerTarget.x = Mathf.Clamp(_EulerTarget.x + mFrameEuler.x, _MinPitch, _MaxPitch);
            mFrameEuler.x = (_OrbitSmoothing <= 0f ? _EulerTarget.x : Mathf.SmoothDampAngle(_Euler.x, _EulerTarget.x, ref mViewVelocityX, _OrbitSmoothing)) - _Euler.x;
            _Euler.x = _Euler.x + mFrameEuler.x;

            if (Mathf.Abs(mFrameEuler.x) < 0.001f && _Euler.x != _EulerTarget.x) { _Euler.x = _EulerTarget.x; }
            else if (Mathf.Abs(_EulerTarget.x - _Euler.x) < 0.001f) { _Euler.x = _EulerTarget.x; }

            if (Mathf.Abs(_EulerTarget.x - _Euler.x) < EPSILON) { mViewVelocityX = 0f; }

            _Euler.x = Mathf.Clamp(_Euler.x, _MinPitch, _MaxPitch);

            // Create the rotation quaternions we'll use later
            Quaternion lViewRotationY = Quaternion.AngleAxis(_Euler.y, Vector3.up);
            Quaternion lViewRotationX = Quaternion.AngleAxis(_Euler.x, Vector3.right);

            // If we're transitioning, keep our direction based on the camera direction
            if (mFrameLockForward || (mOffsetElapsed != 0f || (mDistanceElapsed != 0f && (_AnchorOffset.x == 0f && _AnchorOffset.z == 0f))))
            {
                // Add in any change to the rotation this frame
                Quaternion lCameraRotation = _Transform.rotation * Quaternion.Euler(Vector3.up * mFrameEuler.y);
                Vector3 lCameraForward = lCameraRotation * Vector3.forward;

                // Grab the angle needed to get to our camera forward
                float lDeltaAngle = NumberHelper.GetHorizontalAngle(_Anchor.forward, lCameraForward, _Anchor.up);
                Quaternion lTargetRotation = _Anchor.rotation * Quaternion.Euler(0f, lDeltaAngle, 0f);

                // We force the view to be that of our final forward + our pitch
                lNewAnchorPosition = _Anchor.position + (lTargetRotation * mOffset);

                Vector3 lToAnchorDirection = (lTargetRotation * lViewRotationX) * Vector3.forward;
                if (lToAnchorDirection.sqrMagnitude == 0f) { lToAnchorDirection = lCameraForward; }

                lNewCameraPosition = lNewAnchorPosition - (lToAnchorDirection.normalized * mDistance);

                // Disable the force
                mFrameLockForward = false;
            }
            // At certain times, we may force the rig to face the direction of the actor
            else if (_FrameForceToFollowAnchor || mForceToFollowActor)
            {
                // We force the view to be that of our anchor + our pitch
                Vector3 lToAnchorDirection = (_Anchor.rotation * lViewRotationX) * Vector3.forward; // Quaternion.AngleAxis(lPitch2, Vector3.right)) * Vector3.forward;
                if (lToAnchorDirection.sqrMagnitude == 0f) { lToAnchorDirection = _Anchor.forward; }

                lNewCameraPosition = lNewAnchorPosition - (lToAnchorDirection.normalized * mDistance);

                // TRT 4/6/2016: Since we're forcing the camera position, force the internal values
                _Euler.y = _Anchor.rotation.eulerAngles.y;
                _EulerTarget.y = _Anchor.rotation.eulerAngles.y;

                // Disable the force
                _FrameForceToFollowAnchor = false;
            }
            // If we're not forcing a follow, do our normal processing
            else
            {
                // Put the new direction relative to to the anchor's tilt
                Vector3 lToAnchorDirection = (mTilt * lViewRotationY * lViewRotationX) * Vector3.forward; 
                if (lToAnchorDirection.sqrMagnitude == 0f) { lToAnchorDirection = _Anchor.forward; }

                // Calculate the new orbit center (anchor) and camera position
                lNewCameraPosition = lNewAnchorPosition - (lToAnchorDirection.normalized * mDistance);

                // If there was no orbiting control and we're not in the middle of tilting, 
                // we want the character to start driving the position and rotation of the rig
                if ((_AnchorOrbitsCamera &&
                     (_OrbitSmoothing > 0f && _Euler.y == _EulerTarget.y) ||
                     (_OrbitSmoothing <= 0f && mFrameEuler.y == 0f)) &&
                     lTiltDelta.IsIdentity())
                {
                    // Given our new "follow" position, grab the lateral distance. 
                    Vector3 lLateralToNewCameraPosition = lNewCameraPosition - lNewAnchorPosition;
                    lLateralToNewCameraPosition = lLateralToNewCameraPosition - Vector3.Project(lLateralToNewCameraPosition, _Anchor.up);
                    float lDistance = lLateralToNewCameraPosition.magnitude;

                    // Grab the lateral offset from our current position to the new anchor position
                    Vector3 lLateralToOldCameraPosition = _Transform.position - lNewAnchorPosition;
                    lLateralToOldCameraPosition = lLateralToOldCameraPosition - Vector3.Project(lLateralToOldCameraPosition, _Anchor.up);

                    // If we have a valid direction...
                    if (lLateralToOldCameraPosition.sqrMagnitude > 0.00001f)
                    {
                        // Use the normal and our desired distance to determine the next position of the camera.
                        // This could pull the camera forward or push the camera back as it tries to keep a hard distance.
                        lLateralToOldCameraPosition.Normalize();
                        lLateralToOldCameraPosition = lLateralToOldCameraPosition * lDistance;

                        // We want to keep the hieght of the camera that was caused by our pitch.
                        // So, we store that. 
                        Vector3 lVerticalNewCameraPosition = Vector3.Project(lNewCameraPosition, _Anchor.up);

                        // Then, we grab the new "lateral" position of our camera
                        // and re-apply the vertical position we saved.
                        Vector3 lLateralNewCameraPosition = lNewAnchorPosition + lLateralToOldCameraPosition;
                        lLateralNewCameraPosition = lLateralNewCameraPosition - Vector3.Project(lLateralNewCameraPosition, _Anchor.up);

                        // Put together the new position
                        lNewCameraPosition = lLateralNewCameraPosition + lVerticalNewCameraPosition;
                    }
                }
            }

            // Test for collision and view obstruction
            bool lObstruction = false;
            if ((_Mode == 0 && _IsObstructionsEnabled) || (_Mode > 0 && _IsAltObstructionsEnabled))
            {
                RaycastHit lViewHit;
                lObstruction = TestView(lNewAnchorPosition, ref lNewCameraPosition, _ObstructionRadius, out lViewHit);
            }

            bool lCollision = false;
            if ((_Mode == 0 && _IsCollisionsEnabled) || (_Mode > 0 && _IsAltCollisionsEnabled))
            {
                if (!lObstruction)
                {
                    RaycastHit lCollisionHit;
                    lCollision = TestCollisions(lNewAnchorPosition, _Transform.position, ref lNewCameraPosition, _CollisionRadius, out lCollisionHit);
                }
            }

            // If there is none, reset our distance
            if (!lCollision && !lObstruction)
            {
                mDistanceEnd = mActiveDistance;
                mLastCollisionDirection = Vector3.zero;
            }

            // Finally, move the camera
            //_Transform.position = lNewCameraPosition;

            Vector3 lPositionDelta = lNewCameraPosition - _Transform.position;
            Vector3 lVerticalDelta = Vector3.Project(lPositionDelta, _Anchor.up);
            Vector3 lLateralDelta = lPositionDelta - lVerticalDelta;

            _Transform.position = _Transform.position + lLateralDelta + (lVerticalDelta * 1f);

            // Grab the new rotation and set it
            Vector3 lLookDirection = (lNewAnchorPosition - lNewCameraPosition).normalized;
            if (lLookDirection.sqrMagnitude > 0f)
            {
                lNewCameraRotation = Quaternion.LookRotation(lLookDirection, _Anchor.up);
                _Transform.rotation = lNewCameraRotation;
            }

            // Grab the current "natural up" rotations
            bool lIsEqual = (_EulerTarget.y == _Euler.y);

            // ******************* UNCOMMENT OUT THIS BLOCK (BELOW)
            // lIsEqual = false;
            // ******************* UNCOMMENT OUT THIS BLOCK (ABOVE)

            float lYaw = (Quaternion.Inverse(mTilt) * lNewCameraRotation).eulerAngles.y;
            if (lYaw > 180f) { lYaw = lYaw - 360f; }
            else if (lYaw < -180f) { lYaw = lYaw + 360f; }

            _Euler.y = lYaw;
            //if (_OrbitSmoothing <= 0f || !mInputSource.IsViewingActivated) { _EulerTarget.y = lYaw; }
            if (lIsEqual || _OrbitSmoothing <= 0f || mFrameEuler.sqrMagnitude == 0f) { _EulerTarget.y = lYaw; }
            //if (mFrameEuler.sqrMagnitude == 0f && mDistance == mDistanceEnd) { _EulerTarget.y = lYaw; }

            // Clear out temp values
            mFrameEuler.y = 0f;
            mFrameEuler.x = 0f;

            // Grab the local rotation
            Quaternion lLocalRotation = _Anchor.rotation.RotationTo(_Transform.rotation);
            mLocalEuler = lLocalRotation.eulerAngles;

            if (mLocalEuler.y > 180f) { mLocalEuler.y = mLocalEuler.y - 360f; }
            else if (mLocalEuler.y < -180f) { mLocalEuler.y = mLocalEuler.y + 360f; }

            if (mLocalEuler.x > 180f) { mLocalEuler.x = mLocalEuler.x - 360f; }
            else if (mLocalEuler.x < -180f) { mLocalEuler.x = mLocalEuler.x + 360f; }

            // Determine the fade state
            float lCameraDistance = Vector3.Distance(lNewCameraPosition, lNewAnchorPosition);
            if (lCameraDistance < _FadeDistance)
            {
                mAlphaStart = mAlpha;
                mAlphaEnd = 0f;
            }
            else
            {
                mAlphaStart = mAlpha;
                mAlphaEnd = 1f;
            }

            if (mAlpha != mAlphaEnd)
            {
                mAlphaElapsed = mAlphaElapsed + Time.deltaTime;
                mAlpha = NumberHelper.SmoothStep(mAlphaStart, mAlphaEnd, (_FadeSpeed > 0f ? mAlphaElapsed / _FadeSpeed : 1f));

                if (_IsFadeEnabled) { SetAnchorAlpha(mAlpha); }
            }
            else
            {
                mAlphaElapsed = 0f;
                mAlphaStart = mAlpha;
            }

            // Apply any shake
            ProcessShake(rDeltaTime);

            //Log.FileWrite("");
        }

        /// <summary>
        /// Causes the camera to shake for the specified duration
        /// </summary>
        /// <param name="rRange">Distance the shake will apply</param>
        /// <param name="rDuration">Time (in seconds) to shake</param>
        public void Shake(float rRange, float rDuration)
        {
            mShakeElapsed = 0f;
            mShakeSpeedFactor = 1f;
            mShakeStrengthX = 1f;
            mShakeStrengthY = 1f;
            mShakeRange = rRange;
            mShakeDuration = rDuration;
        }

        /// <summary>
        /// Causes the camera to shake for the specified duration
        /// </summary>
        /// <param name="rRange">Distance the shake will apply</param>
        /// <param name="rStrengthX">Multiplier to the x-movement of the shake</param>
        /// <param name="rStrengthY">Multiplier to the y-movement of the shake</param>
        /// <param name="rDuration">Time (in seconds) to shake</param>
        public void Shake(float rRange, float rStrengthX, float rStrengthY, float rDuration)
        {
            mShakeElapsed = 0f;
            mShakeSpeedFactor = 1f;
            mShakeStrengthX = rStrengthX;
            mShakeStrengthY = rStrengthY;
            mShakeRange = rRange;
            mShakeDuration = rDuration;
        }

        /// <summary>
        /// Given the current view, force the actor to rotate to match the view
        /// </summary>
        public void ForceActorToView(float rPercent)
        {
            if (_Anchor == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(_Anchor.forward, _Transform.forward, _Anchor.up);

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, _Anchor.up);
            lRotation = Quaternion.Slerp(Quaternion.identity, lRotation, rPercent);

            // If we're dealing with a character controller, set the rotation here as well
            ICharacterController lCharacterController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
            if (lCharacterController != null)
            {
                lCharacterController.Yaw = lCharacterController.Yaw * lRotation;
                _Anchor.rotation = lCharacterController.Tilt * lCharacterController.Yaw;
            }
            else
            {
                _Anchor.rotation = lRotation * _Anchor.rotation;
            }

            // Stop the camera from rotating on it's own. This way, it doesn't rotate past the character.
            mViewVelocityX = 0f;
            mViewVelocityY = 0f;
            _EulerTarget = _Euler;
        }

        /// <summary>
        /// Manages the shake and returns the position offset for the camera inside the rig
        /// </summary>
        /// <returns></returns>
        protected void ProcessShake(float rDeltaTime)
        {
            Vector3 lCameraPosition = Vector3.zero;

            if (mShakeDuration > 0f)
            {
                mShakeElapsed = mShakeElapsed + (rDeltaTime * mShakeSpeedFactor);

                float lDuration = Mathf.Clamp01(mShakeElapsed / mShakeDuration);
                if (lDuration < 1f)
                {
                    float lStrength = _ShakeStrength.Evaluate(lDuration);
                    lCameraPosition.x = (((float)NumberHelper.Randomizer.NextDouble() * 2f) - 1f) * mShakeRange * mShakeStrengthX * lStrength;
                    lCameraPosition.y = (((float)NumberHelper.Randomizer.NextDouble() * 2f) - 1f) * mShakeRange * mShakeStrengthY * lStrength;
                }
                else
                {
                    mShakeElapsed = 0f;
                    mShakeDuration = 0f;
                }

                _Camera.transform.localPosition = lCameraPosition;
            }
        }

        /// <summary>
        /// Delegate callback for handling the camera movement AFTER the character controller
        /// </summary>
        /// <param name="rController"></param>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        protected void OnControllerLateUpdate(ICharacterController rController, float rDeltaTime, int rUpdateIndex)
        {
            RigLateUpdate(rDeltaTime, rUpdateIndex);

            // Call out to our events if needed
            if (mOnPostLateUpdate != null) { mOnPostLateUpdate(rDeltaTime, mUpdateIndex, this); }
        }

        /// <summary>
        /// Test for anything that is blocking the view between the camera and the anchor
        /// </summary>
        /// <param name="rAnchorPosition"></param>
        /// <param name="rCameraPosition"></param>
        /// <param name="rRadius"></param>
        protected bool TestView(Vector3 rAnchorPosition, ref Vector3 rCameraPosition, float rColliderRadius, out RaycastHit rViewHit)
        {
            bool lCollision = false;

            // TRT 3/24/16: Should use current offset, not the base
            //Vector3 lRayStart = _Anchor.position + (_Anchor.rotation * _AnchorOffset);
            Vector3 lRayStart = _Anchor.position + (_Anchor.rotation * mOffset);

            Vector3 lToCamera = rCameraPosition - lRayStart;
            Vector3 lRayDirection = lToCamera.normalized;
            float lRayDistance = mActiveDistance; //lToCamera.magnitude;

            // Check if our view is obstructed from the head to the camera
            if (RaycastExt.SafeSphereCast(lRayStart, lRayDirection, rColliderRadius, out rViewHit, lRayDistance, _CollisionLayers, _Anchor))
            {
                // Since it is, see if we can find a good place where the true collision radius fits
                RaycastHit lCollisionHit;
                if (((_Mode == 0 && _IsCollisionsEnabled) || (_Mode > 0 && _IsAltCollisionsEnabled)) &&
                    (RaycastExt.SafeSphereCast(lRayStart, lRayDirection, _CollisionRadius - 0.01f, out lCollisionHit, lRayDistance, _CollisionLayers, _Anchor)))
                {
                    lCollision = true;

                    rCameraPosition = lRayStart + (lRayDirection * lCollisionHit.distance);

                    mDistance = Mathf.Clamp(lCollisionHit.distance, _MinDistance, _MaxDistance);
                    mDistanceEnd = Mathf.Clamp(lCollisionHit.distance, _MinDistance, _MaxDistance);
                }
                // If we get here, it's really that the full collision doesn't fit. In this case,
                // we will ignore the real collision
                else
                {
                    lCollision = true;

                    rCameraPosition = rAnchorPosition + (lRayDirection * rViewHit.distance);

                    mDistance = rViewHit.distance;
                    mDistanceEnd = rViewHit.distance;
                }
            }
            // If there was no obstruction from the camera to the character, it could be because the
            // obstruction starts too close to our anchor. We'll do an overlap test just in case.
            else
            {
                // If there is an overlap, force the minimum distance on the camera
                Collider[] lOverlaps = null;

                int lHits = RaycastExt.SafeOverlapSphere(lRayStart, rColliderRadius, out lOverlaps, _CollisionLayers, _Anchor);
                if (lOverlaps != null && lHits > 0)
                {
                    lCollision = true;

                    rCameraPosition = rAnchorPosition + (lRayDirection * _MinDistance);

                    mDistance = _MinDistance;
                    mDistanceEnd = _MinDistance;
                }
            }

            if (lCollision)
            {
                mLastCollisionTime = Time.time;
                mLastCollisionDistance = mDistance;
            }
            else
            {
                if (mLastCollisionTime > 0f && _RecoveryDelay > 0f && mLastCollisionTime + _RecoveryDelay > Time.time)
                {
                    lCollision = true;
                    mDistance = mLastCollisionDistance;
                    mDistanceEnd = mLastCollisionDistance;
                }
            }

            return lCollision;
        }

        /// <summary>
        /// Test for anything that prevents the camera from moving to the new position
        /// </summary>
        /// <param name="rAnchorPosition"></param>
        /// <param name="rCameraPosition"></param>
        /// <param name="rRadius"></param>
        protected bool TestCollisions(Vector3 rAnchorPosition, Vector3 rPrevCameraPosition, ref Vector3 rCameraPosition, float rColliderRadius, out RaycastHit rCollisionHit)
        {
            bool lCollision = false;
            rCollisionHit = RaycastExt.EmptyHitInfo;

            // If we're too far away from the anchor, we're just going to say there is no collision
            float lAnchorDistance = Vector3.Distance(rAnchorPosition, rPrevCameraPosition);
            if (lAnchorDistance < _MaxDistance)
            {
                // Check if there's any objects we'll collide with
                Vector3 lMovement = rCameraPosition - rPrevCameraPosition;
                BodyShapeHit[] lHits = CollisionCastAll(rPrevCameraPosition, lMovement.normalized, lMovement.magnitude, _CollisionLayers);
                if (lHits != null && lHits.Length > 0)
                {
                    // Sort the collisions
                    if (lHits.Length > 1) { lHits = lHits.OrderBy(x => x.HitDistance).ToArray<BodyShapeHit>(); }

                    // Process the first collision
                    BodyShapeHit lBodyShapeHit = lHits[0];
                    if (lBodyShapeHit != null)
                    {
                        lCollision = true;

                        // Keep track of the vector so we can use it next frame to test for
                        // future collisions.
                        mLastCollisionDirection = lBodyShapeHit.HitPoint - lBodyShapeHit.HitOrigin;

                        // Process the first one
                        // If there is a positive distance, this is the initial room that
                        // we have before we get to the collider.
                        Vector3 lPreCollisionMovement = Vector3.zero;
                        if (lBodyShapeHit.HitDistance > COLLISION_BUFFER - EPSILON)
                        {
                            lPreCollisionMovement = lMovement.normalized * Mathf.Min(lBodyShapeHit.HitDistance - COLLISION_BUFFER, lMovement.magnitude);
                        }
                        // If there is a negative distance, we've penetrated the collider and
                        // we need to back up before we can continue.
                        else if (lBodyShapeHit.HitDistance < COLLISION_BUFFER + EPSILON)
                        {
                            lPreCollisionMovement = mLastCollisionDirection.normalized * (lBodyShapeHit.HitDistance - COLLISION_BUFFER);
                        }

                        // Track the amount of remaining movement we can deflect
                        Vector3 lRemainingMovement = (lMovement - lPreCollisionMovement);
                        if (lRemainingMovement.sqrMagnitude > 0f)
                        {
                            lRemainingMovement = lRemainingMovement - Vector3.Project(lRemainingMovement, lBodyShapeHit.HitNormal);

                            Vector3 lDeflectedCameraPosition = rPrevCameraPosition + lPreCollisionMovement + lRemainingMovement;
                            float lDeflectedDistance = Vector3.Distance(lDeflectedCameraPosition, rAnchorPosition);

                            // Adjust the camera distance so we slide along walls
                            mDistance = Mathf.Clamp(lDeflectedDistance, _MinDistance, _AnchorDistance * 1.25f);
                            mDistanceEnd = Mathf.Clamp(lDeflectedDistance, _MinDistance, _AnchorDistance * 1.25f);
                        }

                        Vector3 lFutureCameraPosition = rPrevCameraPosition + lPreCollisionMovement + lRemainingMovement;
                        float lFutureAnchorDistance = Vector3.Distance(rAnchorPosition, lFutureCameraPosition);
                        if (lFutureAnchorDistance < _MaxDistance)
                        {
                            // Determine the final position of the camera
                            rCameraPosition = rPrevCameraPosition + lPreCollisionMovement + lRemainingMovement;
                        }

                        //Log.FileWrite("ACR.TC() cam-pos:" + StringHelper.ToString(rCameraPosition) + " anch-dist:" + lAnchorDistance.ToString("f3") + " dist:" + mDistance.ToString("f3") + " dist-end:" + mDistanceEnd.ToString("f3"));
                        //Log.FileWrite("ACR.TC() pre-pos:" + StringHelper.ToString(rPrevCameraPosition));
                        //Log.FileWrite("ACR.TC() pre-col-pos:" + StringHelper.ToString(lPreCollisionMovement));
                        //Log.FileWrite("ACR.TC() pre-col-rem:" + StringHelper.ToString(lRemainingMovement));
                        //Log.FileWrite("ACR.TC() result dist:" + (rCameraPosition - rAnchorPosition).magnitude.ToString("f3"));

                        // Release any of our allocations
                        for (int i = 0; i < lHits.Length; i++)
                        {
                            BodyShapeHit.Release(lHits[i]);
                        }
                    }
                }
            }

            // If there's no collision, do a last check to see if we think we may collide. If
            // not, we can clean up and get out.
            if (!lCollision)
            {
                if (mLastCollisionDirection.sqrMagnitude > 0f)
                {
                    RaycastHit lTestHitInfo;
                    Vector3 lTestVector = mLastCollisionDirection.normalized;
                    float lTestDistance = mLastCollisionDirection.magnitude;

                    if (RaycastExt.SafeRaycast(rCameraPosition, lTestVector, out lTestHitInfo, lTestDistance + 0.01f, _CollisionLayers, _Anchor))
                    {
                        lCollision = true;
                    }
                }
            }

            return lCollision;
        }

        /// <summary>
        /// Casts out a shape to see if a collision will occur.
        /// </summary>
        /// <param name="rPositionDelta">Movement to add to the current position</param>
        /// <param name="rDirection">Direction of the cast</param>
        /// <param name="rDistance">Distance of the case</param>
        /// <param name="rLayerMask">Layer mask for determing what we'll collide with</param>
        /// <returns>Returns an array of BodyShapeHit values representing all the hits that take place</returns>
        protected BodyShapeHit[] CollisionCastAll(Vector3 rPosition, Vector3 rDirection, float rDistance, int rLayerMask)
        {
            Vector3 lBodyShapePos1 = rPosition;

            // Clear any existing body shape hits. They are released by the calloer
            for (int i = 0; i < mBodyShapeHitArray.Length; i++) { mBodyShapeHitArray[i] = null; }

            // Use the non-allocating version if we can
            int lHitCount = 0;

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            mRaycastHitArray = UnityEngine.Physics.SphereCastAll(lBodyShapePos1, _CollisionRadius, rDirection, rDistance + EPSILON, rLayerMask);
            if (mRaycastHitArray != null) 
            { 
                lHitCount = mRaycastHitArray.Length; 
                mBodyShapeHitArray = new BodyShapeHit[lHitCount];
            }
#else
            lHitCount = UnityEngine.Physics.SphereCastNonAlloc(lBodyShapePos1, _CollisionRadius, rDirection, mRaycastHitArray, rDistance + EPSILON, rLayerMask, QueryTriggerInteraction.Ignore);
#endif

            int lBodyShapeHitsIndex = 0;
            for (int i = 0; i < lHitCount; i++)
            {
                Transform lCurrentTransform = mRaycastHitArray[i].collider.transform;
                if (lCurrentTransform == _Transform) { continue; }

                // Ensure we're not colliding with a transform in our chain
                bool lIsValidHit = true;
                while (lCurrentTransform != null)
                {
                    if (lCurrentTransform == _Anchor)
                    {
                        lIsValidHit = false;
                        break;
                    }

                    lCurrentTransform = lCurrentTransform.parent;
                }

                if (!lIsValidHit) { continue; }

                // Once we get here, we have a valid collider
                BodyShapeHit lBodyShapeHit = BodyShapeHit.Allocate();
                lBodyShapeHit.StartPosition = lBodyShapePos1;
                lBodyShapeHit.Hit = mRaycastHitArray[i];
                lBodyShapeHit.HitOrigin = lBodyShapePos1;
                lBodyShapeHit.HitCollider = mRaycastHitArray[i].collider;
                lBodyShapeHit.HitPoint = mRaycastHitArray[i].point;
                lBodyShapeHit.HitNormal = mRaycastHitArray[i].normal;

                // This distance is the distance between the surfaces and not the start!
                lBodyShapeHit.HitDistance = mRaycastHitArray[i].distance;

                // With the sphere cast all, we can recieve hits for colliders that
                // start by intruding on the sphere. In this case, the distance is "0". So,
                // we'll find the true distance ourselves.
                if (mRaycastHitArray[i].distance == 0f)
                {
                    Vector3 lColliderPoint = Vector3.zero;

                    if (lBodyShapeHit.HitCollider is TerrainCollider)
                    {
                        lColliderPoint = GeometryExt.ClosestPoint(lBodyShapePos1, rDirection * rDistance, _CollisionRadius, (TerrainCollider)lBodyShapeHit.HitCollider);
                    }
                    else
                    {
                        lColliderPoint = GeometryExt.ClosestPoint(lBodyShapePos1, _CollisionRadius, lBodyShapeHit.HitCollider);
                    }

                    // If we don't have a valid point, we will skip
                    if (lColliderPoint == Vector3.zero)
                    {
                        BodyShapeHit.Release(lBodyShapeHit);
                        continue;
                    }

                    // If the hit is further than our radius, we can skip
                    Vector3 lHitVector = lColliderPoint - lBodyShapePos1;

                    // Setup the remaining info
                    lBodyShapeHit.HitOrigin = lBodyShapePos1;
                    lBodyShapeHit.HitPoint = lColliderPoint;

                    // We want distance between the surfaces. We have the start point and
                    // surface collider point. So, we remove our radius to get to the surface.
                    lBodyShapeHit.HitDistance = lHitVector.magnitude - _CollisionRadius;
                    lBodyShapeHit.HitPenetration = (lBodyShapeHit.HitDistance < 0f);

                    // Shoot a ray for the normal
                    RaycastHit lRaycastHitInfo;
                    if (RaycastExt.SafeRaycast(lBodyShapePos1, lHitVector.normalized, out lRaycastHitInfo, Mathf.Max(lBodyShapeHit.HitDistance + _CollisionRadius, _CollisionRadius + 0.01f)))
                    {
                        lBodyShapeHit.HitNormal = lRaycastHitInfo.normal;
                    }
                    // If the ray is so close that we can't get a result we can end up here
                    else if (lBodyShapeHit.HitDistance < EPSILON)
                    {
                        lBodyShapeHit.HitNormal = (lBodyShapePos1 - lColliderPoint).normalized;
                    }
                }

                // Add the collision info
                if (lBodyShapeHit != null)
                {
                    // Store the distance between the hit point and our character's root
                    lBodyShapeHit.HitRootDistance = _Anchor.InverseTransformPoint(lBodyShapeHit.HitPoint).y;

                    //lBodyShapeHits.Add(lBodyShapeHit);

                    // Add the valid hit to our array
                    mBodyShapeHitArray[lBodyShapeHitsIndex] = lBodyShapeHit;
                    lBodyShapeHitsIndex++;
                }
            }

            if (lBodyShapeHitsIndex == 0) { return null; }

            // Return only the valid parts of the array
            BodyShapeHit[] lBodyShapeHitArray = new BodyShapeHit[lBodyShapeHitsIndex];
            System.Array.Copy(mBodyShapeHitArray, lBodyShapeHitArray, lBodyShapeHitsIndex);
            return lBodyShapeHitArray;
        }

        /// <summary>
        /// Fades in/out the anchor
        /// </summary>
        /// <param name="rAlpha"></param>
        protected void SetAnchorAlpha(float rAlpha)
        {
            bool lIsEnabled = (rAlpha > 0f ? true : false);

            Renderer[] lRenderers = _Anchor.gameObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < lRenderers.Length; i++)
            {
                Material[] lMaterials = lRenderers[i].materials;
                for (int j = 0; j < lMaterials.Length; j++)
                {
                    if (lMaterials[j].HasProperty("_Color"))
                    {
                        Color lColor = lMaterials[j].color;
                        lColor.a = rAlpha;

                        lMaterials[j].color = lColor;
                    }
                }

                if (_DisableRenderers)
                {
                    lRenderers[i].enabled = lIsEnabled;
                }
            }
        }

        /// <summary>
        /// Grab the camera's near plane width
        /// </summary>
        /// <returns></returns>
        protected float GetNearPlaneWidth()
        {            
            float lDistance = _Camera.nearClipPlane;
            float lHalfFOV = _Camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float lHeight = (Mathf.Tan(lHalfFOV) * lDistance);
            float lWidth = (lHeight / _Camera.pixelHeight) * _Camera.pixelWidth;

            return lWidth;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

        // Values to help us manage the editor
        public int EditorTabIndex = 0;

        /// <summary>
        /// Debug
        /// </summary>
        protected void OnDrawGizmos()
        {
#if UNITY_EDITOR

            //if (_Transform != null)
            //{
            //    if ((_Mode == 0 && _IsCollisionsEnabled) || (_Mode != 0 && _IsAltCollisionsEnabled))
            //    {
            //        EditorHelper.DrawWireSphere(_Transform.position, _CollisionRadius, Color.white);
            //    }

            //    if ((_Mode == 0 && _IsObstructionsEnabled) || (_Mode > 0 && _IsAltObstructionsEnabled))
            //    {
            //        EditorHelper.DrawWireSphere(_Transform.position, _ObstructionRadius, Color.yellow);
            //        EditorHelper.DrawWireSphere(_Anchor.position + (_Anchor.rotation * _AnchorOffset), _ObstructionRadius, Color.yellow);
            //    }
            //}

#endif
        }

        // **************************************************************************************************
        // Static functions for global access
        // **************************************************************************************************

        /// <summary>
        /// Determines if the camera can use FOV zooming
        /// </summary>
        /// <param name="rEnabled"></param>
        public static void EnableAltCameraMode(bool rEnabled)
        {
            if (Camera.main == null) { return; }

            GameObject lCamera = Camera.main.transform.gameObject;
            GameObject lCameraParent = (lCamera.transform.parent != null ? lCamera.transform.parent.gameObject : null);

            AdventureRig lCameraRig = lCamera.GetComponent<AdventureRig>();
            if (lCameraRig == null && lCameraParent != null) { lCameraRig = lCameraParent.GetComponent<AdventureRig>(); }
            if (lCameraRig == null) { return; }

            lCameraRig.IsAltModeEnabled = rEnabled;

            if (!rEnabled)
            {
                lCameraRig.LockMode = false;
                lCameraRig.Mode = EnumCameraMode.THIRD_PERSON_FOLLOW;
            }
        }
    }
}

