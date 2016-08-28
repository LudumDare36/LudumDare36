using UnityEngine;
using UnityEditor;
using System.Collections;


[CustomEditor(typeof(GroundBuilder))]
public class GroundBuilderEditor : Editor{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GroundBuilder myScript = (GroundBuilder)target;

        if( GUILayout.Button( "Preview/Generate Monolith Crown" ) )
        {
            myScript.BuildMonolithCrown();
        }
    }

}
