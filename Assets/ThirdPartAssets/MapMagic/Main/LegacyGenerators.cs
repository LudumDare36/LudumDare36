using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

//using Plugins;

namespace MapMagic
{
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Voronoi (Legacy)", disengageable = true, disabled = true)]
	public class VoronoiGenerator : Generator
	{
		public float intensity = 1f;
		public int cellCount = 16;
		public float uniformity = 0;
		public int seed = 12345;
		public enum BlendType { flat, closest, secondClosest, cellular, organic }
		public BlendType blendType = BlendType.cellular;
		
		public Input input = new Input("Input", InoutType.Map);
		public Input maskIn = new Input("Mask", InoutType.Map);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }


		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = (Matrix)input.GetObject(chunk); if (matrix != null) matrix = matrix.Copy(null);
			if (matrix == null) matrix = chunk.defaultMatrix;
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (chunk.stop) return;
			if (!enabled || intensity==0 || cellCount==0) { output.SetObject(chunk, matrix); return; } 

			//NoiseGenerator.Noise(matrix,200,0.5f,Vector2.zero);
			//matrix.Multiply(amount);

			InstanceRandom random = new InstanceRandom(MapMagic.instance.seed + seed);
	
			//creating point matrix
			float cellSize = 1f * matrix.rect.size.x / cellCount;
			Matrix2<Vector3> points = new Matrix2<Vector3>( new CoordRect(0,0,cellCount+2,cellCount+2) );
			points.rect.offset = new Coord(-1,-1);

			Coord matrixSpaceOffset = new Coord((int)(matrix.rect.offset.x/cellSize), (int)(matrix.rect.offset.z/cellSize));
		
			//scattering points
			for (int x=-1; x<points.rect.size.x-1; x++)
				for (int z=-1; z<points.rect.size.z-1; z++)
				{
					Vector3 randomPoint = new Vector3(x+random.CoordinateRandom(x+matrixSpaceOffset.x,z+matrixSpaceOffset.z), 0, z+random.NextCoordinateRandom());
					Vector3 centerPoint = new Vector3(x+0.5f,0,z+0.5f);
					Vector3 point = randomPoint*(1-uniformity) + centerPoint*uniformity;
					point = point*cellSize + new Vector3(matrix.rect.offset.x, 0, matrix.rect.offset.z);
					point.y = random.NextCoordinateRandom();
					points[x,z] = point;
				}

			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max; 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				//finding current cell
				Coord cell = new Coord((int)((x-matrix.rect.offset.x)/cellSize), (int)((z-matrix.rect.offset.z)/cellSize));
		
				//finding min dist
				float minDist = 200000000; float secondMinDist = 200000000;
				float minHeight = 0; //float secondMinHeight = 0;
				for (int ix=-1; ix<=1; ix++)
					for (int iz=-1; iz<=1; iz++)
				{
					Coord nearCell = new Coord(cell.x+ix, cell.z+iz);
					//if (!points.rect.CheckInRange(nearCell)) continue; //no need to perform test as points have 1-cell border around matrix

					Vector3 point = points[nearCell];
					float dist = (x-point.x)*(x-point.x) + (z-point.z)*(z-point.z);
					if (dist<minDist) 
					{ 
						secondMinDist = minDist; minDist = dist; 
						minHeight = point.y;
					}
					else if (dist<secondMinDist) secondMinDist = dist; 
				}

				float val = 0;
				switch (blendType)
				{
					case BlendType.flat: val = minHeight; break;
					case BlendType.closest: val = minDist / (MapMagic.instance.resolution*16); break;
					case BlendType.secondClosest: val = secondMinDist / (MapMagic.instance.resolution*16); break;
					case BlendType.cellular: val = (secondMinDist-minDist) / (MapMagic.instance.resolution*16); break;
					case BlendType.organic: val = (secondMinDist+minDist)/2 / (MapMagic.instance.resolution*16); break;
				}
				if (mask==null) matrix[x,z] += val*intensity;
				else matrix[x,z] += val*intensity*mask[x,z];
			}

			if (chunk.stop) return; //do not write object is generating is stopped
			output.SetObject(chunk, matrix);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.5f;
			layout.Field(ref blendType, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref cellCount, "Cell Count"); cellCount = Mathf.ClosestPowerOfTwo(cellCount);
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref seed, "Seed");
		}
	}
	
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Noise (Legacy)", disengageable = true, disabled = true)]
	public class NoiseGenerator : Generator
	{
		public int seed = 12345;
		public float intensity = 1f;
		public float bias = 0.0f;
		public float size = 200;
		public float detail = 0.5f;
		public Vector2 offset = new Vector2(0,0);
		//public float contrast = 0f;

		public Input input = new Input("Input", InoutType.Map);
		public Input maskIn = new Input("Mask", InoutType.Map);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = (Matrix)input.GetObject(chunk); if (matrix != null) matrix = matrix.Copy(null);
			if (matrix == null) matrix = chunk.defaultMatrix;
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (!enabled) { output.SetObject(chunk, matrix); return; }
			if (chunk.stop) return;

			Noise(matrix, size, intensity, bias, detail, offset, seed, mask);
			
			if (chunk.stop) return; //do not write object is generating is stopped
			output.SetObject(chunk, matrix);
		}

		public static void Noise (Matrix matrix, float size, float intensity=1, float bias=0, float detail=0.5f, Vector2 offset=new Vector2(), int seed=12345, Matrix mask=null)
		{
			int step = (int)(4096f / matrix.rect.size.x);

			int totalSeedX = ((int)offset.x + MapMagic.instance.seed + seed*7) % 77777;
			int totalSeedZ = ((int)offset.y + MapMagic.instance.seed + seed*3) % 73333;

			//get number of iterations
			int numIterations = 1; //max size iteration included
			float tempSize = size;
			for (int i=0; i<100; i++)
			{
				tempSize = tempSize/2;
				if (tempSize<1) break;
				numIterations++;
			}

			//making some noise
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
			{
				for (int z=min.z; z<max.z; z++)
				{
					float result = 0.5f;
					float curSize = size*10;
					float curAmount = 1;
				
					//applying noise
					for (int i=0; i<numIterations;i++)
					{
						float perlin = Mathf.PerlinNoise(
						(x + totalSeedX + 1000*(i+1))*step/(curSize+1), 
						(z + totalSeedZ + 100*i)*step/(curSize+1) );
						perlin = (perlin-0.5f)*curAmount + 0.5f;

						//applying overlay
						if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin);
						else result = 2*perlin*result;

						curSize *= 0.5f;
						curAmount *= detail; //detail is 0.5 by default
					}

					//apply contrast and bias
					result = result*intensity;
					result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

					if (result < 0) result = 0; 
					if (result > 1) result = 1;

					if (mask==null) matrix[x,z] += result;
					else matrix[x,z] += result*mask[x,z];
				}
			}
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout); 
			layout.Par(5);
			
			//params
			layout.fieldSize = 0.6f;
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.Field(ref seed, "Seed");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref bias, "Bias");
			layout.Field(ref size, "Size", min:1);
			layout.Field(ref detail, "Detail", max:1);
			layout.Field(ref offset, "Offset");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Raw Input (Legacy)", disengageable = true, disabled = true)]
	public class RawInput1 : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public Matrix textureMatrix;
		public Matrix previewMatrix;
		[System.NonSerialized] public Texture2D preview;  
		public string texturePath; 
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public bool tile = false;

		public void ImportRaw (string path=null)
		{
			#if UNITY_EDITOR
			//importing
			if (path==null) path = UnityEditor.EditorUtility.OpenFilePanel("Import Texture File", "", "raw,r16");
			if (path==null || path.Length==0) return;

			UnityEditor.Undo.RecordObject(MapMagic.instance.gens, "MapMagic Open RAW");
			MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;

			//if (textureAsset == null) textureAsset = ScriptableObject.CreateInstance<MatrixAsset>();
			if (textureMatrix == null) textureMatrix = new Matrix( new CoordRect(0,0,1,1) );

			textureMatrix.ImportRaw(path);
			texturePath = path;

			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 70, 70);
			previewMatrix = textureMatrix.Resize(previewRect, previewMatrix);
			preview = previewMatrix.SimpleToTexture();

			UnityEditor.EditorUtility.SetDirty(MapMagic.instance.gens);
			#endif
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (!enabled || textureMatrix==null) { output.SetObject(chunk, matrix); return; }
			if (chunk.stop) return;

			//matrix = textureMatrix.Resize(matrix.rect);
			
			CoordRect scaledRect = new CoordRect(
				(int)(offset.x * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(offset.y * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(matrix.rect.size.x*scale),
				(int)(matrix.rect.size.z*scale) );
			Matrix scaledTexture = textureMatrix.Resize(scaledRect);

			matrix.Replicate(scaledTexture, tile:tile);
			matrix.Multiply(intensity);

			if (scale > 1)
			{
				Matrix cpy = matrix.Copy();
				for (int i=0; i<scale-1; i++) matrix.Blur();
				Matrix.SafeBorders(cpy, matrix, Mathf.Max(matrix.rect.size.x/128, 4));
			}
			
			//if (tile) textureMatrix.FromTextureTiled(texture);
			//else textureMatrix.FromTexture(texture);
			
			//if (!Mathf.Approximately(scale,1)) textureMatrix = textureMatrix.Resize(matrix.rect, result:matrix);*/
			if (chunk.stop) return;
			output.SetObject(chunk, matrix);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout);
			layout.Par(5);
			
			//preview texture
			layout.margin = 4;
			#if UNITY_EDITOR
			int previewSize = 70;
			int controlsSize = (int)layout.field.width - previewSize - 10;
			Rect oldCursor = layout.cursor;
			if (preview == null) 
			{
				if (previewMatrix != null) preview = previewMatrix.SimpleToTexture();
				else preview = Extensions.ColorTexture(2,2,Color.black);
			}
			layout.Par(previewSize+3); layout.Inset(controlsSize);
			layout.Icon(preview, layout.Inset(previewSize+4));
			layout.cursor = oldCursor;
			
			//preview params
			layout.Par(); if (layout.Button("Browse", rect:layout.Inset(controlsSize))) { ImportRaw(); layout.change = true; }
			layout.Par(); if (layout.Button("Refresh", rect:layout.Inset(controlsSize))) { ImportRaw(texturePath); layout.change = true; }
			layout.Par(40); layout.Label("Square gray 16bit RAW, PC byte order", layout.Inset(controlsSize), helpbox:true, fontSize:9);
			#endif

			layout.fieldSize = 0.62f;
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			layout.Toggle(ref tile, "Tile");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Cavity (Legacy)", disengageable = true, disabled = true)]
	public class CavityGenerator : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Output convexOut = new Output("Convex", InoutType.Map);
		public Output concaveOut = new Output("Concave", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return convexOut;  yield return concaveOut; }
		public float intensity = 1;
		public float spread = 0.5f;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || !enabled || matrix==null) return; 

			//preparing outputs
			Matrix result = new Matrix(matrix.rect);
			Matrix temp = new Matrix(matrix.rect);

			//cavity
			System.Func<float,float,float,float> cavityFn = delegate(float prev, float curr, float next) 
			{
				float c = curr - (next+prev)/2;
				return (c*c*(c>0?1:-1))*intensity*100000;
			};
			result.Blur(cavityFn, intensity:1, additive:true, reference:matrix); //intensity is set in func
			if (chunk.stop) return;

			//borders
			result.RemoveBorders(); 
			if (chunk.stop) return;

			//spread
			result.Spread(strength:spread, copy:temp); 
			if (chunk.stop) return;

			//clamping and inverting
			for (int i=0; i<result.count; i++) 
			{
				temp.array[i] = 0;
				if (result.array[i]<0) { temp.array[i] = -result.array[i]; result.array[i] = 0; }
			}

			//setting outputs
			if (chunk.stop) return;
			convexOut.SetObject(chunk, result);
			concaveOut.SetObject(chunk, temp);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); convexOut.DrawIcon(layout);
			layout.Par(20); concaveOut.DrawIcon(layout);
			layout.Par(5);
			
			//params
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref spread, "Spread");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Slope (Legacy)", disengageable = true, disabled = true)]
	public class SlopeGenerator : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		public float steepness = 2.5f;
		public float range = 0.3f;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || !enabled || matrix==null) return; 

			//preparing output
			Matrix result = new Matrix(matrix.rect);

			//using the terain-height relative values
			float dist = range;
			float start = steepness-dist/4; //4, not 2 because blurring is additive

			//transforming to 0-1 range
			start = start/MapMagic.instance.terrainHeight;
			dist = dist/MapMagic.instance.terrainHeight;

			//incline
			System.Func<float,float,float,float> inclineFn = delegate(float prev, float curr, float next) 
			{
				float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
				float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
				float delta = prevDelta>nextDelta? prevDelta : nextDelta; 
				delta *= 1.8f; //for backwards compatibility
				float val = (delta-start)/dist; if (val < 0) val=0; if (val>1) val=1;

				return val;
			};
			result.Blur(inclineFn, intensity:1, additive:true, reference:matrix); //intensity is set in func
			
			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, result);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(5);
			
			//params
			layout.Field(ref steepness, "Steepness", min:0);
			layout.Field(ref range, "Range", min:0.1f);
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Objects", name ="Stamp (legacy)", disengageable = true, disabled = true)]
	public class StampGenerator : Generator
	{
		public Input objectsIn = new Input("Objects", InoutType.Objects, mandatory:true, write:false);
		public Input canvasIn = new Input("Canvas", InoutType.Map, mandatory:false, write:false);
		public Input maskIn = new Input("Mask", InoutType.Map, mandatory:false, write:false);
		public override IEnumerable<Input> Inputs() {  yield return objectsIn; yield return canvasIn; yield return maskIn; }

		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float radius = 10;
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public bool useNoise = false;
		public float noiseAmount = 0.1f;
		public float noiseSize = 100;
		public bool maxHeight = true;
		public float sizeFactor = 0;
		public int safeBorders = 0;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting inputs
			SpatialHash objects = (SpatialHash)objectsIn.GetObject(chunk);
			Matrix src = (Matrix)canvasIn.GetObject(chunk);
			
			//return on stop/disable/null input
			if (chunk.stop || objects==null) return; 
			if (!enabled) { output.SetObject(chunk, src); return; }

			//preparing output
			Matrix dst; 
			if (src != null) dst = src.Copy(null); 
			else dst = chunk.defaultMatrix;

			//finding maximum radius
			float maxRadius = radius;
			if (sizeFactor > 0.00001f)
			{
				float maxObjSize = 0;
				foreach (SpatialObject obj in objects.AllObjs())
					if (obj.size > maxObjSize) maxObjSize = obj.size;
				maxObjSize = maxObjSize / MapMagic.instance.terrainSize * MapMagic.instance.resolution; //transforming to map-space
				maxRadius = radius*(1-sizeFactor) + radius*maxObjSize*sizeFactor;
			}

			//preparing procedural matrices
			Matrix noiseMatrix = new Matrix( new CoordRect(0,0,maxRadius*2+2,maxRadius*2+2) );
			Matrix percentMatrix = new Matrix( new CoordRect(0,0,maxRadius*2+2,maxRadius*2+2) );

			foreach (SpatialObject obj in objects.AllObjs())
			{
				//finding current radius
				float curRadius = radius*(1-sizeFactor) + radius*obj.size*sizeFactor;
				curRadius = curRadius / MapMagic.instance.terrainSize * MapMagic.instance.resolution; //transforming to map-space

				//resizing procedural matrices
				CoordRect matrixSize = new CoordRect(0,0,curRadius*2+2,curRadius*2+2);
				noiseMatrix.ChangeRect(matrixSize);
				percentMatrix.ChangeRect(matrixSize);

				//apply stamp
				noiseMatrix.rect.offset = new Coord((int)(obj.pos.x-curRadius-1), (int)(obj.pos.y-curRadius-1));
				percentMatrix.rect.offset = new Coord((int)(obj.pos.x-curRadius-1), (int)(obj.pos.y-curRadius-1));

				CoordRect intersection = CoordRect.Intersect(noiseMatrix.rect, dst.rect);
				Coord min = intersection.Min; Coord max = intersection.Max; 
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					float dist = Mathf.Sqrt((x-obj.pos.x+0.5f)*(x-obj.pos.x+0.5f) + (z-obj.pos.y+0.5f)*(z-obj.pos.y+0.5f));
					float percent = 1f - dist / curRadius; 
					if (percent < 0 || dist > curRadius) percent = 0;

					percentMatrix[x,z] = percent;
				}

				//adjusting value by curve
				Curve c = new Curve(curve);
				for (int i=0; i<percentMatrix.array.Length; i++) percentMatrix.array[i] = c.Evaluate(percentMatrix.array[i]);

				//adding some noise
				if (useNoise) 
				{
					NoiseGenerator.Noise(noiseMatrix, noiseSize, 0.5f, offset:Vector2.zero);

					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
					{
						float val = percentMatrix[x,z];
						if (val < 0.0001f) continue;

						float noise = noiseMatrix[x,z];
						if (val < 0.5f) noise *= val*2;
						else noise = 1 - (1-noise)*(1-val)*2;

						percentMatrix[x,z] = noise*noiseAmount + val*(1-noiseAmount);
					}
				}

				//applying matrices
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					//float distSq = (x-obj.pos.x)*(x-obj.pos.x) + (z-obj.pos.y)*(z-obj.pos.y);
					//if (distSq > radius*radius) continue;
					
					float percent = percentMatrix[x,z];
					dst[x,z] = (maxHeight? 1:obj.height)*percent + dst[x,z]*(1-percent);
				}
			}

			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) Matrix.Mask(src, dst, mask);
			if (safeBorders != 0) Matrix.SafeBorders(src, dst, safeBorders);

			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, dst);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); objectsIn.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); canvasIn.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);
			layout.Par(5);

			//params
			layout.margin=5;
			layout.Field(ref radius, "Radius");
			layout.Label("Fallof:");
			
			//curve
			Rect cursor = layout.cursor;
			layout.Par(53);
			layout.Curve(curve, rect:layout.Inset(80));

			layout.cursor = cursor; 
			int margin = layout.margin; layout.margin = 86; layout.fieldSize = 0.8f;
			layout.Toggle(ref useNoise, "Noise");
			layout.Field(ref noiseAmount, "A");
			layout.Field(ref noiseSize, "S");
			
			layout.Par(5); layout.margin = margin; layout.fieldSize = 0.5f;
			layout.Field(ref sizeFactor, "Size Factor");
			layout.Field(ref safeBorders, "Safe Borders");
			layout.Toggle(ref maxHeight,"Use Maximum Height");
		}
	}
}
