using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

 
public class CustomMaterialInspector : MaterialEditor
{
	//enum Ch {_2CH, _4CH, _8CH};
	bool[] openedChannels = new bool[8];
	
	public override void OnInspectorGUI ()
	{
		if (!isVisible) { base.OnInspectorGUI (); return; }

		Material targetMat = target as Material;
		bool isTerrain = targetMat.shader.name.Contains("Standard");
		
		bool temp = targetMat.IsKeywordEnabled("_NORMALMAP");
		temp = EditorGUILayout.ToggleLeft("Normal Map", temp);
		if (temp) targetMat.EnableKeyword("_NORMALMAP"); else targetMat.DisableKeyword("_NORMALMAP"); 

		temp = targetMat.IsKeywordEnabled("_SPECGLOSSMAP");
		temp = EditorGUILayout.ToggleLeft("Specular/Metallic and Gloss Map", temp);
		if (temp) targetMat.EnableKeyword("_SPECGLOSSMAP"); else targetMat.DisableKeyword("_SPECGLOSSMAP"); 

		temp = targetMat.IsKeywordEnabled("_PARALLAXMAP");
		temp = EditorGUILayout.ToggleLeft("Parallax Map", temp);
		if (temp) targetMat.EnableKeyword("_PARALLAXMAP"); else targetMat.DisableKeyword("_PARALLAXMAP"); 

		if (!isTerrain)
		{
			temp = targetMat.IsKeywordEnabled("_WIND");
			temp = EditorGUILayout.ToggleLeft("Use Wind Animation", temp);
			if (temp) targetMat.EnableKeyword("_WIND"); else targetMat.DisableKeyword("_WIND"); 
		}

		//draw channels for terrain
		if (isTerrain)
		{
			temp = targetMat.IsKeywordEnabled("_8CH");
			temp = EditorGUILayout.ToggleLeft("8 Channels", temp);
			if (temp) targetMat.EnableKeyword("_8CH"); else targetMat.DisableKeyword("_8CH");


			//channels
			int numChannels = targetMat.IsKeywordEnabled("_8CH") ? 8 : 4;
			for (int i=0; i<numChannels; i++)
			{
				openedChannels[i] = EditorGUILayout.Foldout(openedChannels[i], "Channel " + (i+1).ToString());
				if (!openedChannels[i]) continue;

				string postfix = i==0 ? "" : (i+1).ToString();
			
				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.Space();

					EditorGUILayout.BeginVertical();
						DisplayColor(targetMat, "_Color"+postfix, "Color");
						DisplayTexture(targetMat, "_MainTex"+postfix, "Albedo (RGB)");
						DisplayTexture(targetMat, "_BumpMap"+postfix, "Normal Map");
						DisplayTexture(targetMat, "_SpecGlossMap"+postfix, "Spec Map (RGB), Smooth Map (A)");
						DisplayColor(targetMat, "_Specular"+postfix, "Spec Value (RGB), Smooth Val (A)");
						DisplayFloat(targetMat, "_Tile"+postfix, "Tile");
						EditorGUILayout.Space();
					EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}
		}
		//draw default inspector for non-terrain
		else base.OnInspectorGUI();
	}

	public void DisplayFloat (Material mat, string param, string label)
	{
		mat.SetFloat(param, EditorGUILayout.FloatField(label, mat.GetFloat(param)) );
	}

	public void DisplayColor (Material mat, string param, string label)
	{
		mat.SetColor(param, EditorGUILayout.ColorField(label, mat.GetColor(param)) );
	}

	public void DisplayTexture (Material mat, string param, string label)
	{
		//TexturePropertySingleLine(param, EditorGUILayout.FloatField(label, mat.GetFloat(param)) );
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(label);
		mat.SetTexture( param, EditorGUILayout.ObjectField(mat.GetTexture(param), typeof(Texture2D), false) as Texture2D);
		EditorGUILayout.EndHorizontal();
	}
}
