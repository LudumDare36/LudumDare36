using UnityEngine;
using System.Collections;

namespace MapMagic
{
	[System.Serializable]
	public struct Fallof
	{
		public AnimationCurve curve;
		public float max;
		public float noiseSize;
		public float noiseAmount;
		public float additionalWidth;
		public float splatPower;
		public bool guiShow;

		public Fallof (float max)
		{
			curve = new AnimationCurve();
			curve.AddKey(new Keyframe(0,0));
			curve.AddKey(new Keyframe(1,max));
			this.max = max; noiseSize = 100f; noiseAmount = 1f; additionalWidth = 0f; splatPower = 3f; guiShow=false;
		}

		public float Percent (float dist, int x, int z) 
		{
			//float radius = fallof.keys[fallof.keys.Length-1].time;
			//if (dist>max) return 0;

			//creating percent by fallof
			float percent = curve.Evaluate(dist-additionalWidth);
			percent = 1f - Mathf.Clamp01(percent);

			//adding noise
//			float noise = Noise.Fractal(x,z,noiseSize)*2f - 1;
//			float maxNoise = 1f - Mathf.Max(percent, 1f-percent);
//			percent += noise*maxNoise*noiseAmount;

			return percent;
		}
	}
}
