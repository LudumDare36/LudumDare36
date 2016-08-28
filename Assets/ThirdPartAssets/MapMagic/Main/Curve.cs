using UnityEngine;
using System.Collections;

namespace MapMagic
{
	public class Curve 
	{
		float[] keyTimes = new float[0]; 
		float[] keyVals = new float[0]; 
		float[] keyInTangents = new float[0]; 
		float[] keyOutTangents = new float[0];

		public Curve (AnimationCurve animCurve)
		{
			int keyCount = animCurve.keys.Length;

			keyTimes = new float[keyCount]; keyVals = new float[keyCount]; keyInTangents = new float[keyCount]; keyOutTangents = new float[keyCount];

			for (int k=0; k<keyCount; k++) 
			{
				keyTimes[k] = animCurve.keys[k].time;
				keyVals[k] = animCurve.keys[k].value;
				keyInTangents[k] = animCurve.keys[k].inTangent;
				keyOutTangents[k] = animCurve.keys[k].outTangent;
			}
		}

		public float Evaluate (float time)
		{
			int keyCount = keyTimes.Length;
			if (time <= keyTimes[0]) return keyVals[0];
			if (time >= keyTimes[keyCount-1]) return keyVals[keyCount-1];

			int keyNum = 0;
			for (int k=0; k<keyCount-1; k++)
			{
				if (keyTimes[keyNum+1] > time) break;
					keyNum++;
			}
			
			float delta = keyTimes[keyNum+1] - keyTimes[keyNum];
			float relativeTime = (time - keyTimes[keyNum]) / delta;

			float timeSq = relativeTime * relativeTime;
			float timeCu = timeSq * relativeTime;
     
			float a = 2*timeCu - 3*timeSq + 1;
			float b = timeCu - 2*timeSq + relativeTime;
			float c = timeCu - timeSq;
			float d = -2*timeCu + 3*timeSq;

			return a*keyVals[keyNum] + b*keyOutTangents[keyNum]*delta + c*keyInTangents[keyNum+1]*delta + d*keyVals[keyNum+1];
		}
	}
}