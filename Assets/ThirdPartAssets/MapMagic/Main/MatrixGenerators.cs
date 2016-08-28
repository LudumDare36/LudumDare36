using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using Plugins;

namespace MapMagic
{
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Test", disengageable = true, disabled = true)]
	public class TestGenerator : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }
		public int iterations = 100000;
		public float result;

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (!enabled) { output.SetObject(chunk, matrix); return; }

			//testing matrix
			for (int x=0; x<matrix.rect.size.x; x++)
				for (int z=0; z<matrix.rect.size.z; z++)
					matrix[x+matrix.rect.offset.x, z+matrix.rect.offset.z] = 0.3f*x/matrix.rect.size.x*5f;// + 0.5f*z/matrix.rect.size.z;
			
			//testing performance

			//for (int i=iterations*1000-1; i>=0; i--) 
			//	{ result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); result = InlineFn(result); }
				//{ result += 0.01f; result += 0.01f; result += 0.01f; result += 0.01f; result += 0.01f; }

			if (chunk.stop) return; //do not write object is generating is stopped
			output.SetObject(chunk, matrix);
		}

		public float InlineFn (float input)
		{
			return input + 0.01f; 
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout);
			layout.Par(5);
			
			//params
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.fieldSize = 0.55f;
			layout.Field(ref iterations, "K Iterations");
			layout.Field(ref result, "Result");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Constant", disengageable = true)]
	public class ConstantGenerator : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }
		public float level;

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (!enabled) { output.SetObject(chunk, matrix); return; }
			matrix.Fill(level);

			if (chunk.stop) return; //do not write object is generating is stopped
			output.SetObject(chunk, matrix);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); output.DrawIcon(layout);
			layout.Par(5);
			
			//params
			//output.sharedResolution.guiResolution = layout.ComplexField(output.sharedResolution.guiResolution, "Output Resolution");
			layout.Field(ref level, "Value", max:1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Noise", disengageable = true)]
	public class NoiseGenerator1 : Generator
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

			Noise noise = new Noise(size, matrix.rect.size.x, MapMagic.instance.seed + seed*7, MapMagic.instance.seed + seed*3);
			Coord min = matrix.rect.Min; Coord max = matrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float result = noise.Fractal(x+(int)(offset.x), z+(int)(offset.y), detail);
									
				//apply contrast and bias
				result = result*intensity;
				result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

				if (result < 0) result = 0; 
				if (result > 1) result = 1;

				if (mask==null) matrix[x,z] += result;
				else matrix[x,z] += result*mask[x,z];
			}

			//Noise(matrix, size, intensity, bias, detail, offset, seed, mask);
			
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
	[GeneratorMenu (menu="Map", name ="Voronoi", disengageable = true)]
	public class VoronoiGenerator1 : Generator
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
			float finalIntensity = intensity * cellCount / matrix.rect.size.x * 26; //backward compatibility factor

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
				if (mask==null) matrix[x,z] += val*finalIntensity;
				else matrix[x,z] += val*finalIntensity*mask[x,z];
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

	/*[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Texture Input", disengageable = true)]
	public class TextureInput : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public MatrixAsset textureAsset;
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
			if (textureAsset == null) textureAsset = ScriptableObject.CreateInstance<MatrixAsset>();
			if (textureAsset.matrix == null) textureAsset.matrix = new Matrix( new CoordRect(0,0,1,1) );

			textureAsset.matrix.ImportRaw(path);
			texturePath = path;

			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 70, 70);
			previewMatrix = textureAsset.matrix.Resize(previewRect, previewMatrix);
			preview = previewMatrix.SimpleToTexture();
			#endif
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (!enabled || textureAsset==null || textureAsset.matrix==null) { output.SetObject(chunk, matrix); return; }
			if (chunk.stop) return;

			//matrix = textureMatrix.Resize(matrix.rect);
			
			CoordRect scaledRect = new CoordRect(
				(int)(offset.x), 
				(int)(offset.y), 
				(int)(matrix.rect.size.x*scale),
				(int)(matrix.rect.size.z*scale) );
			Matrix scaledTexture = textureAsset.matrix.Resize(scaledRect);

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
			
			if (chunk.stop) return;
			output.SetObject(chunk, matrix);
		}

		public override void OnGUI (Layout layout)
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
	}*/

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Simple Form", disengageable = true)]
	public class SimpleForm : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum FormType { GradientX, GradientZ, Pyramid, Cone }
		public FormType type = FormType.Cone;
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public bool tile = false;


		public override void Generate (MapMagic.Chunk chunk)
		{
			if (!enabled || chunk.stop) return;

			CoordRect scaledRect = new CoordRect(
				(int)(offset.x * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(offset.y * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(MapMagic.instance.resolution*scale),
				(int)(MapMagic.instance.resolution*scale) );
			Matrix stampMatrix = new Matrix(scaledRect);

			float gradientStep = 1f/stampMatrix.rect.size.x;
			Coord center = scaledRect.Center;
			float radius = stampMatrix.rect.size.x / 2f;
			Coord min = stampMatrix.rect.Min; Coord max = stampMatrix.rect.Max;
			
			switch (type)
			{
				case FormType.GradientX:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
							stampMatrix[x,z] = x*gradientStep;
					break;
				case FormType.GradientZ:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
							stampMatrix[x,z] = z*gradientStep;
					break;
				case FormType.Pyramid:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							float valX = x*gradientStep; if (valX > 1-valX) valX = 1-valX;
							float valZ = z*gradientStep; if (valZ > 1-valZ) valZ = 1-valZ;
							stampMatrix[x,z] = valX<valZ? valX*2 : valZ*2;
						}
					break;
				case FormType.Cone:
					for (int x=min.x; x<max.x; x++)
						for (int z=min.z; z<max.z; z++)
						{
							float val = 1 - (Coord.Distance(new Coord(x,z), center) / radius);
							if (val<0) val = 0;
							stampMatrix[x,z] = val;
						}
					break;
			}

			Matrix matrix = chunk.defaultMatrix;
			matrix.Replicate(stampMatrix, tile:tile);
			matrix.Multiply(intensity);

			
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
			
			layout.fieldSize = 0.62f;
			layout.Field(ref type, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			layout.Toggle(ref tile, "Tile");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Raw Input", disengageable = true)]
	public class RawInput : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public MatrixAsset textureAsset;
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
			if (textureAsset == null) textureAsset = ScriptableObject.CreateInstance<MatrixAsset>();
			if (textureAsset.matrix == null) textureAsset.matrix = new Matrix( new CoordRect(0,0,1,1) );

			textureAsset.matrix.ImportRaw(path);
			texturePath = path;

			//generating preview
			CoordRect previewRect = new CoordRect(0,0, 70, 70);
			previewMatrix = textureAsset.matrix.Resize(previewRect, previewMatrix);
			preview = previewMatrix.SimpleToTexture();
			#endif
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (!enabled || textureAsset==null || textureAsset.matrix==null) { output.SetObject(chunk, matrix); return; }
			if (chunk.stop) return;

			//matrix = textureMatrix.Resize(matrix.rect);
			
			CoordRect scaledRect = new CoordRect(
				(int)(offset.x), 
				(int)(offset.y), 
				(int)(matrix.rect.size.x*scale),
				(int)(matrix.rect.size.z*scale) );
			Matrix scaledTexture = textureAsset.matrix.Resize(scaledRect);

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
			layout.Field(ref offset, "Offset", handles:true);
			layout.Toggle(ref tile, "Tile");
		}
	}




	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Texture Input", disengageable = true)]
	public class TextureInput : Generator
	{
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return output; }

		public Texture2D texture;
		[System.NonSerialized] public Matrix textureMatrix;
		[System.NonSerialized] public object matrixLocker = new object();
		public float intensity = 1;
		public float scale = 1;
		public Vector2 offset;
		public bool tile = true;

		public void CheckLoadTexture ()
		{
			lock (matrixLocker)
			{
				if (texture == null) return;
				textureMatrix = new Matrix( new CoordRect(0,0, texture.width, texture.height) );
				try { textureMatrix.FromTexture(texture); }
				catch (UnityException e) { Debug.LogError(e); }
			}
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix matrix = chunk.defaultMatrix;
			if (chunk.stop || !enabled || texture==null) return;

			CoordRect scaledRect = new CoordRect(
				(int)(offset.x * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(offset.y * MapMagic.instance.resolution / MapMagic.instance.terrainSize), 
				(int)(matrix.rect.size.x*scale),
				(int)(matrix.rect.size.z*scale) );
			Matrix scaledTexture = null;
			lock (matrixLocker) { scaledTexture = textureMatrix.Resize(scaledRect); }

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
			layout.fieldSize = 0.62f;
			layout.Field(ref texture, "Texture");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref scale, "Scale");
			layout.Field(ref offset, "Offset");
			layout.Toggle(ref tile, "Tile");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Intensity/Bias", disengageable = true)]
	public class IntensityBiasGenerator : Generator
	{
		public float intensity = 1f;
		public float bias = 0.0f;

		public Input input = new Input("Input", InoutType.Map, write:false);//, mandatory:true);
		public Input maskIn = new Input("Mask", InoutType.Map);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || src==null) return;
			if (!enabled) { output.SetObject(chunk, src); return; }

			//preparing output
			Matrix dst = src.Copy(null);

			for (int i=0; i<dst.count; i++)
			{
				float result = dst.array[i];
				
				//apply contrast and bias
				result = result*intensity;
				result -= 0*(1-bias) + (intensity-1)*bias; //0.5f - intensity*bias;

				if (result < 0) result = 0; 
				if (result > 1) result = 1;

				dst.array[i] = result;
			}
			
			//mask and safe borders
			if (chunk.stop) return;
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, dst);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);
			layout.Par(5);

			//params
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref bias, "Bias");
		}
	}

	
	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Invert", disengageable = true)]
	public class InvertGenerator : Generator
	{
		//yap, this one is from the tutorial

		public Input input = new Input("Input", InoutType.Map, mandatory:true);
		public Output output = new Output("Output", InoutType.Map);
		public Input maskIn = new Input("Mask", InoutType.Map);

		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public float level = 1;

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix src = (Matrix)input.GetObject(chunk);

			if (src==null || chunk.stop) return;
			if (!enabled) { output.SetObject(chunk, src); return; }

			Matrix dst = new Matrix(src.rect);

			Coord min = src.rect.Min; Coord max = src.rect.Max;
			for (int x=min.x; x<max.x; x++)
			   for (int z=min.z; z<max.z; z++)
			   {
					float val = level - src[x,z];
					dst[x,z] = val>0? val : 0;
				}

			//mask and safe borders
			if (chunk.stop) return;
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) Matrix.Mask(src, dst, mask);

			if (chunk.stop) return;
			output.SetObject(chunk, dst);
		}

		public override void OnGUI ()
		{
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);

			layout.Field(ref level, "Level", min:0, max:1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Curve", disengageable = true)]
	public class CurveGenerator : Generator
	{
		//public override Type guiType { get { return Generator.Type.curve; } }
		
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		public bool extended = true;
		//public float inputMax = 1;
		//public float outputMax = 1;
		public Vector2 range = new Vector2(0,1);

		public Input input = new Input("Input", InoutType.Map, write:false);//, mandatory:true);
		public Input maskIn = new Input("Mask", InoutType.Map);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || src==null) return;
			if (!enabled) { output.SetObject(chunk, src); return; }

			//preparing output
			Matrix dst = src.Copy(null);

			//curve
			Curve c = new Curve(curve);
			for (int i=0; i<dst.array.Length; i++) dst.array[i] = c.Evaluate(dst.array[i]);
			
			//mask and safe borders
			if (chunk.stop) return;
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, dst);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);
			layout.Par(5);

			//params
			Rect savedCursor = layout.cursor;
			layout.Par(50, padding:0);
			layout.Inset(3);
			layout.Curve(curve, rect:layout.Inset(80, padding:0), min:range.x, max:range.y);
			layout.Par(3);

			layout.margin = 86;
			layout.cursor = savedCursor;
			layout.Label("Range:");
			//layout.Par(); layout.Label("Min:", rect:layout.Inset(0.999f)); layout.Label("Max:", rect:layout.Inset(1f));
			layout.Field(ref range);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Blend", disengageable = true)]
	public class BlendGenerator : Generator
	{
		public Input baseInput = new Input("Base", InoutType.Map, write:true, mandatory:true);
		public Input blendInput = new Input("Blend", InoutType.Map, write:false, mandatory:true);
		public Input maskInput = new Input("Mask", InoutType.Map, write:false);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return baseInput; yield return blendInput; yield return maskInput; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum Algorithm {mix=0, add=1, subtract=2, multiply=3, divide=4, difference=5, min=6, max=7, overlay=8, hardLight=9, softLight=10} 
		public Algorithm algorithm = Algorithm.mix;
		public float opacity = 1;

		//outdated
		public enum GuiAlgorithm {mix, add, subtract, multiply, divide, difference, min, max, overlay, hardLight, softLight, none} 
		public GuiAlgorithm guiAlgorithm = GuiAlgorithm.mix;

		public static System.Func<float,float,float> GetAlgorithm (Algorithm algorithm)
		{
			switch (algorithm)
			{
				case Algorithm.mix: return delegate (float a, float b) { return b; };
				case Algorithm.add: return delegate (float a, float b) { return a+b; };
				case Algorithm.subtract: return delegate (float a, float b) { return a-b; };
				case Algorithm.multiply: return delegate (float a, float b) { return a*b; };
				case Algorithm.divide: return delegate (float a, float b) { return a/b; };
				case Algorithm.difference: return delegate (float a, float b) { return Mathf.Abs(a-b); };
				case Algorithm.min: return delegate (float a, float b) { return Mathf.Min(a,b); };
				case Algorithm.max: return delegate (float a, float b) { return Mathf.Max(a,b); };
				case Algorithm.overlay: return delegate (float a, float b) 
				{
					if (a > 0.5f) return 1 - 2*(1-a)*(1-b);
					else return 2*a*b; 
				}; 
				case Algorithm.hardLight: return delegate (float a, float b) 
				{
						if (b > 0.5f) return 1 - 2*(1-a)*(1-b);
						else return 2*a*b; 
				};
				case Algorithm.softLight: return delegate (float a, float b) { return (1-2*b)*a*a + 2*b*a; };
				default: return delegate (float a, float b) { return b; };
			}
		}


		public override void Generate (MapMagic.Chunk chunk)
		{
			//preparing inputs
			Matrix baseMatrix = (Matrix)baseInput.GetObject(chunk);
			Matrix blendMatrix = (Matrix)blendInput.GetObject(chunk);
			Matrix maskMatrix = (Matrix)maskInput.GetObject(chunk);
			
			//return on stop/disable/null input
			if (chunk.stop || baseMatrix==null) return;
			if (!enabled || blendMatrix==null) { output.SetObject(chunk, baseMatrix); return; }
			
			//preparing output
			baseMatrix = baseMatrix.Copy(null);

			//setting algorithm
			if (guiAlgorithm != GuiAlgorithm.none) { algorithm = (Algorithm)guiAlgorithm; guiAlgorithm = GuiAlgorithm.none; }
			System.Func<float,float,float> algorithmFn = GetAlgorithm(algorithm);


			//special fast cases for mix and add
			/*if (maskMatrix == null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a*(1-opacity) + b*opacity;
				}
			else if (maskMatrix != null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = maskMatrix.array[i] * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a*(1-m) + b*m;
				}
			else if (maskMatrix == null && guiAlgorithm == GuiAlgorithm.add)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a + b*opacity;
				}
			else if (maskMatrix != null && guiAlgorithm == GuiAlgorithm.mix)
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = maskMatrix.array[i] * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];
					baseMatrix.array[i] = a + b*m;
				}*/

			//generating all other
			//else
				for (int i=0; i<baseMatrix.array.Length; i++)
				{
					float m = (maskMatrix==null ? 1 : maskMatrix.array[i]) * opacity;
					float a = baseMatrix.array[i];
					float b = blendMatrix.array[i];

					baseMatrix.array[i] = a*(1-m) + algorithmFn(a,b)*m;
				}
		
			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, baseMatrix);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); baseInput.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); blendInput.DrawIcon(layout);
			layout.Par(20); maskInput.DrawIcon(layout);
			layout.Par(5);
			
			//params
			if (guiAlgorithm != GuiAlgorithm.none) { algorithm = (Algorithm)guiAlgorithm; guiAlgorithm = GuiAlgorithm.none; }
			layout.Field(ref algorithm, "Algorithm");
			layout.Field(ref opacity, "Opacity");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Blur", disengageable = true)]
	public class BlurGenerator : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Input maskIn = new Input("Mask", InoutType.Map, write:false, mandatory:false);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int iterations = 1;
		public float intensity = 1f;
		public int loss = 1;
		public int safeBorders = 5;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix src = (Matrix)input.GetObject(chunk); 

			//return on stop/disable/null input
			if (chunk.stop || src==null) return; 
			if (!enabled) { output.SetObject(chunk, src); return; }
			
			//preparing output
			Matrix dst = src.Copy(null);

			//blurring beforehead if loss is on
			if (loss!=1) for (int i=0; i<iterations;i++) dst.Blur(intensity:0.666f);

			//blur with loss
			int curLoss = loss;
			while (curLoss>1)
			{
				dst.LossBlur(curLoss);
				curLoss /= 2;
			}
			
			//main blur (after loss)
			for (int i=0; i<iterations;i++) dst.Blur(intensity:1f);

			//mask and safe borders
			if (intensity < 0.9999f) Matrix.Blend(src, dst, intensity);
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
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout); 
			layout.Par(5);
			
			//params
			layout.Field(ref intensity, "Intensity", max:1);
			layout.Field(ref iterations, "Iterations", min:1);
			layout.Field(ref loss, "Loss", min:1);
			layout.Field(ref safeBorders, "Safe Borders");
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Cavity", disengageable = true)]
	public class CavityGenerator1 : Generator
	{
		
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public enum CavityType { Convex, Concave }
		public CavityType type = CavityType.Convex;
		public float intensity = 1;
		public float spread = 0.5f;
		public bool normalize = true;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || !enabled || matrix==null) return; 

			//preparing outputs
			Matrix result = new Matrix(matrix.rect);

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

			//inverting
			if (type == CavityType.Concave) result.Invert();
			if (chunk.stop) return;

			//normalizing
			if (!normalize) result.Clamp01();
			if (chunk.stop) return;

			//spread
			result.Spread(strength:spread); 
			if (chunk.stop) return;

			//clamping and inverting
			/*for (int i=0; i<result.count; i++) 
			{
				temp.array[i] = 0;
				if (result.array[i]<0) { temp.array[i] = -result.array[i]; result.array[i] = 0; }
			}*/

			//setting outputs
			output.SetObject(chunk, result);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			//layout.Par(20); concaveOut.DrawIcon(layout);
			layout.Par(5);
			
			//params
			layout.Field(ref type, "Type");
			layout.Field(ref intensity, "Intensity");
			layout.Field(ref spread, "Spread");
			layout.Par(3);
			layout.Toggle(ref normalize, "Normalize");
			layout.Par(15); layout.Inset(20); layout.Label(label:"Convex + Concave", rect:layout.Inset(), textAnchor:TextAnchor.LowerLeft);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Slope", disengageable = true)]
	public class SlopeGenerator1 : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		public Vector2 steepness = new Vector2(45,90);
		public float range = 5f;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			Matrix matrix = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || !enabled || matrix==null) return; 

			//preparing output
			Matrix result = new Matrix(matrix.rect);

			//using the terain-height relative values
			float pixelSize = 1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			
			float min0 = Mathf.Tan((steepness.x-range/2)*Mathf.Deg2Rad) * pixelSize / MapMagic.instance.terrainHeight;
			float min1 = Mathf.Tan((steepness.x+range/2)*Mathf.Deg2Rad) * pixelSize / MapMagic.instance.terrainHeight;
			float max0 = Mathf.Tan((steepness.y-range/2)*Mathf.Deg2Rad) * pixelSize / MapMagic.instance.terrainHeight;
			float max1 = Mathf.Tan((steepness.y+range/2)*Mathf.Deg2Rad) * pixelSize / MapMagic.instance.terrainHeight;

			//dealing with 90-degree
			if (steepness.y-range/2 > 89.9f) max0 = 20000000; if (steepness.y+range/2 > 89.9f) max1 = 20000000;

			//ignoring min if it is zero
			if (steepness.x<0.0001f) { min0=0; min1=0; }

			//delta map
			System.Func<float,float,float,float> inclineFn = delegate(float prev, float curr, float next) 
			{
				float prevDelta = prev-curr; if (prevDelta < 0) prevDelta = -prevDelta;
				float nextDelta = next-curr; if (nextDelta < 0) nextDelta = -nextDelta;
				return prevDelta>nextDelta? prevDelta : nextDelta; 
			};
			result.Blur(inclineFn, intensity:1, takemax:true, reference:matrix); //intensity is set in func

			//slope map
			for (int i=0; i<result.array.Length; i++)
			{
				float delta = result.array[i];
				
				if (steepness.x<0.0001f) result.array[i] = 1-(delta-max0)/(max1-max0);
				else
				{
					float minVal = (delta-min0)/(min1-min0);
					float maxVal = 1-(delta-max0)/(max1-max0);
					float val = minVal>maxVal? maxVal : minVal;
					if (val<0) val=0; if (val>1) val=1;

					result.array[i] = val;
				}
			}

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
			layout.fieldSize = 0.6f;
			layout.Field(ref steepness, "Steepness", min:0, max:90);
			layout.Field(ref range, "Range", min:0.1f);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Terrace", disengageable = true)]
	public class TerraceGenerator : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Input maskIn = new Input("Mask", InoutType.Map);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return output; }

		public int seed = 12345;
		public int num = 10;
		public float uniformity = 0.5f;
		public float steepness = 0.5f;
		public float intensity = 1f;

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting inputs
			Matrix src = (Matrix)input.GetObject(chunk);

			//return on stop/disable/null input
			if (chunk.stop || src==null) return; 
			if (!enabled || num <= 1) { output.SetObject(chunk, src); return; }
			
			//preparing output
			Matrix dst = src.Copy(null);

			//creating terraces
			float[] terraces = new float[num];
			InstanceRandom random = new InstanceRandom(MapMagic.instance.seed + 12345);
			
			float step = 1f / (num-1);
			for (int t=1; t<num; t++)
				terraces[t] = terraces[t-1] + step;

			for (int i=0; i<10; i++)
				for (int t=1; t<num-1; t++)
				{
					float rndVal = random.Random(terraces[t-1], terraces[t+1]);
					terraces[t] = terraces[t]*uniformity + rndVal*(1-uniformity);
				}

			//adjusting matrix
			if (chunk.stop) return;
			for (int i=0; i<dst.count; i++)
			{
				float val = dst.array[i];
				if (val > 0.999f) continue;	//do nothing with values that are out of range

				int terrNum = 0;		
				for (int t=0; t<num-1; t++)
				{
					if (terraces[terrNum+1] > val || terrNum+1 == num) break;
					terrNum++;
				}

				//kinda curve evaluation
				float delta = terraces[terrNum+1] - terraces[terrNum];
				float relativePos = (val - terraces[terrNum]) / delta;

				float percent = 3*relativePos*relativePos - 2*relativePos*relativePos*relativePos;

				percent = (percent-0.5f)*2;
				bool minus = percent<0; percent = Mathf.Abs(percent);

				percent = Mathf.Pow(percent,1f-steepness);

				if (minus) percent = -percent;
				percent = percent/2 + 0.5f;

				dst.array[i] = (terraces[terrNum]*(1-percent) + terraces[terrNum+1]*percent)*intensity + dst.array[i]*(1-intensity);
				//matrix.array[i] = a*keyVals[keyNum] + b*keyOutTangents[keyNum]*delta + c*keyInTangents[keyNum+1]*delta + d*keyVals[keyNum+1];
			}

			//mask and safe borders
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) Matrix.Mask(src, dst, mask);

			//setting output
			if (chunk.stop) return;
			output.SetObject(chunk, dst);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); input.DrawIcon(layout); output.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout);
			layout.Par(5);
			
			//params
			layout.Field(ref num, "Treads Num", min:2);
			layout.Field(ref uniformity, "Uniformity", min:0, max:1);
			layout.Field(ref steepness, "Steepness", min:0, max:1);
			layout.Field(ref intensity, "Intensity", min:0, max:1);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Erosion", disengageable = true)]
	public class ErosionGenerator : Generator
	{
		public Input heightIn = new Input("Heights", InoutType.Map, write:false, mandatory:true);
		public Input maskIn = new Input("Mask", InoutType.Map, write:false, mandatory:false);
		public Output heightOut = new Output("Heights", InoutType.Map);
		public Output cliffOut = new Output("Cliff", InoutType.Map);
		public Output sedimentOut = new Output("Sediment", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return heightIn; yield return maskIn; }
		public override IEnumerable<Output> Outputs() { yield return heightOut; yield return cliffOut; yield return sedimentOut; }

		public int iterations = 5;
		public float terrainDurability=0.9f;
		public float erosionAmount=1f;
		public float sedimentAmount=0.75f;
		public int fluidityIterations=3;
		public float ruffle=0.4f;
		public int safeBorders = 10;
		public float cliffOpacity = 1f;
		public float sedimentOpacity = 1f;


		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting inputs
			Matrix src = (Matrix)heightIn.GetObject(chunk);
			
			//return
			if (chunk.stop || src==null) return; 
			if (!enabled || iterations <= 0) { heightOut.SetObject(chunk, src); return; }

			//creating output arrays
			Matrix dst = new Matrix(src.rect);
			Matrix dstErosion = new Matrix(src.rect);
			Matrix dstSediment = new Matrix(src.rect);

			//crating temporary arrays (with margins)
			int margins = 10;
			Matrix height = new Matrix(src.rect.offset-margins, src.rect.size+margins*2);
			height.Fill(src, removeBorders:true);

			Matrix erosion = new Matrix(height.rect);
			Matrix sediment = new Matrix(height.rect);
			Matrix internalTorrents = new Matrix(height.rect);
			int[] stepsArray = new int[1000001];
			int[] heightsInt = new int[height.count];
			int[] order = new int[height.count];

			//calculate erosion
			for (int i=0; i<iterations; i++) 
			{
				if (chunk.stop) return;

				Erosion.ErosionIteration (height, erosion, sediment, area:height.rect,
							erosionDurability:terrainDurability, erosionAmount:erosionAmount, sedimentAmount:sedimentAmount, erosionFluidityIterations:fluidityIterations, ruffle:ruffle, 
							torrents:internalTorrents, stepsArray:stepsArray, heightsInt:heightsInt, order:order);

				Coord min = dst.rect.Min; Coord max = dst.rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						{ dstErosion[x,z] += erosion[x,z]*cliffOpacity*30f; dstSediment[x,z] += sediment[x,z]*sedimentOpacity; }
			}

			//fill dst
			dst.Fill(height);
			
			//expanding sediment map 1 pixel
			//dstSediment.Spread(strength:1, iterations:1);

			//mask and safe borders
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null) { Matrix.Mask(src, dst, mask); Matrix.Mask(null, dstErosion, mask); Matrix.Mask(null, dstSediment, mask); }
			if (safeBorders != 0) { Matrix.SafeBorders(src, dst, safeBorders); Matrix.SafeBorders(null, dstErosion, safeBorders); Matrix.SafeBorders(null, dstSediment, safeBorders); }
			
			//finally
			if (chunk.stop) return;
			heightOut.SetObject(chunk, dst);
			cliffOut.SetObject(chunk, dstErosion);
			sedimentOut.SetObject(chunk, dstSediment);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); heightIn.DrawIcon(layout); heightOut.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout); cliffOut.DrawIcon(layout);
			layout.Par(20); sedimentOut.DrawIcon(layout);
			layout.Par(5);
			
			//params
			//layout.SmartField(ref downscale, "Downscale", min:1); //downscale = Mathf.NextPowerOfTwo(downscale);
			//layout.ComplexField(ref preserveDetail, "Preserve Detail");
			layout.Par(30);
			layout.Label("Generating erosion takes significant amount of time", rect:layout.Inset(), helpbox:true, fontSize:9);
			layout.Par(2);
			layout.Field(ref iterations, "Iterations");
			layout.Field(ref terrainDurability, "Durability");
			layout.Field(ref erosionAmount, "Erosion", min:0, max:1);
			layout.Field(ref sedimentAmount, "Sediment");
			layout.Field(ref fluidityIterations, "Fluidity");
			layout.Field(ref ruffle, "Ruffle");
			layout.Field(ref safeBorders, "Safe Borders");
			layout.Field(ref cliffOpacity, "Cliff Opacity");
			layout.Field(ref sedimentOpacity, "Sediment Opacity");
		}
	}


	[System.Serializable]
	//[GeneratorMenu (menu="Map", name ="Noise Mask", disengageable = true)]
	public class NoiseMaskGenerator : Generator
	{
		public Input inputIn = new Input("Input", InoutType.Map, write:false);
		public override IEnumerable<Input> Inputs() { yield return inputIn; }

		public Output maskedOut = new Output("Masked", InoutType.Map);
		public Output invMaskedOut = new Output("Inv Masked", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return maskedOut; yield return invMaskedOut; }

		public float opacity = 1f;
		public float size = 200;
		public Vector2 offset = new Vector2(0,0);
		public AnimationCurve curve = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting inputs
			Matrix input = (Matrix)inputIn.GetObject(chunk);
			Matrix masked = chunk.defaultMatrix;
			Matrix invMasked = chunk.defaultMatrix;

			//return
			if (chunk.stop || input==null) return; 
			if (!enabled) { maskedOut.SetObject(chunk, input); return; }
			
			//generating noise
			NoiseGenerator.Noise(masked, size, 1, 0.5f, offset:offset);
			if (chunk.stop) return;
			
			//adjusting curve
			Curve c = new Curve(curve);
			for (int i=0; i<masked.array.Length; i++) masked.array[i] = c.Evaluate(masked.array[i]);
			if (chunk.stop) return;

			//get inverse mask
			for (int i=0; i<masked.array.Length; i++)
				invMasked.array[i] = 1f - masked.array[i];
			if (chunk.stop) return;
			
			//multiply masks by input
			if (input != null)
			{
				for (int i=0; i<masked.array.Length; i++) masked.array[i] = input.array[i]*masked.array[i]*opacity + input.array[i]*(1f-opacity);
				for (int i=0; i<invMasked.array.Length; i++) invMasked.array[i] = input.array[i]*invMasked.array[i]*opacity + input.array[i]*(1f-opacity);
			}

			if (chunk.stop) return;
			maskedOut.SetObject(chunk, masked);
			invMaskedOut.SetObject(chunk, invMasked);
		}

		public override void OnGUI ()
		{
			//inouts
			layout.Par(20); inputIn.DrawIcon(layout); maskedOut.DrawIcon(layout);
			layout.Par(20); invMaskedOut.DrawIcon(layout);
			layout.Par(5);
			
			//params
			Rect cursor = layout.cursor; layout.rightMargin = 90; layout.fieldSize = 0.75f;
			layout.Field(ref opacity, "A", max:1);
			layout.Field(ref size, "S", min:1);
			layout.Field(ref offset, "O");
			
			layout.cursor = cursor; layout.rightMargin = layout.margin; layout.margin = (int)layout.field.width - 85 - layout.margin*2;
			layout.Par(53);
			layout.Curve(curve, layout.Inset());
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Map", name ="Shore", disengageable = true)]
	public class ShoreGenerator : Generator
	{
		public Input heightIn = new Input("Input", InoutType.Map, write:false);
		public Input maskIn = new Input("Mask", InoutType.Map, write:false);
		public Input ridgeNoiseIn = new Input("Ridge Noise", InoutType.Map, write:false);
		public override IEnumerable<Input> Inputs() { yield return heightIn; yield return maskIn; yield return ridgeNoiseIn; }

		public Output heightOut = new Output("Output", InoutType.Map);
		public Output sandOut = new Output("Sand", InoutType.Map);
		public override IEnumerable<Output> Outputs() { yield return heightOut; yield return sandOut; }

		public float intensity = 1f;
		public float beachLevel = 20f;
		public float beachSize = 10f;
		public float ridgeMinGlobal = 2;
		public float ridgeMaxGlobal = 10;

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix src = (Matrix)heightIn.GetObject(chunk);

			if (src==null || chunk.stop) return;
			if (!enabled) { heightOut.SetObject(chunk, src); return; }

			Matrix dst = new Matrix(src.rect);
			Matrix ridgeNoise = (Matrix)ridgeNoiseIn.GetObject(chunk);

			//preparing sand
			Matrix sands = new Matrix(src.rect);

			//converting ui values to internal
			float beachMin = beachLevel / MapMagic.instance.terrainHeight;
			float beachMax = (beachLevel+beachSize) / MapMagic.instance.terrainHeight;
			float ridgeMin = ridgeMinGlobal / MapMagic.instance.terrainHeight;
			float ridgeMax = ridgeMaxGlobal / MapMagic.instance.terrainHeight;

			Coord min = src.rect.Min; Coord max = src.rect.Max;
			for (int x=min.x; x<max.x; x++)
			   for (int z=min.z; z<max.z; z++)
			{
				float srcHeight = src[x,z];

				//creating beach
				float height = srcHeight;
				if (srcHeight > beachMin && srcHeight < beachMax) height = beachMin;
				
				float sand = 0;
				if (srcHeight <= beachMax) sand = 1;

				//blurring ridge
				float curRidgeDist = 0;
				float noise = 0; if (ridgeNoise != null) noise = ridgeNoise[x,z];
				curRidgeDist = ridgeMin*(1-noise) + ridgeMax*noise;
				
				if (srcHeight >= beachMax && srcHeight <= beachMax+curRidgeDist) 
				{
					float percent = (srcHeight-beachMax) / curRidgeDist;
					percent = Mathf.Sqrt(percent);
					percent = 3*percent*percent - 2*percent*percent*percent;
					
					height = beachMin*(1-percent) + srcHeight*percent;
					
					sand = 1-percent;
				}

				//setting height
				height = height*intensity + srcHeight*(1-intensity);
				dst[x,z] = height;
				sands[x,z] = sand;
			}

			//mask
			Matrix mask = (Matrix)maskIn.GetObject(chunk);
			if (mask != null)  Matrix.Mask(src, dst, mask); // Matrix.Mask(null, sands, mask); }

			if (chunk.stop) return;
			heightOut.SetObject(chunk, dst); 
			sandOut.SetObject(chunk, sands);
		}

		public override void OnGUI ()
		{
			layout.Par(20); heightIn.DrawIcon(layout); heightOut.DrawIcon(layout);
			layout.Par(20); maskIn.DrawIcon(layout); sandOut.DrawIcon(layout);
			layout.Par(20); ridgeNoiseIn.DrawIcon(layout); 

			layout.Field(ref intensity, "Intensity", min:0);
			layout.Field(ref beachLevel, "Water Level", min:0);
			layout.Field(ref beachSize, "Beach Size", min:0.0001f);
			layout.Field(ref ridgeMinGlobal, "Ridge Step Min", min:0);
			layout.Field(ref ridgeMaxGlobal, "Ridge Step Max", min:0);
		}
	}

}
