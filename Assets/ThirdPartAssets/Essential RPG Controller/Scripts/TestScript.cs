using UnityEngine;
using System.Collections;

public class TestScript : MonoBehaviour 
{
	public CameraManager man = null;
	public EssentialControlScript ess = null;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Input.GetButtonDown("ModeChange"))
		{
			if (man.Mode == GameMode.FPS)
			{
				man.Mode = GameMode.RPG;
				ess.Mode = GameMode.RPG;
			}
			else
			{
				man.Mode = GameMode.FPS;
				ess.Mode = GameMode.FPS;
			}
		}
	}
}
