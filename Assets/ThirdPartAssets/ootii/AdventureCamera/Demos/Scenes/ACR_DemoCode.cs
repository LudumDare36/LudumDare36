using UnityEngine;
using com.ootii.Actors;
using com.ootii.Cameras;
using com.ootii.Utilities.Debug;

namespace com.ootii.Demos
{
    public class ACR_DemoCode : MonoBehaviour
    {
        AdventureRig mAdventureRig = null;

        AdventureController mAdventureController = null;

        // Use this for initialization
        void Start()
        {
            mAdventureRig = Component.FindObjectOfType<AdventureRig>();
            mAdventureController = Component.FindObjectOfType<AdventureController>();
        }

        // Update is called once per frame
        void Update()
        {
            if (mAdventureController == null) { return; }

            if (mAdventureController.Stance == 2)
            {
                Log.ScreenWrite("AdventureController.Update - Controller Mode: COMBAT_RANGED", 1);
            }
            else if (mAdventureController.Stance == 1)
            {
                Log.ScreenWrite("AdventureController.Update - Controller Mode: COMBAT_MELEE", 1);
            }
            else
            {
                Log.ScreenWrite("AdventureController.Update - Controller Mode: EXPLORATION", 1);
            }

            // Finally print out info for player
            Log.ScreenWrite("WASD Keys = Movement", 3);
            Log.ScreenWrite("Right Mouse Button Down = View", 4);
            Log.ScreenWrite("Middle Mouse Button Down = Aim", 5);
            Log.ScreenWrite("Mouse Scroll Wheel (in Aim) = Zoom", 6);
            Log.ScreenWrite("T = Switch between Exploration and Combat movement", 7);
        }

        // Used to render to the screen
        void OnGUI()
        {
            // Show buttons for the shake
            Rect lShakeRect = new Rect(10, 160, 80, 20);
            if (GUI.Button(lShakeRect, new GUIContent("Shake", "")))
            {
                mAdventureRig.Shake(0.02f, 1f, 0.5f, 1f);
            }

            // Show buttons for the shake
            Rect lBigShakeRect = new Rect(100, 160, 80, 20);
            if (GUI.Button(lBigShakeRect, new GUIContent("Big Shake", "")))
            {
                mAdventureRig.Shake(0.1f, 1f, 0.5f, 2f);
            }
        }
    }
}
