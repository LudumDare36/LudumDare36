using System;
using UnityEngine;
using UnityEditor;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Input;

[CanEditMultipleObjects]
[CustomEditor(typeof(AdventureRig))]
public class AdventureRigEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private AdventureRig mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (AdventureRig)target;
        mTargetSO = new SerializedObject(target);

        EditorHelper.RefreshLayers();
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Adventure Rig");

        EditorHelper.DrawInspectorDescription("Advanced camera rig that mimics the behavior of AAA quality adventure games.", MessageType.None);

        GUILayout.Space(5);

        if (mTarget._Camera == null) { mTarget._Camera = Component.FindObjectOfType<Camera>(); }
        if (mTarget._Camera == null || mTarget._Camera.gameObject.transform.parent != mTarget.gameObject.transform)
        {
            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorGUILayout.HelpBox("Scene does not appear to be setup correctly. Press the button to set it up.", MessageType.Warning);

            if (GUILayout.Button("Setup Scene", EditorStyles.miniButton))
            {
                InitializeScene();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        EditorGUILayout.BeginHorizontal();

        GameObject lNewInputSourceOwner = EditorHelper.InterfaceOwnerField<IInputSource>(new GUIContent("Input Source", "Input source we'll use to get key presses, mouse movement, etc. This GameObject should have a component implementing the IInputSource interface."), mTarget.InputSourceOwner, true);
        if (lNewInputSourceOwner != mTarget.InputSourceOwner)
        {
            mIsDirty = true;
            mTarget.InputSourceOwner = lNewInputSourceOwner;
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField(new GUIContent("Find", "Determines if we attempt to automatically find the input source at startup if one isn't set."), GUILayout.Width(30));

        bool lNewAutoFindInputSource = EditorGUILayout.Toggle(mTarget.AutoFindInputSource, GUILayout.Width(16));
        if (lNewAutoFindInputSource != mTarget.AutoFindInputSource)
        {
            mIsDirty = true;
            mTarget.AutoFindInputSource = lNewAutoFindInputSource;
        }

        EditorGUILayout.EndHorizontal();

        bool lNewInvertPitch = EditorGUILayout.Toggle(new GUIContent("Invert Pitch", "Determines if the camera inverts the input when it comes to the pitch."), mTarget.InvertPitch);
        if (lNewInvertPitch != mTarget.InvertPitch)
        {
            mIsDirty = true;
            mTarget.InvertPitch = lNewInvertPitch;
        }

        Camera lCamera = mTarget.Camera;
        if (lCamera == null) { lCamera = Component.FindObjectOfType<Camera>(); }
        if (lCamera != null)
        {
            // Move it from the Unity default
            if (lCamera.nearClipPlane == 0.3f) { lCamera.nearClipPlane = 0.1f; }

            // Warn about the clipping plane distance
            if (lCamera.nearClipPlane > 0.1f)
            {
                GUILayout.Space(5);

                EditorGUILayout.BeginVertical(EditorHelper.Box);

                EditorGUILayout.HelpBox("The camera's near plane distance is currently " + lCamera.nearClipPlane + ", but should be 0.1 to prevent clipping.", MessageType.Warning);

                if (GUILayout.Button("Setup Distance", EditorStyles.miniButton))
                {
                    lCamera.nearClipPlane = 0.1f;
                }

                EditorGUILayout.EndVertical();
            }
        }

        EditorHelper.DrawLine();

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("", BasicIcon, GUILayout.Width(16), GUILayout.Height(16));

        if (GUILayout.Button("Basic", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            mTarget.EditorTabIndex = 0;
            mIsDirty = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("", AdvancedIcon, GUILayout.Width(16), GUILayout.Height(16));

        if (GUILayout.Button("Advanced", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            mTarget.EditorTabIndex = 1;
            mIsDirty = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("", DebugIcon, GUILayout.Width(16), GUILayout.Height(16)))
        {
            mTarget.EditorTabIndex = 2;
            mIsDirty = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (mTarget.EditorTabIndex == 0)
        {
            bool lIsDirty = OnBasicInspector();
            if (lIsDirty) { mIsDirty = true; }
        }
        else if (mTarget.EditorTabIndex == 1)
        {
            bool lIsDirty = OnAdvancedInspector();
            if (lIsDirty) { mIsDirty = true; }
        }
        else if (mTarget.EditorTabIndex == 2)
        {
            bool lIsDirty = OnDebugInspector();
            if (lIsDirty) { mIsDirty = true; }
        }

        GUILayout.Space(5);

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Pushes the values back to the runtime so it has the changes
            mTargetSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    /// <summary>
    /// Properties to display on the basic tab
    /// </summary>
    /// <returns></returns>
    private bool OnBasicInspector()
    {
        bool lIsDirty = false;

        EditorGUILayout.BeginVertical(Box);
        GUILayout.Space(5);

        float lNewYawSpeed = EditorGUILayout.FloatField(new GUIContent("Yaw Speed", "Degrees per second the camera orbits the anchor."), mTarget.YawSpeed);
        if (lNewYawSpeed != mTarget.YawSpeed)
        {
            mIsDirty = true;
            mTarget.YawSpeed = lNewYawSpeed;
        }

        float lNewPitchSpeed = EditorGUILayout.FloatField(new GUIContent("Pitch Speed", "Degrees per second the camera orbits the anchor."), mTarget.PitchSpeed);
        if (lNewPitchSpeed != mTarget.PitchSpeed)
        {
            mIsDirty = true;
            mTarget.PitchSpeed = lNewPitchSpeed;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        GUILayout.Space(5);

        Transform lNewAnchor = EditorGUILayout.ObjectField(new GUIContent("Anchor", "Transform the camera is meant to follow."), mTarget.Anchor, typeof(Transform), true) as Transform;
        if (lNewAnchor != mTarget.Anchor)
        {
            lIsDirty = true;
            mTarget.Anchor = lNewAnchor;
        }

        Vector3 lNewAnchorOffset = EditorGUILayout.Vector3Field(new GUIContent("Anchor Offset", "Offset from the transform that represents the true anchor."), mTarget.AnchorOffset);
        if (lNewAnchorOffset != mTarget.AnchorOffset)
        {
            lIsDirty = true;
            mTarget.AnchorOffset = lNewAnchorOffset;
        }

        float lNewAnchorDistance = EditorGUILayout.FloatField(new GUIContent("Anchor Distance", "Orbit distance the camera is from the anchor."), mTarget.AnchorDistance);
        if (lNewAnchorDistance != mTarget.AnchorDistance)
        {
            lIsDirty = true;
            mTarget.AnchorDistance = lNewAnchorDistance;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);

        EditorHelper.DrawSmallTitle("Options");

        EditorHelper.DrawInspectorDescription("Select the desired options for your camera.", MessageType.None);
        GUILayout.Space(5);

        // Colliding
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        bool lNewTestCollisions = EditorGUILayout.Toggle(mTarget.IsObstructionsEnabled, OptionToggle, GUILayout.Width(16));
        if (lNewTestCollisions != mTarget.IsObstructionsEnabled)
        {
            lIsDirty = true;
            mTarget.IsObstructionsEnabled = lNewTestCollisions;
        }

        EditorGUILayout.LabelField("Use line-of-sight testing", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        if (lNewTestCollisions)
        {
            int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("", "Layers that identifies objects that will block the camera's movement."), mTarget.CollisionLayers, GUILayout.Width(100));
            if (lNewCollisionLayers != mTarget.CollisionLayers)
            {
                lIsDirty = true;
                mTarget.CollisionLayers = lNewCollisionLayers;
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Aiming
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        bool lNewIsAltModeEnabled = EditorGUILayout.Toggle(mTarget.IsAltModeEnabled, OptionToggle, GUILayout.Width(16));
        if (lNewIsAltModeEnabled != mTarget.IsAltModeEnabled)
        {
            lIsDirty = true;
            mTarget.IsAltModeEnabled = lNewIsAltModeEnabled;
        }

        EditorGUILayout.LabelField("Target by swapping camera mode", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        // Zooming
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        bool lNewIsAltFieldOfViewEnabled = EditorGUILayout.Toggle(mTarget.IsAltFieldOfViewEnabled, OptionToggle, GUILayout.Width(16));
        if (lNewIsAltFieldOfViewEnabled != mTarget.IsAltFieldOfViewEnabled)
        {
            lIsDirty = true;
            mTarget.IsAltFieldOfViewEnabled = lNewIsAltFieldOfViewEnabled;
        }

        EditorGUILayout.LabelField("Zoom using the field-of-view", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);

        EditorGUILayout.EndVertical();


        return lIsDirty;
    }

    /// <summary>
    /// Displays the properties for the advanced tab
    /// </summary>
    /// <returns></returns>
    private bool OnAdvancedInspector()
    {
        bool lIsDirty = false;

        EditorGUILayout.BeginVertical(Box);
        GUILayout.Space(5);

        bool lNewIsInternalUpdateEnabled = EditorGUILayout.Toggle(new GUIContent("Force Update", "Determines if we allow the camera rig to update itself or if something like the Actor Controller will tell the camera when to update."), mTarget.IsInternalUpdateEnabled);
        if (lNewIsInternalUpdateEnabled != mTarget.IsInternalUpdateEnabled)
        {
            mIsDirty = true;
            mTarget.IsInternalUpdateEnabled = lNewIsInternalUpdateEnabled;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        GUILayout.Space(5);

        float lNewOrbitSmoothing = EditorGUILayout.FloatField(new GUIContent("Rotation Smoothing", "Power of the smoothing function to ease rotations. (0=none, 1=lots)"), mTarget.OrbitSmoothing);
        if (lNewOrbitSmoothing != mTarget.OrbitSmoothing)
        {
            mIsDirty = true;
            mTarget.OrbitSmoothing = lNewOrbitSmoothing;
        }

        GUILayout.Space(5);

        float lNewMinDistance = EditorGUILayout.FloatField(new GUIContent("Min Distance", "Minimum distance the camera can get to the anchor."), mTarget.MinDistance);
        if (lNewMinDistance != mTarget.MinDistance)
        {
            mIsDirty = true;
            mTarget.MinDistance = lNewMinDistance;
        }

        float lNewMaxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "Maximum distance the camera can get from the anchor."), mTarget.MaxDistance);
        if (lNewMaxDistance != mTarget.MaxDistance)
        {
            mIsDirty = true;
            mTarget.MaxDistance = lNewMaxDistance;
        }

        GUILayout.Space(5);

        float lNewMinPitch = EditorGUILayout.FloatField(new GUIContent("Min Pitch", "Minimum pitch the camera can get to. Pitch values are from -87 to 87 to prevent flipping."), mTarget.MinPitch);
        if (lNewMinPitch != mTarget.MinPitch)
        {
            mIsDirty = true;
            mTarget.MinPitch = lNewMinPitch;
        }

        float lNewMaxPitch = EditorGUILayout.FloatField(new GUIContent("Max Pitch", "Maximum pitch the camera can get to. Pitch values are from -87 to 87 to prevent flipping."), mTarget.MaxPitch);
        if (lNewMaxPitch != mTarget.MaxPitch)
        {
            mIsDirty = true;
            mTarget.MaxPitch = lNewMaxPitch;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        GUILayout.Space(5);

        Transform lNewAnchor = EditorGUILayout.ObjectField(new GUIContent("Anchor", "Transform the camera is meant to follow."), mTarget.Anchor, typeof(Transform), true) as Transform;
        if (lNewAnchor != mTarget.Anchor)
        {
            lIsDirty = true;
            mTarget.Anchor = lNewAnchor;
        }

        Vector3 lNewAnchorOffset = EditorGUILayout.Vector3Field(new GUIContent("Anchor Offset", "Offset from the transform that represents the true anchor."), mTarget.AnchorOffset);
        if (lNewAnchorOffset != mTarget.AnchorOffset)
        {
            lIsDirty = true;
            mTarget.AnchorOffset = lNewAnchorOffset;
        }

        float lNewAnchorDistance = EditorGUILayout.FloatField(new GUIContent("Anchor Distance", "Orbit distance from the anchor."), mTarget.AnchorDistance);
        if (lNewAnchorDistance != mTarget.AnchorDistance)
        {
            lIsDirty = true;
            mTarget.AnchorDistance = lNewAnchorDistance;
        }

        bool lNewAnchorOrbitsCamera = EditorGUILayout.Toggle(new GUIContent("Anchor Orbits Camera", "Determines if the player walks around the camera."), mTarget.AnchorOrbitsCamera);
        if (lNewAnchorOrbitsCamera != mTarget.AnchorOrbitsCamera)
        {
            mIsDirty = true;
            mTarget.AnchorOrbitsCamera = lNewAnchorOrbitsCamera;
        }

        float lNewAnchorTime = EditorGUILayout.FloatField(new GUIContent("Transition Time", "Time (in seconds) to transition to this mode."), mTarget.AnchorTime);
        if (lNewAnchorTime != mTarget.AnchorTime)
        {
            mIsDirty = true;
            mTarget.AnchorTime = lNewAnchorTime;
        }

        float lNewYawSpeed = EditorGUILayout.FloatField(new GUIContent("Yaw Speed", "Degrees per second the camera orbits the anchor."), mTarget.YawSpeed);
        if (lNewYawSpeed != mTarget.YawSpeed)
        {
            mIsDirty = true;
            mTarget.YawSpeed = lNewYawSpeed;
        }

        float lNewPitchSpeed = EditorGUILayout.FloatField(new GUIContent("Pitch Speed", "Degrees per second the camera orbits the anchor."), mTarget.PitchSpeed);
        if (lNewPitchSpeed != mTarget.PitchSpeed)
        {
            mIsDirty = true;
            mTarget.PitchSpeed = lNewPitchSpeed;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        EditorHelper.DrawSmallTitle("Targeting Mode Properties");

        bool lNewIsAltModeEnabled = EditorGUILayout.Toggle(new GUIContent("Is Targeting Enabled", "Determines if the player can use the alternate targeting mode."), mTarget.IsAltModeEnabled);
        if (lNewIsAltModeEnabled != mTarget.IsAltModeEnabled)
        {
            mIsDirty = true;
            mTarget.IsAltModeEnabled = lNewIsAltModeEnabled;
        }

        string lNewAltActionAlias = EditorGUILayout.TextField(new GUIContent("Targeting Alias", "Input used to activate the alternate targeting mode."), mTarget.AltActionAlias);
        if (lNewAltActionAlias != mTarget.AltActionAlias)
        {
            mIsDirty = true;
            mTarget.AltActionAlias = lNewAltActionAlias;
        }

        bool lNewAltActionAliasAsToggle = EditorGUILayout.Toggle(new GUIContent("Targeting As Toggle", "Determines if the alias works as a toggle or needs to be held for the alternate targeting mode"), mTarget.AltActionAliasAsToggle);
        if (lNewAltActionAliasAsToggle != mTarget.AltActionAliasAsToggle)
        {
            mIsDirty = true;
            mTarget.AltActionAliasAsToggle = lNewAltActionAliasAsToggle;
        }

        Vector3 lNewAltAnchorOffset = EditorGUILayout.Vector3Field(new GUIContent("Targeting Anchor Offset", "Position of the camera relative to the anchor."), mTarget.AltAnchorOffset);
        if (lNewAltAnchorOffset != mTarget.AltAnchorOffset)
        {
            mIsDirty = true;
            mTarget.AltAnchorOffset = lNewAltAnchorOffset;
        }

        float lNewAltAnchorDistance = EditorGUILayout.FloatField(new GUIContent("Targeting Anchor Distance", "Distance from the anchor."), mTarget.AltAnchorDistance);
        if (lNewAltAnchorDistance != mTarget.AltAnchorDistance)
        {
            mIsDirty = true;
            mTarget.AltAnchorDistance = lNewAltAnchorDistance;
        }

        float lNewAltPitchSpeed = EditorGUILayout.FloatField(new GUIContent("Pitch Speed", "Degrees per second the view pitches while targeting."), mTarget.AltPitchSpeed);
        if (lNewAltPitchSpeed != mTarget.AltPitchSpeed)
        {
            mIsDirty = true;
            mTarget.AltPitchSpeed = lNewAltPitchSpeed;
        }

        float lNewAltAnchorTime = EditorGUILayout.FloatField(new GUIContent("Transition Time", "Time (in seconds) to transition to this mode."), mTarget.AltAnchorTime);
        if (lNewAltAnchorTime != mTarget.AltAnchorTime)
        {
            mIsDirty = true;
            mTarget.AltAnchorTime = lNewAltAnchorTime;
        }

        bool lNewAltForceActorToView = EditorGUILayout.Toggle(new GUIContent("Start Actor at View", "Determines if we rotate the actor to look at our current view when targeting starts."), mTarget.AltForceActorToView);
        if (lNewAltForceActorToView != mTarget.AltForceActorToView)
        {
            mIsDirty = true;
            mTarget.AltForceActorToView = lNewAltForceActorToView;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        EditorHelper.DrawSmallTitle("Collision Properties");

        if ((mTarget.IsCollisionsEnabled && mTarget.IsObstructionsEnabled) || (mTarget.IsAltCollisionsEnabled && mTarget.IsAltObstructionsEnabled))
        {
            EditorGUILayout.HelpBox("Having both collisions and line-of-sight testing enabled could cause some popping.", MessageType.Warning);
        }

        bool lNewTestCollisions = EditorGUILayout.Toggle(new GUIContent("Is Collision Enabled", "Deterines if we can collide with obstacles in the normal mode."), mTarget.IsCollisionsEnabled);
        if (lNewTestCollisions != mTarget.IsCollisionsEnabled)
        {
            mIsDirty = true;
            mTarget.IsCollisionsEnabled = lNewTestCollisions;
        }

        bool lNewAltTestCollisions = EditorGUILayout.Toggle(new GUIContent("Is Targeting Collision Enabled", "Determines if we can collide with obstacles in the targeting mode."), mTarget.IsAltCollisionsEnabled);
        if (lNewAltTestCollisions != mTarget.IsAltCollisionsEnabled)
        {
            mIsDirty = true;
            mTarget.IsAltCollisionsEnabled = lNewAltTestCollisions;
        }

        bool lNewAutoCollisionRadius = EditorGUILayout.Toggle(new GUIContent("Auto Collision Radius", "Automatically sets the collider radius to cover the camera's near plane."), mTarget.AutoCollisionRadius);
        if (lNewAutoCollisionRadius != mTarget.AutoCollisionRadius)
        {
            mIsDirty = true;
            mTarget.AutoCollisionRadius = lNewAutoCollisionRadius;
        }

        if (lNewAutoCollisionRadius)
        {
            float lNewAutoCollisionRadiusFactor = EditorGUILayout.FloatField(new GUIContent("Auto Radius Factor", "Multiplier to use when automatically setting the collider radius."), mTarget.AutoCollisionRadiusFactor);
            if (lNewAutoCollisionRadiusFactor != mTarget.AutoCollisionRadiusFactor)
            {
                mIsDirty = true;
                mTarget.AutoCollisionRadiusFactor = lNewAutoCollisionRadiusFactor;
            }
        }
        else
        {
            float lNewCollisionRadius = EditorGUILayout.FloatField(new GUIContent("Collision Radius", "Radius of the collider used by the camera."), mTarget.CollisionRadius);
            if (lNewCollisionRadius != mTarget.CollisionRadius)
            {
                mIsDirty = true;
                mTarget.CollisionRadius = lNewCollisionRadius;
            }
        }

        GUILayout.Space(5);

        bool lNewTestObstructions = EditorGUILayout.Toggle(new GUIContent("Is LOS Enabled", "Deterines if we use line-of-sight test for objects blocing the view in the normal mode."), mTarget.IsObstructionsEnabled);
        if (lNewTestObstructions != mTarget.IsObstructionsEnabled)
        {
            mIsDirty = true;
            mTarget.IsObstructionsEnabled = lNewTestObstructions;
        }

        bool lNewAltTestObstructions = EditorGUILayout.Toggle(new GUIContent("Is Targeting LOS Enabled", "Determines if we test for objects blocking the view in the targeting mode."), mTarget.IsAltObstructionsEnabled);
        if (lNewAltTestObstructions != mTarget.IsAltObstructionsEnabled)
        {
            mIsDirty = true;
            mTarget.IsAltObstructionsEnabled = lNewAltTestObstructions;
        }

        bool lNewAutoObstructionRadius = EditorGUILayout.Toggle(new GUIContent("Auto LOS Radius", "Automatically sets the obstruction radius to cover the camera's near plane."), mTarget.AutoObstructionRadius);
        if (lNewAutoObstructionRadius != mTarget.AutoObstructionRadius)
        {
            mIsDirty = true;
            mTarget.AutoObstructionRadius = lNewAutoObstructionRadius;
        }

        if (lNewAutoObstructionRadius)
        {
            float lNewAutoCollisionRadiusFactor = EditorGUILayout.FloatField(new GUIContent("Auto Radius Factor", "Multiplier to use when automatically setting the obstruction radius."), mTarget.AutoCollisionRadiusFactor);
            if (lNewAutoCollisionRadiusFactor != mTarget.AutoCollisionRadiusFactor)
            {
                mIsDirty = true;
                mTarget.AutoCollisionRadiusFactor = lNewAutoCollisionRadiusFactor;
            }
        }
        else
        {
            float lNewObstructionRadius = EditorGUILayout.FloatField(new GUIContent("LOS Radius", "Radius of the collider used by the camera."), mTarget.ObstructionRadius);
            if (lNewObstructionRadius != mTarget.ObstructionRadius)
            {
                mIsDirty = true;
                mTarget.ObstructionRadius = lNewObstructionRadius;
            }
        }

        float lNewRecoveryDelay = EditorGUILayout.FloatField(new GUIContent("LOS Recovery Delay", "After a collision, the number of seconds before we head back to our desired distance."), mTarget.RecoveryDelay);
        if (lNewRecoveryDelay != mTarget.RecoveryDelay)
        {
            mIsDirty = true;
            mTarget.RecoveryDelay = lNewRecoveryDelay;
        }

        GUILayout.Space(5);

        int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers that identies objects the camera will collide with."), mTarget.CollisionLayers);
        if (lNewCollisionLayers != mTarget.CollisionLayers)
        {
            mIsDirty = true;
            mTarget.CollisionLayers = lNewCollisionLayers;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        EditorHelper.DrawSmallTitle("Fade Properties");

        if ((mTarget.IsFadeEnabed))
        {
            EditorGUILayout.HelpBox("True fading requires character shaders to use transparency." + (mTarget.DisableRenderers ? "" : "Disabling renderers works with opaque or transparent shaders."), MessageType.Warning);
        }

        bool lNewIsFadeEnabled = EditorGUILayout.Toggle(new GUIContent("Is Fade Enabled", "Determines if we fade the anchor when the camera is too close. This requires the anchor shaders to use transparencies."), mTarget.IsFadeEnabed);
        if (lNewIsFadeEnabled != mTarget.IsFadeEnabed)
        {
            mIsDirty = true;
            mTarget.IsFadeEnabed = lNewIsFadeEnabled;
        }

        float lNewFadeDistance = EditorGUILayout.FloatField(new GUIContent("Fade Distance", "Distance between the anchor and camera where we start fading out."), mTarget.FadeDistance);
        if (lNewFadeDistance != mTarget.FadeDistance)
        {
            mIsDirty = true;
            mTarget.FadeDistance = lNewFadeDistance;
        }

        float lNewFadeSpeed = EditorGUILayout.FloatField(new GUIContent("Fade Speed", "Time (in seconds) to fade the anchor in and out."), mTarget.FadeSpeed);
        if (lNewFadeSpeed != mTarget.FadeSpeed)
        {
            mIsDirty = true;
            mTarget.FadeSpeed = lNewFadeSpeed;
        }

        bool lNewDisableRenderers = EditorGUILayout.Toggle(new GUIContent("Disable Renderers", "Determines if the anchor's renderers are disabled when fading is complete. This works for non-transparent shaders too."), mTarget.DisableRenderers);
        if (lNewDisableRenderers != mTarget.DisableRenderers)
        {
            mIsDirty = true;
            mTarget.DisableRenderers = lNewDisableRenderers;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        EditorHelper.DrawSmallTitle("Zoom Properties");

        bool lNewIsFieldOfViewEnabled = EditorGUILayout.Toggle(new GUIContent("Is FOV Enabled", "Determines if we allow changing the field-of-view while in normal mode."), mTarget.IsFieldOfViewEnabled);
        if (lNewIsFieldOfViewEnabled != mTarget.IsFieldOfViewEnabled)
        {
            mIsDirty = true;
            mTarget.IsFieldOfViewEnabled = lNewIsFieldOfViewEnabled;
        }

        bool lNewIsAltFieldOfViewEnabled = EditorGUILayout.Toggle(new GUIContent("Is Targeting FOV Enabled", "Determines if we allow changing the field-of-view while in targeting mode."), mTarget.IsAltFieldOfViewEnabled);
        if (lNewIsAltFieldOfViewEnabled != mTarget.IsAltFieldOfViewEnabled)
        {
            mIsDirty = true;
            mTarget.IsAltFieldOfViewEnabled = lNewIsAltFieldOfViewEnabled;
        }

        string lNewFieldOfViewActionAlias = EditorGUILayout.TextField(new GUIContent("FOV Action Alias", "Input alias used to get the field-of-view delta each frame."), mTarget.FieldOfViewActionAlias);
        if (lNewFieldOfViewActionAlias != mTarget.FieldOfViewActionAlias)
        {
            mIsDirty = true;
            mTarget.FieldOfViewActionAlias = lNewFieldOfViewActionAlias;
        }

        if (lNewFieldOfViewActionAlias == "Camera FOV")
        {
            if (!InputManagerHelper.IsDefined("Camera FOV"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera FOV";
                lEntry.Gravity = 0f;
                lEntry.Dead = 0f;
                lEntry.Sensitivity = 0.1f;
                lEntry.Type = InputManagerEntryType.MOUSE_MOVEMENT;
                lEntry.Axis = 3;
                lEntry.JoyNum = 0;
                InputManagerHelper.AddEntry(lEntry);

                lEntry = new InputManagerEntry();
                lEntry.Name = "Camera FOV";
                lEntry.Gravity = 1f;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1f;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                lEntry.Axis = 6;
#else
                lEntry.Axis = 10;
#endif
                lEntry.JoyNum = 0;
                InputManagerHelper.AddEntry(lEntry, true);
            }
        }

        float lNewMinFieldOfView = EditorGUILayout.FloatField(new GUIContent("Min FOV", "Minimum field of view (tightest zoom) allowed."), mTarget.MinFieldOfView);
        if (lNewMinFieldOfView != mTarget.MinFieldOfView)
        {
            mIsDirty = true;
            mTarget.MinFieldOfView = lNewMinFieldOfView;
        }

        float lNewFieldOfViewSpeed = EditorGUILayout.FloatField(new GUIContent("FOV Speed", "Speed (in seconds) at which we change the field-of-view."), mTarget.FieldOfViewSpeed);
        if (lNewFieldOfViewSpeed != mTarget.FieldOfViewSpeed)
        {
            mIsDirty = true;
            mTarget.FieldOfViewSpeed = lNewFieldOfViewSpeed;
        }

        float lNewFieldOfViewSmoothing = EditorGUILayout.FloatField(new GUIContent("FOV Smoothing", "Smoothing applied to field-of-view changes. (0=none, 1=lots)"), mTarget.FieldOfViewSmoothing);
        if (lNewFieldOfViewSmoothing != mTarget.FieldOfViewSmoothing)
        {
            mIsDirty = true;
            mTarget.FieldOfViewSmoothing = lNewFieldOfViewSmoothing;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.BeginVertical(Box);
        EditorHelper.DrawSmallTitle("Camera Shake Properties");

        // Determine how gravity is applied
        AnimationCurve lNewShakeStrength = EditorGUILayout.CurveField(new GUIContent("Shake Strength", "Determines how strong the shake is over the duration. (0 = none, 1 = 100%)"), mTarget.ShakeStrength);
        if (lNewShakeStrength != mTarget.ShakeStrength)
        {
            mIsDirty = true;
            mTarget.ShakeStrength = lNewShakeStrength;
        }

        GUILayout.Space(3);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        //float lNewDistanceEnd = EditorGUILayout.FloatField(new GUIContent("Distance End", ""), mTarget.mDistanceEnd);
        //if (lNewDistanceEnd != mTarget.mDistanceEnd)
        //{
        //    mIsDirty = true;
        //    mTarget.mDistanceEnd = lNewDistanceEnd;
        //}

        //float lNewLocalYaw = EditorGUILayout.FloatField(new GUIContent("Local Yaw", ""), mTarget.LocalYaw);
        //if (lNewLocalYaw != mTarget.LocalYaw)
        //{
        //    mIsDirty = true;
        //    mTarget.LocalYaw = lNewLocalYaw;
        //}

        //float lNewLocalPitch = EditorGUILayout.FloatField(new GUIContent("Local Pitch", ""), mTarget.LocalPitch);
        //if (lNewLocalPitch != mTarget.LocalPitch)
        //{
        //    mIsDirty = true;
        //    mTarget.LocalPitch = lNewLocalPitch;
        //}

        return lIsDirty;
    }

    /// <summary>
    /// Properties to display on the debug tab
    /// </summary>
    /// <returns></returns>
    private bool OnDebugInspector()
    {
        bool lIsDirty = false;

        EditorGUILayout.LabelField("Debug Info:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(string.Format("Mode:{0} AOC:{1}", mTarget.Mode, mTarget.AnchorOrbitsCamera));
        EditorGUILayout.LabelField(string.Format("ColRadius: {0:f3}  ObsRadius: {1:f3}", mTarget.CollisionRadius, mTarget.ObstructionRadius));
        EditorGUILayout.LabelField(string.Format("Frame-FTF:{0}  FTF:{1}", mTarget.FrameForceToFollowAnchor, mTarget.ForceToFollowAnchor));
        GUILayout.Space(5);
        EditorGUILayout.LabelField(string.Format("Dist: {0:f6}  Target: {1:f6}", mTarget.ActualDistance, mTarget.DistanceEnd));
        GUILayout.Space(5);
        EditorGUILayout.LabelField(string.Format("Euler : {0:f6}, {1:f6}, {2:f6}", mTarget.Euler.x, mTarget.Euler.y, mTarget.Euler.z));
        EditorGUILayout.LabelField(string.Format("Frame : {0:f6}, {1:f6}, {2:f6}", mTarget.FrameEuler.x, mTarget.FrameEuler.y, mTarget.FrameEuler.z));
        EditorGUILayout.LabelField(string.Format("Target: {0:f6}, {1:f6}, {2:f6}", mTarget.EulerTarget.x, mTarget.EulerTarget.y, mTarget.EulerTarget.z));

        return lIsDirty;
    }

    /// <summary>
    /// Test for scene components we expect and create them if needed.
    /// </summary>
    private void InitializeScene()
    {
        // Get or create the input source
        IInputSource lInputSource = CreateInputSource<UnityInputSource>();
        lInputSource.IsEnabled = true;

        GameObject lInputSourceGO = null;
        if (lInputSource is MonoBehaviour)
        {
            lInputSourceGO = ((MonoBehaviour)lInputSource).gameObject;
        }

        mTarget.InputSourceOwner = lInputSourceGO;

        // Attempt to find the anchor and set it if it doesn't exist
        if (mTarget.Anchor == null && ReflectionHelper.IsTypeValid("com.ootii.Actors.AnimationControllers.MotionController, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            UnityEngine.Object lObject = Component.FindObjectOfType(Type.GetType("com.ootii.Actors.AnimationControllers.MotionController, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
            if (lObject != null) { mTarget.Anchor = ((MonoBehaviour)lObject).gameObject.transform; }
        }

        if (mTarget.Anchor == null && ReflectionHelper.IsTypeValid("com.ootii.Actors.ActorController, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
        {
            UnityEngine.Object lObject = Component.FindObjectOfType(Type.GetType("com.ootii.Actors.ActorController, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
            if (lObject != null) { mTarget.Anchor = ((MonoBehaviour)lObject).gameObject.transform; }
        }

        if (mTarget.Anchor == null)
        {
            CharacterController lController = Component.FindObjectOfType<CharacterController>();
            if (lController != null) { mTarget.Anchor = lController.gameObject.transform; }
        }

        if (mTarget.Anchor == null)
        { 
            Animator lAnimator = Component.FindObjectOfType<Animator>();
            if (lAnimator != null) { mTarget.Anchor = lAnimator.gameObject.transform; }
        }

        // Ensure the camera is a child
        Camera lCamera = Camera.main;
        if (lCamera == null) { lCamera = mTarget.gameObject.GetComponent<Camera>(); }
        if (lCamera == null) { lCamera = mTarget.gameObject.GetComponentInChildren<Camera>(); }
        if (lCamera == null) { lCamera = Component.FindObjectOfType<Camera>(); }

        // Create the camera if we need to
        if (lCamera == null)
        {
            GameObject lCameraGO = new GameObject("Main Camera");
            lCamera = lCameraGO.AddComponent<Camera>();

            lCamera.nearClipPlane = 0.1f;
        }
        // Move the camera down if we need to
        else if (lCamera.gameObject.transform == mTarget.gameObject.transform)
        {
            GameObject lCameraGO = Instantiate<GameObject>(mTarget.gameObject);
            if (lCameraGO != null)
            {
                lCameraGO.name = mTarget.name;

                lCamera = lCameraGO.GetComponent<Camera>();
                lCamera.nearClipPlane = 0.1f;

                DestroyImmediate(lCameraGO.GetComponent<AdventureRig>());
            }

            Camera lOldCamera = null;
            Component[] lComponents = mTarget.gameObject.GetComponents<Component>();
            for (int i = 0; i < lComponents.Length; i++)
            {
                if (lComponents[i] == mTarget || lComponents[i] is Transform)
                {
                    continue;
                }
                else if (lComponents[i] is Camera)
                {
                    lOldCamera = lComponents[i] as Camera;
                }
                else
                {
                    DestroyImmediate(lComponents[i]);
                }
            }

            if (lOldCamera != null) { DestroyImmediate(lOldCamera); }

            mTarget.name = "Camera Rig";
        }

        if (lCamera != null)
        {
            GameObject lCameraGO = lCamera.gameObject;
            lCameraGO.transform.parent = mTarget.gameObject.transform;
            lCameraGO.transform.localPosition = Vector3.zero;
            lCameraGO.transform.localRotation = Quaternion.identity;
            lCameraGO.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Creates the camera rig if need and returns the GO
    /// </summary>
    /// <returns></returns>
    private IInputSource CreateInputSource<T>() where T : IInputSource
    {
        IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
        if (lInputSources != null && lInputSources.Length > 0) { return lInputSources[0]; }

        // Create the input source
        GameObject lInputSourceGO = new GameObject("Input Source");
        T lInputSource = (T)((object)lInputSourceGO.AddComponent(typeof(T)));

        return lInputSource;
    }

    /// <summary>
    /// Label
    /// </summary>
    private static GUIStyle mOptionText = null;
    private static GUIStyle OptionText
    {
        get
        {
            if (mOptionText == null)
            {
                mOptionText = new GUIStyle(GUI.skin.label);
                mOptionText.wordWrap = true;
                mOptionText.padding.top = 11;
            }

            return mOptionText;
        }
    }

    /// <summary>
    /// Label
    /// </summary>
    private static GUIStyle mOptionToggle = null;
    private static GUIStyle OptionToggle
    {
        get
        {
            if (mOptionToggle == null)
            {
                mOptionToggle = new GUIStyle(GUI.skin.toggle);
            }

            return mOptionToggle;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mBasicIcon = null;
    private static GUIStyle BasicIcon
    {
        get
        {
            if (mBasicIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "BasicIcon_pro" : "BasicIcon");

                mBasicIcon = new GUIStyle(GUI.skin.button);
                mBasicIcon.normal.background = lTexture;
                mBasicIcon.padding = new RectOffset(0, 0, 0, 0);
                mBasicIcon.margin = new RectOffset(0, 0, 1, 0);
                mBasicIcon.border = new RectOffset(0, 0, 0, 0);
                mBasicIcon.stretchHeight = false;
                mBasicIcon.stretchWidth = false;

            }

            return mBasicIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mAdvancedIcon = null;
    private static GUIStyle AdvancedIcon
    {
        get
        {
            if (mAdvancedIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "AdvancedIcon_pro" : "AdvancedIcon");

                mAdvancedIcon = new GUIStyle(GUI.skin.button);
                mAdvancedIcon.normal.background = lTexture;
                mAdvancedIcon.padding = new RectOffset(0, 0, 0, 0);
                mAdvancedIcon.margin = new RectOffset(0, 0, 1, 0);
                mAdvancedIcon.border = new RectOffset(0, 0, 0, 0);
                mAdvancedIcon.stretchHeight = false;
                mAdvancedIcon.stretchWidth = false;

            }

            return mAdvancedIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mDebugIcon = null;
    private static GUIStyle DebugIcon
    {
        get
        {
            if (mDebugIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "DebugIcon_pro" : "DebugIcon");

                mDebugIcon = new GUIStyle(GUI.skin.button);
                mDebugIcon.normal.background = lTexture;
                mDebugIcon.padding = new RectOffset(0, 0, 0, 0);
                mDebugIcon.margin = new RectOffset(0, 0, 1, 0);
                mDebugIcon.border = new RectOffset(0, 0, 0, 0);
                mDebugIcon.stretchHeight = false;
                mDebugIcon.stretchWidth = false;

            }

            return mDebugIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mBox = null;
    private static GUIStyle Box
    {
        get
        {
            if (mBox == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "Editor/GroupBox_pro" : "Editor/OrangeGrayBox");

                mBox = new GUIStyle(GUI.skin.box);
                mBox.normal.background = lTexture;
                mBox.padding = new RectOffset(0, 0, 0, 0);
                mBox.margin = new RectOffset(0, 0, 0, 0);
            }

            return mBox;
        }
    }
}
