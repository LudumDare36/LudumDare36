using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using Plugins;

namespace MapMagic
{
	public interface IOutput 
	{
		IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data);
		//static void Clear (MapMagic.Chunk chunk);
	}

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Height", disengageable = true)]
	public class HeightOutput : Generator, IOutput
	{
		public Input input = new Input("Height", InoutType.Map, write:false);//, mandatory:true);
		public override IEnumerable<Input> Inputs() { yield return input; }

		//public Output result = new Output("Result", InoutType.Map); //type does not matter actually
		//public override IEnumerable<Output> Outputs() { yield return result; }
		//public Texture2D texture;
		
		public float scale = 1;
		public float layer { get; set; }

		public override void Generate (MapMagic.Chunk chunk)
		{
			Matrix heights = (Matrix)input.GetObject(chunk);
			if (chunk.stop) return;
			if (heights==null) heights = chunk.defaultMatrix; // new Matrix( new CoordRect(0,0,8,8) );

			//scale heights
			if (scale < 0.0624f) scale = 0.0625f;
			if (scale > 1.1f) heights = heights.BlurredUpscale((int)scale);
			if (scale < 0.9f) heights = heights.Resize(heights.rect*scale);

			/* sliced 
			//preparing out
			int sliceCount = 1; int sliceSize = heights.rect.size.x / sliceCount;
			float[][,] heights2D = new float[sliceCount][,];
			for (int c=0; c<sliceCount-1; c++) heights2D[c] = new float[heights.rect.size.x+1, sliceSize];
			heights2D[sliceCount-1] = new float[heights.rect.size.x+1, sliceSize+1];

			//filling
			for (int s=0; s<sliceCount; s++) 
			{
				float[,] slice = heights2D[s];
				//int maxX = slice.GetLength(0); int maxZ = slice.GetLength(1);
				for (int x=0; x<sliceSize; x++)
					for (int z=0; z<heights.rect.size.x; z++)
						slice[z,x] = heights[x+heights.rect.offset.x+s*sliceSize, z+heights.rect.offset.z];
			}
			*/

			//preparing out
			int heightSize = heights.rect.size.x+1;
			/*float[,] heights2D = null;
			if (chunk.applyHeight.obj != null) heights2D = (float[,])chunk.applyHeight.obj;
			if (heights2D == null || heights2D.GetLength(0) != heightSize)
				heights2D = new float[heightSize, heightSize];*/
			float[,] heights2D = new float[heightSize, heightSize];
			 
			for (int x=0; x<heights.rect.size.x; x++)
				for (int z=0; z<heights.rect.size.z; z++)
					heights2D[z,x] = heights[x+heights.rect.offset.x, z+heights.rect.offset.z];
			
			//processing sides
			for (int x=0; x<heights.rect.size.x; x++) 
			{
				float prevVal = heights[x+heights.rect.offset.x, heights.rect.size.z-2+heights.rect.offset.z]; //size-2
				float currVal = heights[x+heights.rect.offset.x, heights.rect.size.z-1+heights.rect.offset.z]; //size-1, point on border
				float nextVal = currVal - (prevVal-currVal);
				heights2D[heights.rect.size.z,x] = nextVal;
			}
			for (int z=0; z<heights.rect.size.z; z++)
			{
				float prevVal = heights[heights.rect.size.x-2+heights.rect.offset.x, z+heights.rect.offset.z]; //size-2
				float currVal = heights[heights.rect.size.x-1+heights.rect.offset.x, z+heights.rect.offset.z]; //size-1, point on border
				float nextVal = currVal - (prevVal-currVal);
				heights2D[z,heights.rect.size.x] = nextVal;
			}
			heights2D[heights.rect.size.z,heights.rect.size.x] = heights[heights.rect.size.x+heights.rect.offset.x-1, heights.rect.size.z+heights.rect.offset.z-1];

			//blurring bounds for better welding
			for (int z=1; z<heightSize-1; z++)
			{
				heights2D[z,0] = (heights2D[z-1,0]+heights2D[z+1,0])/2;
				heights2D[z,heightSize-1] = (heights2D[z-1,heightSize-1]+heights2D[z+1,heightSize-1])/2;
			}

			for (int x=1; x<heightSize-1; x++)
			{
				heights2D[0,x] = (heights2D[0,x-1]+heights2D[0,x+1])/2;
				heights2D[heightSize-1,x] = (heights2D[heightSize-1,x-1]+heights2D[heightSize-1,x+1])/2;
			}

			//setting output
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,heights2D);
		}

		public static void Purge (MapMagic.Chunk tw)
		{
			if (tw.locked) return; 
			//if (tw.terrain.terrainData.heightmapResolution == 33) return false; //already cleared
			
			float[,] heights2D = new float[33,33]; //already locked in update
			tw.terrain.terrainData.heightmapResolution = heights2D.GetLength(0);
			tw.terrain.terrainData.SetHeights(0,0,heights2D);
			tw.terrain.terrainData.size = new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize);
			
			if (MapMagic.instance.guiDebug) Debug.Log("Heights Cleared");
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			float[,] heights2D = (float[,])chunk.apply[this];

			//quick lod apply
			/*if (chunk.lod)
			{
				//if (chunk.lodTerrain == null) { chunk.lodTerrain = (MapMagic.instance.transform.AddChild("Terrain " + chunk.coord.x + "," + chunk.coord.z + " LOD")).gameObject.AddComponent<Terrain>(); chunk.lodTerrain.terrainData = new TerrainData(); }
				if (chunk.lodTerrain.terrainData==null) chunk.lodTerrain.terrainData = new TerrainData();

				chunk.lodTerrain.Resize(heights2D.GetLength(0), new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize));
				chunk.lodTerrain.terrainData.SetHeightsDelayLOD(0,0,heights2D);
				
				yield break;
			}*/

			//determining data
			//TerrainData data = null;
			//if (chunk.terrain.terrainData.heightmapResolution == heights2D.GetLength(0)) data = chunk.terrain.terrainData;
			//else data = UnityEngine.Object.Instantiate(chunk.terrain.terrainData);
			
			//resizing terrain
			Vector3 terrainSize = new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize);
			int terrainResolution = heights2D.GetLength(0); //heights2D[0].GetLength(0);
			if ((data.size-terrainSize).sqrMagnitude > 0.01f || data.heightmapResolution != terrainResolution) 
			{
				if (terrainResolution <= 64) //brute force
				{
					data.heightmapResolution = terrainResolution;
					data.size = new Vector3(terrainSize.x, terrainSize.y, terrainSize.z);
				}

				else //setting res 64, re-scaling to 1/64, and then changing res
				{
					data.heightmapResolution = 65;
					chunk.terrain.Flush(); //otherwise unity crushes without an error
					int resFactor = (terrainResolution-1) / 64;
					data.size = new Vector3(terrainSize.x/resFactor, terrainSize.y, terrainSize.z/resFactor);
					data.heightmapResolution = terrainResolution;
				}
			}
			yield return null;

			//welding
			MapMagic.Chunk chunkPrevX = MapMagic.instance.terrains[chunk.coord.x-1, chunk.coord.z]; 
			MapMagic.Chunk chunkNextX = MapMagic.instance.terrains[chunk.coord.x+1, chunk.coord.z]; 
			MapMagic.Chunk chunkPrevZ = MapMagic.instance.terrains[chunk.coord.x, chunk.coord.z-1]; 
			MapMagic.Chunk chunkNextZ = MapMagic.instance.terrains[chunk.coord.x, chunk.coord.z+1]; 

			Terrain terrainPrevX = (chunkPrevX!=null && chunkPrevX.complete)? chunkPrevX.terrain : null;
			Terrain	terrainNextX = (chunkNextX!=null && chunkNextX.complete)? chunkNextX.terrain : null;
			Terrain terrainPrevZ = (chunkPrevZ!=null && chunkPrevZ.complete)? chunkPrevZ.terrain : null;
			Terrain terrainNextZ = (chunkNextZ!=null && chunkNextZ.complete)? chunkNextZ.terrain : null;

			WeldTerrains.WeldHeights(heights2D, terrainPrevX, terrainNextZ, terrainNextX, terrainPrevZ, MapMagic.instance.heightWeldMargins);
			yield return null;

			data.SetHeightsDelayLOD(0,0,heights2D);
			yield return null;
			
			/* sliced
			int sliceCount = heights2D.Length; int sliceSize = heights2D[0].GetLength(1); int heightSize = heights2D[0].GetLength(0);
			for (int c=0; c<sliceCount; c++) 
			{
				wrapper.terrain.terrainData.SetHeightsDelayLOD(sliceSize*c, 0, heights2D[c]);
				yield return null;
			}
			*/

			chunk.terrain.ApplyDelayedHeightmapModification();
			chunk.terrain.Flush();

			//setting nigs - no matter if the terrains were complete or not
			chunk.terrain.SetNeighbors( //terrainPrevX, terrainNextZ, terrainNextX, terrainPrevZ);
				chunkPrevX!=null? chunkPrevX.terrain : null,
				chunkNextZ!=null? chunkNextZ.terrain : null,
				chunkNextX!=null? chunkNextX.terrain : null,
				chunkPrevZ!=null? chunkPrevZ.terrain : null);
			
			//applying new data
			if (chunk.terrain.terrainData != data) chunk.terrain.terrainData = data;

			if (MapMagic.instance.guiDebug) Debug.Log("Height Applied");

			yield return null;
		}
		
		public override void OnGUI ()
		{
			layout.Par(20); input.DrawIcon(layout);
			layout.Par(5);

			layout.Field(ref scale, "Scale", min:0.0625f, max:8f);
			if (scale > 1) scale = Mathf.ClosestPowerOfTwo((int)scale);
			else scale = 1f / Mathf.ClosestPowerOfTwo((int)(1f/scale));
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Textures", disengageable = true)]
	public class SplatOutput : Generator, IOutput, Layout.ILayered
	{
		//layer
		public class Layer : Layout.ILayer
		{
			public Input input = new Input("LayerInput", InoutType.Map, write:false, mandatory:false);
			public Output output = new Output("LayerOutput", InoutType.Map);
			public string name = "Layer";
			public float opacity = 1;
			public SplatPrototype splat = new SplatPrototype();
			
			public bool pinned { get; set; }

			public void OnCollapsedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
				layout.Par(20); 
				if (!pinned) input.DrawIcon(layout, drawLabel:false);
				layout.Label(name, rect:layout.Inset());
				output.DrawIcon(layout, drawLabel:false);
			}

			public void OnExtendedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20); 

				if (!pinned) input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref name, rect:layout.Inset());
				output.DrawIcon(layout, drawLabel:false);

				layout.Par(2);
				layout.Par(60); //not 65
				splat.texture = layout.Field(splat.texture, rect:layout.Inset(60)); 
				splat.normalMap = layout.Field(splat.normalMap, rect:layout.Inset(60));
				layout.Par(2);

				layout.margin = 5; layout.rightMargin = 5; layout.fieldSize = 0.6f;
				//layout.SmartField(ref downscale, "Downscale", min:1, max:8); downscale = Mathf.ClosestPowerOfTwo(downscale);
				opacity = layout.Field(opacity, "Opacity", min:0);
				splat.tileSize = layout.Field(splat.tileSize, "Size");
				splat.tileOffset = layout.Field(splat.tileOffset, "Offset");
				splat.specular = layout.Field(splat.specular, "Specular");
				splat.smoothness = layout.Field(splat.smoothness, "Smooth", max:1);
				splat.metallic = layout.Field(splat.metallic, "Metallic", max:1);
			}

			public void OnAdd () { splat = new SplatPrototype() { texture=defaultTex }; }
			public void OnRemove () 
			{ 
				input.Link(null,null); 
				Input connectedInput = output.GetConnectedInput(MapMagic.instance.gens.list);
				if (connectedInput != null) connectedInput.Link(null, null);
			}
		}
		public Layer[] baseLayers = new Layer[] { new Layer(){pinned=true, name="Background"} };
		public Layout.ILayer[] layers 
		{ 
			get {return baseLayers;} 
			set {baseLayers=ArrayTools.Convert<Layer,Layout.ILayer>(value);} 
		}

		public int selected { get; set; }
		public int collapsedHeight { get; set; }
		public int extendedHeight { get; set; }
		public Layout.ILayer def {get{ return new Layer() {splat=new SplatPrototype() { texture=defaultTex} };	}}
		
		public static Texture2D _defaultTex;
		public static Texture2D defaultTex {get{ if (_defaultTex==null) _defaultTex=Extensions.ColorTexture(2,2,new Color(0.5f, 0.5f, 0.5f, 0f)); return _defaultTex; }}


		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input; 
		}
		public override IEnumerable<Output> Outputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i].input != null)
					yield return baseLayers[i].output; 
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i=0; i<baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null) 
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(chunk);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = chunk.defaultMatrix;
			}
			if (chunk.stop || !enabled) return; 

			//background matrix
			//matrices[0] = terrain.defaultMatrix; //already created
			matrices[0].Fill(1);

			//finding matrices size
			CoordRect rect = matrices[0].rect;

			//subtracting matrices
			float[] row = new float[matrices.Length];

			Coord min = rect.Min; Coord max = rect.Max;
			for (int x=min.x; x<max.x; x++) //TODO: iterate an array
				for (int z=min.z; z<max.z; z++)
			{
				//populating layered row
				for (int i=0; i<matrices.Length; i++) 
				{
					if (chunk.stop) return; //to prevent matrix change error on move

					float val = matrices[i][x,z] * baseLayers[i].opacity;
					if (val < 0) val = 0; if (val > 1) val = 1;
					float invVal = 1-val;
					for (int j=0; j<i; j++) row[j] *= invVal;
					row[i] = val;
				}

				//saving row
				for (int i=0; i<matrices.Length; i++) matrices[i][x,z] = row[i];

				//checking
				float sum = 0;
				for (int i=0; i<matrices.Length; i++) sum += row[i];
				if (sum < 0.999f || sum > 1.001f) { Debug.Log("Layered sum is " + sum); return; }
			}

			//saving changed matrix results
			for (int i=0; i<baseLayers.Length; i++) 
			{
				if (chunk.stop) return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(chunk, matrices[i]);
			}

			//preparing out
			//if (chunk.applySplats.obj != null) splats3D = (float[,,])chunk.applySplats.obj;
			//if (splats3D == null || splats3D.GetLength(0) != MapMagic.instance.resolution || splats3D.GetLength(2) != baseLayers.Length) 
			//	splats3D = new float[MapMagic.instance.resolution, MapMagic.instance.resolution, baseLayers.Length];
			//chunk.applySplats.obj = splats3D;
			float[,,] splats3D = new float[MapMagic.instance.resolution, MapMagic.instance.resolution, baseLayers.Length];

			//writing to out
			for (int s=0; s<baseLayers.Length; s++)
			{
				Matrix matrix = matrices[s];
				for (int x=0; x<matrix.rect.size.x; x++)
					for (int z=0; z<matrix.rect.size.z; z++)
						splats3D[z,x,s] = matrix[x+rect.offset.x, z+matrix.rect.offset.z];
			}		

			//setting output
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,splats3D);
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			float[,,] splats3D = (float[,,])chunk.apply[this];

			//TerrainData data1 = chunk.terrain.terrainData;

			//setting resolution
			int size = splats3D.GetLength(0);
			if (data.alphamapResolution != size) data.alphamapResolution = size;

			//compiling prototypes
			SplatPrototype[] prototypes = new SplatPrototype[baseLayers.Length];
			for (int i=0; i<prototypes.Length; i++)
			{
				prototypes[i] = baseLayers[i].splat;
				
				//checking prototypes texture
				if (prototypes[i].texture == null) prototypes[i].texture = defaultTex;
			}

			//welding
			MapMagic.Chunk chunkPrevX = MapMagic.instance.terrains[chunk.coord.x-1, chunk.coord.z]; 
			MapMagic.Chunk chunkNextX = MapMagic.instance.terrains[chunk.coord.x+1, chunk.coord.z]; 
			MapMagic.Chunk chunkPrevZ = MapMagic.instance.terrains[chunk.coord.x, chunk.coord.z-1]; 
			MapMagic.Chunk chunkNextZ = MapMagic.instance.terrains[chunk.coord.x, chunk.coord.z+1]; 

			Terrain terrainPrevX = (chunkPrevX!=null && chunkPrevX.complete)? chunkPrevX.terrain : null;
			Terrain	terrainNextX = (chunkNextX!=null && chunkNextX.complete)? chunkNextX.terrain : null;
			Terrain terrainPrevZ = (chunkPrevZ!=null && chunkPrevZ.complete)? chunkPrevZ.terrain : null;
			Terrain terrainNextZ = (chunkNextZ!=null && chunkNextZ.complete)? chunkNextZ.terrain : null;

			WeldTerrains.WeldSplats(splats3D, terrainPrevX, terrainNextZ, terrainNextX, terrainPrevZ, MapMagic.instance.splatsWeldMargins);

			//setting
			data.splatPrototypes = prototypes;
			data.SetAlphamaps(0,0,splats3D);

			if (MapMagic.instance.guiDebug) Debug.Log("Textures Applied");

			yield return null;
		}

		public static void Purge (MapMagic.Chunk chunk)
		{
			if (chunk.locked) return;

			SplatPrototype[] prototypes = new SplatPrototype[1];
			if (prototypes[0]==null) prototypes[0] = new SplatPrototype();
			if (prototypes[0].texture==null) prototypes[0].texture = defaultTex;
			chunk.terrain.terrainData.splatPrototypes = prototypes;
		
			float[,,] emptySplats = new float[16,16,1];
			for (int x=0; x<16; x++)
				for (int z=0; z<16; z++)
					emptySplats[z,x,0] = 1;

			chunk.terrain.terrainData.alphamapResolution = 16;
			chunk.terrain.terrainData.SetAlphamaps(0,0,emptySplats);

			if (MapMagic.instance.guiDebug) Debug.Log("Splats Cleared");
		}

		public override void OnGUI () 
		{
			layout.DrawLayered(this, "Layers:");
		}
	}
	
	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Preview", disengageable = true)]
	public class PreviewOutput : Generator, IOutput
	{
		public Input input = new Input("Matrix", InoutType.Map, write:false, mandatory:true);
		public override IEnumerable<Input> Inputs() { yield return input; }

		public bool onTerrain = false;
		public bool inWindow = false;
		public Color blacks = new Color(1,0,0,0); public Color oldBlacks;
		public Color whites = new Color(0,1,0,0); public Color oldWhites;

		public delegate void RefreshWindow(object obj);
		public event RefreshWindow OnObjectChanged;

		//[System.NonSerialized] public SplatPrototype[] prototypes = new SplatPrototype[2]; 
		public SplatPrototype redPrototype = new SplatPrototype();
		public SplatPrototype greenPrototype = new SplatPrototype();

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting input
			object obj = input.GetObject(chunk);
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,obj);
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			//getting input
			object inobj = chunk.apply[this];
			Matrix matrix = inobj as Matrix;
			SpatialHash sphash = inobj as SpatialHash;

			//previewing matrix on terrain  
			if (onTerrain && matrix != null)
			{
				//presertving the number of prototypes
				int numPrototypes = chunk.terrain.terrainData.alphamapLayers;
				if (numPrototypes < 2) numPrototypes = 2;
				
				//preparing out
				float[,,] heights3D = new float[matrix.rect.size.x, matrix.rect.size.z, numPrototypes];
			 
				for (int x=0; x<matrix.rect.size.x; x++)
					for (int z=0; z<matrix.rect.size.z; z++)
				{
					heights3D[z,x,0] = 1-matrix[x+matrix.rect.offset.x, z+matrix.rect.offset.z];
					heights3D[z,x,1] = matrix[x+matrix.rect.offset.x, z+matrix.rect.offset.z];
				}

				//preparing prototypes
				SplatPrototype[] prototypes = chunk.terrain.terrainData.splatPrototypes;
				if (prototypes==null || prototypes.Length<2) prototypes = new SplatPrototype[2];
				if (redPrototype==null) redPrototype = new SplatPrototype();
				if (redPrototype.texture==null) redPrototype.texture = Extensions.ColorTexture(2,2, new Color(1,0,0,0)); 
				if (greenPrototype==null) greenPrototype = new SplatPrototype();
				if (greenPrototype.texture==null) greenPrototype.texture = Extensions.ColorTexture(2,2, new Color(0,1,0,0));
				prototypes[0] = redPrototype; prototypes[1] = greenPrototype;
			
				//apply
				TerrainData data1 = chunk.terrain.terrainData;
				data1.splatPrototypes = prototypes;
				data1.alphamapResolution = heights3D.GetLength(0);
				data1.SetAlphamaps(0,0,heights3D);
			}

			//previewing objects on terrain
			if (onTerrain && sphash != null)
			{
				chunk.previewObjs = sphash;
				//Debug.Log("Preview");
				//foreach (SpatialObject obj in sphash)
				//	Debug.DrawLine( new Vector3(obj.pos.x,0,obj.pos.y), new Vector3(obj.pos.x,MapMagic.instance.terrainHeight,obj.pos.y), Color.white, 10 );
			}

			//refreshing window
			if (inWindow && OnObjectChanged!=null) OnObjectChanged(inobj);

			yield return null;


			//Projector
			//if (wrapper.debugProjector == null)
			//{
			//	GameObject obj = new GameObject("Debug Projector");
			//	obj.transform.parent = wrapper.terrain.transform.parent;
			//	obj.transform.position = new Vector3((wrapper.coord.x+0.5f)*MapMagic.terrainSize, MapMagic.terrainHeight+1, (wrapper.coord.z+0.5f)*MapMagic.terrainSize);
			//	obj.transform.localEulerAngles = new Vector3(90,0,0);
			//	wrapper.debugProjector = obj.AddComponent<Projector>();
			//	wrapper.debugProjector.farClipPlane = MapMagic.terrainHeight+1;
			//	wrapper.debugProjector.material = new Material(Shader.Find("Mobile/Particles/Multiply"));
			//	wrapper.debugProjector.orthographic = true;
			//	wrapper.debugProjector.orthographicSize = MapMagic.terrainSize/2f-1;
			//}

			//creating texture
			//Color[] pixels = (Color[])wrapper.outputResults[this];
			//int texSize = (int)Mathf.Sqrt(pixels.Length);
			//Texture2D tex = new Texture2D(texSize, texSize);
			//tex.SetPixels(pixels);
			//tex.Apply();

			//assigning texture
			//wrapper.debugProjector.material.SetTexture("_MainTex", tex);
		}

		public static void Purge (MapMagic.Chunk chunk)
		{
			if (chunk.locked) return;
//			tw.applySplats = true; tw.hasSplats = true;
//			SplatOutput.Consume(tw);
			chunk.previewObjs = null;
			Debug.Log(chunk.previewObjs);
			if (MapMagic.instance.guiDebug) Debug.Log("Preview Cleared");
		}



				
		public override void OnGUI ()
		{
			layout.Par(20); input.DrawIcon(layout);
			layout.Par(5);

			layout.Field(ref onTerrain, "On Terrain");
			layout.Field(ref inWindow, "In Window");
			layout.Field(ref whites, "Whites");
			layout.Field(ref blacks, "Blacks");
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Objects", disengageable = true)]
	public class ObjectOutput : Generator, IOutput, Layout.ILayered
	{
		//layer
		public class Layer : Layout.ILayer
		{
			public Input input = new Input("LayerInput", InoutType.Objects, write:false, mandatory:false);
			
			public Transform prefab;
			public bool rotate = true;
			public bool scale = true;
			public bool scaleY;
			public bool usePool = true;
			public bool parentToRoot = false;

			//public bool processChildren = false;
			//public bool floorChildren;
			//public Vector2 rotateChildren;
			//public Vector2 scaleChildren;
			//public float removeChildren = 0;
			
			public bool pinned { get; set; }

			public void OnCollapsedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20); 
				input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref prefab, rect:layout.Inset());
			}

			public void OnExtendedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20); 

				input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref prefab, rect:layout.Inset());

				layout.Par(); layout.Toggle(ref rotate, rect:layout.Inset(20)); layout.Label("Rotate", rect:layout.Inset(45)); 
				layout.Par(); layout.Toggle(ref scale, rect:layout.Inset(20)); layout.Label("Scale", rect:layout.Inset(40)); //if (layout.lastChange) scaleY = false;
				layout.disabled = !scale;
				layout.Toggle(ref scaleY, rect:layout.Inset(18)); layout.Label("Y only", rect:layout.Inset(45)); //if (layout.lastChange) scaleU = false;
				layout.disabled = false;
				layout.Toggle(ref usePool, "Use Object Pool");

				//layout.Par(); layout.Field(ref processChildren, width:20); layout.Label("Process Children"); 
				//if (processChildren)
				//{
				//	layout.margin += 10;
				//	layout.ComplexField(ref floorChildren, "Floor");
				//	layout.SmartField(ref rotateChildren, "Rotate", min:-360, max:360);
				//	layout.SmartField(ref scaleChildren, "Scale", min:-2, max:2);
				//	layout.SmartField(ref removeChildren, "Delete", max:1);
				//	layout.margin -= 10;
				//	layout.Par(3);
				//}
			}

			public void OnAdd () {  }
			public void OnRemove () { input.Link(null,null); }
		}
		public Layer[] baseLayers = new Layer[0];
		public Layout.ILayer[] layers 
		{ 
			get {return baseLayers;} 
			set {baseLayers=ArrayTools.Convert<Layer,Layout.ILayer>(value);} 
		}

		public int selected { get; set; }
		public int collapsedHeight { get; set; }
		public int extendedHeight { get; set; }
		public Layout.ILayer def {get{ return new Layer(); }}


		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input; 
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			//preparing output
			TransformPool.InstanceDraft[][] instancesArray = new TransformPool.InstanceDraft[baseLayers.Length][];

			for (int i=0; i<baseLayers.Length; i++)
			{
				Layer layer = baseLayers[i];
				
				//loading input
				SpatialHash objs = (SpatialHash)baseLayers[i].input.GetObject(chunk);
				if (objs==null) { instancesArray[i] = new TransformPool.InstanceDraft[0]; continue; }

				//creating instances
				TransformPool.InstanceDraft[] instances = new TransformPool.InstanceDraft[objs.Count];
				instancesArray[i] = instances;

				//filling instances
				int counter = 0;
				foreach (SpatialObject obj in objs.AllObjs())
				{
					Vector3 pos = new Vector3(obj.pos.x, 0, obj.pos.y);
					pos = pos / objs.size * MapMagic.instance.terrainSize;
					pos.y = obj.height * MapMagic.instance.terrainHeight;
					if (!baseLayers[i].parentToRoot) pos -= chunk.coord.vector3*MapMagic.instance.terrainSize; //local to parent terrain

					Quaternion rot = layer.rotate? (obj.rotation%360).EulerToQuat() : Quaternion.identity;
					Vector3 scl = layer.scale? new Vector3(layer.scaleY?1:obj.size, obj.size, layer.scaleY?1:obj.size) : Vector3.one;

					instances[counter] = new TransformPool.InstanceDraft() { pos=pos, rotation=rot, scale=scl };
					counter++;
				}
			}

			//setting output
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,instancesArray);
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			System.Diagnostics.Stopwatch timer = null; if (MapMagic.instance.guiDebug) { timer = new System.Diagnostics.Stopwatch(); timer.Start(); }

			TransformPool.InstanceDraft[][] instancesArray = (TransformPool.InstanceDraft[][])chunk.apply[this];
			if (instancesArray.Length != baseLayers.Length) Debug.LogError("Object Output: instances count not equal to layers length");

			//pools operations
			//creating or re-creating pools
			if (chunk.pools == null) chunk.pools = new TransformPool[baseLayers.Length];

			//removing extra pools with objects
			if (chunk.pools.Length > baseLayers.Length)
			{
				TransformPool[] newPools = new TransformPool[baseLayers.Length];
				for (int i=0; i<baseLayers.Length; i++) newPools[i] = chunk.pools[i];
				for (int i=baseLayers.Length; i<chunk.pools.Length; i++) chunk.pools[i].Clear();
				chunk.pools = newPools;
			}

			//adding lacking pools
			if (chunk.pools.Length < baseLayers.Length)
			{
				TransformPool[] newPools = new TransformPool[baseLayers.Length];
				for (int i=0; i<chunk.pools.Length; i++) newPools[i] = chunk.pools[i];
				chunk.pools = newPools;
			}

			
			for (int i=0; i<baseLayers.Length; i++) 
			{
				//filling nulls with new pools
				if (chunk.pools[i] == null) chunk.pools[i] = new TransformPool() { prefab=baseLayers[i].prefab, parent=chunk.terrain.transform };

				//comparing pool prefabs with layer prefabs
				if (chunk.pools[i].prefab != baseLayers[i].prefab)
				{
					chunk.pools[i].Clear();
					chunk.pools[i].prefab = baseLayers[i].prefab;
				}

				//other params
				chunk.pools[i].allowReposition = baseLayers[i].usePool;
				chunk.pools[i].parent = !baseLayers[i].parentToRoot? chunk.terrain.transform : null;
			}

			//instantiating
			for (int i=0; i<baseLayers.Length; i++)
			{
				if (baseLayers[i].prefab == null) { chunk.pools[i].Clear(); continue; }

				IEnumerator e = chunk.pools[i].SetTransformsCoroutine(instancesArray[i]);
				while (e.MoveNext()) yield return null;
			}

			if (timer != null) { timer.Stop(); Debug.Log("Objects applied  ( " + chunk.coord + "): " + timer.ElapsedMilliseconds + "ms"); }
			

			yield return null;
		}


		public static void Purge (MapMagic.Chunk chunk)
		{
			if (chunk.locked || chunk.pools==null) return;
			for (int i=0; i<chunk.pools.Length; i++)
				chunk.pools[i].Clear();
			if (MapMagic.instance.guiDebug) Debug.Log("Objects Cleared");
		}

	
		public override void OnGUI ()
		{
			layout.DrawLayered(this, "Layers:");
		}

	}
	
	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Trees", disengageable = true)]
	public class TreesOutput : Generator, IOutput, Layout.ILayered
	{
		//layer
		public class Layer : Layout.ILayer
		{
			public Input input = new Input("LayerInput", InoutType.Objects, write:false, mandatory:false);
			
			public GameObject prefab;
			public bool rotate;
			public bool widthScale;
			public bool heightScale;
			public Color color = Color.white;
			public float bendFactor;

			public bool pinned { get; set; }

			public void OnCollapsedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20); 
				input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref prefab, rect:layout.Inset());
			}

			public void OnExtendedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20); 

				input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref prefab, rect:layout.Inset());

				layout.Par(); layout.Toggle(ref rotate, rect:layout.Inset(20)); layout.Label("Rotate", rect:layout.Inset(45)); 
				layout.Par(); layout.Toggle(ref widthScale, rect:layout.Inset(20)); layout.Label("Width Scale", rect:layout.Inset(100));
				layout.Par(); layout.Toggle(ref heightScale, rect:layout.Inset(20)); layout.Label("Height Scale", rect:layout.Inset(100));
				layout.fieldSize = 0.37f;
				layout.Field(ref color, "Color");
				layout.Field(ref bendFactor, "Bend Factor");
			}

			public void OnAdd () { }
			public void OnRemove () { input.Link(null,null); }
		}
		public Layer[] baseLayers = new Layer[0];
		public Layout.ILayer[] layers 
		{ 
			get {return baseLayers;} 
			set {baseLayers=ArrayTools.Convert<Layer,Layout.ILayer>(value);} 
		}

		public int selected { get; set; }
		public int collapsedHeight { get; set; }
		public int extendedHeight { get; set; }
		public Layout.ILayer def {get{ return new Layer(); }}


		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input; 
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			//getting inputs
			SpatialHash[] hashes = new SpatialHash[baseLayers.Length];
			for (int i=0; i<baseLayers.Length; i++)
				hashes[i] = (SpatialHash)baseLayers[i].input.GetObject(chunk);

			//calculating total tree count
			int treeCount = 0;
			for (int i=0; i<hashes.Length; i++) 
				if (hashes[i] != null) treeCount += hashes[i].Count;
			
			//preparing output
			TreeInstance[] instances = new TreeInstance[treeCount];

			//filling instances
			int counter = 0;
			for (int i=0; i<hashes.Length; i++)
			{
				SpatialHash hash = hashes[i];
				Layer layer = baseLayers[i];
				if (hash == null) continue;

				foreach (SpatialObject obj in hashes[i].AllObjs())
				{
					TreeInstance tree = new TreeInstance();
					tree.position = new Vector3(
						(obj.pos.x-hash.offset.x)/hash.size,
						obj.height,
						(obj.pos.y-hash.offset.y)/hash.size );
					tree.rotation = layer.rotate? obj.rotation%360 : 0; 
					tree.widthScale = layer.widthScale? obj.size : 1;
					tree.heightScale = layer.heightScale? obj.size : 1;
					tree.prototypeIndex = i;
					tree.color = layer.color;
					tree.lightmapColor = layer.color;
					
					instances[counter] = tree;
					counter++;
				}
			}

			//setting output
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,instances);
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			System.Diagnostics.Stopwatch timer = null; if (MapMagic.instance.guiDebug) { timer = new System.Diagnostics.Stopwatch(); timer.Start(); }

			//prototypes
			TreePrototype[] prototypes = new TreePrototype[baseLayers.Length];
			for (int i=0; i<baseLayers.Length; i++)
			{
				//if (baseLayers[i].prefab == null) baseLayers[i].prefab = new GameObject() {name="Empty"};
				prototypes[i] = new TreePrototype() { prefab=baseLayers[i].prefab, bendFactor=baseLayers[i].bendFactor };
			}
			chunk.terrain.terrainData.treePrototypes = prototypes;

			//instances
			TreeInstance[] instances = (TreeInstance[])chunk.apply[this];
			chunk.terrain.terrainData.treeInstances = instances;

			if (timer != null) { timer.Stop(); Debug.Log("Trees applied  ( " + chunk.coord + "): " + timer.ElapsedMilliseconds + "ms"); }

			if (MapMagic.instance.guiDebug) Debug.Log("Trees Applied");

			yield return null;
		}


		public static void Purge (MapMagic.Chunk chunk)
		{
			if (chunk.locked) return;
			chunk.terrain.terrainData.treeInstances = new TreeInstance[0];
			chunk.terrain.terrainData.treePrototypes = new TreePrototype[0];
		}

	
		public override void OnGUI ()
		{
			layout.DrawLayered(this, "Layers:");
		}

	}
	

	[System.Serializable]
	[GeneratorMenu (menu="Output", name ="Grass", disengageable = true)]
	public class GrassOutput : Generator, IOutput, Layout.ILayered
	{
		//layer
		public class Layer : Layout.ILayer
		{
			public Input input = new Input("LayerInput", InoutType.Map, write:false, mandatory:false);
			
			public DetailPrototype det = new DetailPrototype();
			public string name;
			public float density = 0.5f;

			public bool pinned { get; set; }

			public void OnCollapsedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
				layout.Par(20); 
				input.DrawIcon(layout, drawLabel:false);
				layout.Label(name, rect:layout.Inset());
			}

			public void OnExtendedGUI (Layout layout) 
			{
				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20); 

				input.DrawIcon(layout, drawLabel:false);
				layout.Field(ref name, rect:layout.Inset());

				layout.margin = 5; layout.rightMargin = 10; layout.fieldSize = 0.6f;
				layout.fieldSize = 0.65f;

				det.renderMode = layout.Field(det.renderMode, "Mode");

				if (det.renderMode == DetailRenderMode.VertexLit)
				{
					det.prototype = layout.Field(det.prototype, "Object");
					det.prototypeTexture = null; //otherwise this texture will be included to build even if not displayed
					det.usePrototypeMesh = true;
				}
				else
				{
					layout.Par(60); //not 65
					layout.Inset((layout.field.width-60)/2);
					det.prototypeTexture = layout.Field(det.prototypeTexture, rect:layout.Inset(60)); 
					det.prototype = null; //otherwise this object will be included to build even if not displayed
					det.usePrototypeMesh = false;
					layout.Par(2);
				}

				density = layout.Field(density, "Density", max:50);
				det.bendFactor = layout.Field(det.bendFactor, "Bend");
				det.dryColor = layout.Field(det.dryColor, "Dry");
				det.healthyColor = layout.Field(det.healthyColor, "Healthy");

				Vector2 temp = new Vector2(det.minWidth, det.maxWidth);
				layout.Field(ref temp, "Width", max:10);
				det.minWidth = temp.x; det.maxWidth = temp.y;

				temp = new Vector2(det.minHeight, det.maxHeight);
				layout.Field(ref temp, "Height", max:10);
				det.minHeight = temp.x; det.maxHeight = temp.y;

				det.noiseSpread = layout.Field(det.noiseSpread, "Noise", max:1);
			}

			public void OnAdd () { name="Grass"; }
			public void OnRemove () { input.Link(null,null); }
		}
		public Layer[] baseLayers = new Layer[0];
		public Layout.ILayer[] layers 
		{ 
			get {return baseLayers;} 
			set {baseLayers=ArrayTools.Convert<Layer,Layout.ILayer>(value);} 
		}

		public int selected { get; set; }
		public int collapsedHeight { get; set; }
		public int extendedHeight { get; set; }
		public Layout.ILayer def {get{ return new Layer() { name="Grass" }; }}

		//params
		public Input maskIn = new Input("Mask", InoutType.Map, write:false, mandatory:false);
		public int patchResolution = 16;
		public bool obscureLayers = false;

		//generator
		public override IEnumerable<Input> Inputs() 
		{ 
			if (maskIn==null) maskIn = new Input("Mask", InoutType.Map, write:false, mandatory:false); //for backwards compatibility, input should not be null
			yield return maskIn;

			if (baseLayers==null) baseLayers = new Layer[0];
			for (int i=0; i<baseLayers.Length; i++) 
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input; 
		}

		public override void Generate (MapMagic.Chunk chunk)
		{
			//preparing mask
			Matrix mask = (Matrix)maskIn.GetObject(chunk);

			//preparing matrices and details
			Matrix[] matrices = new Matrix[baseLayers.Length];
			int[][,] details = new int[baseLayers.Length][,];
			for (int i=0; i<baseLayers.Length; i++)
			{
				matrices[i] = (Matrix)baseLayers[i].input.GetObject(chunk);
				details[i] = new int[MapMagic.instance.resolution, MapMagic.instance.resolution];
			}

			//values to calculate density
			float pixelSize = 1f * MapMagic.instance.terrainSize / MapMagic.instance.resolution;
			float pixelSquare = pixelSize*pixelSize;

			//generating
			InstanceRandom rnd = new InstanceRandom(MapMagic.instance.seed + chunk.coord.x*1000 + chunk.coord.z);
			float[] slice = new float[baseLayers.Length];

			for (int x=0; x<MapMagic.instance.resolution; x++)
				for (int z=0; z<MapMagic.instance.resolution; z++)
			{
				//populating slice
				for (int i=0; i<baseLayers.Length; i++)
				{
					Matrix matrix = matrices[i];
					if (matrix == null) continue;
						
					float num = matrix.array[z*matrix.rect.size.x + x];
					if (mask!=null) num *= mask.array[z*matrix.rect.size.x + x];

					//float num = matrix.GetAveragedValue(x+matrix.rect.offset.x, z+matrix.rect.offset.z, step);
					num *= baseLayers[i].density * pixelSquare;
					slice[i] = num;
				}

				//obscuring slice
				if (obscureLayers)
				{
					for (int i=0; i<slice.Length; i++) 
					{
						float invNum = 1 - slice[i];
						for (int j=0; j<i; j++)
							slice[j] *= invNum;
					}
				}

				//writing to arrays
				for (int i=0; i<baseLayers.Length; i++)
					details[i][z,x] = rnd.RandomToInt(slice[i]); //note that terrain x and z swapped
			}

			//saving result
			if (chunk.stop) return;
			if (chunk.apply.ContainsKey(this)) chunk.apply.Remove(this);
			chunk.apply.Add(this,details);
		}

		public IEnumerator Apply (MapMagic.Chunk chunk, TerrainData data)
		{
			System.Diagnostics.Stopwatch timer = null; if (MapMagic.instance.guiDebug) { timer = new System.Diagnostics.Stopwatch(); timer.Start(); }

			//TerrainData data = chunk.terrain.terrainData;

			//resolution
			data.SetDetailResolution(MapMagic.instance.resolution, patchResolution);

			//prototypes
			DetailPrototype[] prototypes = new DetailPrototype[baseLayers.Length];
			for (int i=0; i<baseLayers.Length; i++)
				prototypes[i] = baseLayers[i].det;
			chunk.terrain.terrainData.detailPrototypes = prototypes;

			//instances
			int[][,] details = (int[][,])chunk.apply[this];
			for (int i=0; i<details.Length; i++)
				chunk.terrain.terrainData.SetDetailLayer(0,0,i,details[i]);

			if (timer != null) { timer.Stop(); Debug.Log("Grass applied  ( " + chunk.coord + "): " + timer.ElapsedMilliseconds + "ms"); }

			if (MapMagic.instance.guiDebug) Debug.Log("Grass Applied");

			yield return null;
		}

		public static void Purge (MapMagic.Chunk chunk)
		{
			if (chunk.locked) return;

			DetailPrototype[] prototypes = new DetailPrototype[0];
			chunk.terrain.terrainData.detailPrototypes = prototypes;
			chunk.terrain.terrainData.SetDetailResolution(16, 8);

			if (MapMagic.instance.guiDebug) Debug.Log("Grass Cleared");
		}

		public override void OnGUI () 
		{
			layout.Par(20); maskIn.DrawIcon(layout);

			layout.Field(ref patchResolution, "Patch Res", min:4, max:64, fieldSize:0.35f);
			patchResolution = Mathf.ClosestPowerOfTwo(patchResolution);
			layout.Field(ref obscureLayers, "Obscure Layers", fieldSize:0.35f);
			layout.Par(3);
			layout.DrawLayered(this, "Layers:");

			layout.fieldSize = 0.4f; layout.margin = 10; layout.rightMargin = 10;
			layout.Par(5);
		}

	}

	[System.Serializable]
	[GeneratorMenu (menu="", name ="Portal", disengageable = true)]
	public class Portal : Generator
	{
		public Input input = new Input("Input", InoutType.Map, write:false, mandatory:true);
		public Output output = new Output("Output", InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { yield return output; }
		
		//public enum PortalType { enter, exit }
		//public PortalType type;

		public InoutType type;
		public enum PortalForm { In, Out }
		public PortalForm form;
		
		public delegate void ChooseEnter(Portal sender, InoutType type);
		public static event ChooseEnter OnChooseEnter;

		public override void Generate (MapMagic.Chunk terrain)
		{
			object obj = null;
			if (input.link != null && enabled) obj = input.GetObject(terrain);
			else 
			{ 
				if (type == InoutType.Map) obj = terrain.defaultMatrix;
				if (type == InoutType.Objects) obj = terrain.defaultSpatialHash;
			}

			if (terrain.stop) return;
			output.SetObject(terrain, obj); 
		}

		public override void OnGUI ()
		{
			layout.margin = 18; layout.rightMargin = 15;
			layout.Par(20); 
			if (form == PortalForm.In) input.DrawIcon(layout, drawLabel:false); 
			else output.DrawIcon(layout, drawLabel:false);

			layout.Field(ref type, rect:layout.Inset(0.33f));
			if (type != input.type) { input.Unlink(); input.type = type; output.type = type; }

			layout.Field(ref form,rect:layout.Inset(0.27f));

			//select input/button
			if (form == PortalForm.In) input.guiName = layout.Field(input.guiName, rect:layout.Inset(0.4f));
			if (form == PortalForm.Out)
			{
				string buttonLabel = "Select";
				if (input.linkGen != null) 
				{
					if (!(input.linkGen is Portal)) input.Link(null, null); //in case connected input portal was changet to output
					else buttonLabel = ((Portal)input.linkGen).input.guiName;
				}
				Rect buttonRect = layout.Inset(0.4f);
				buttonRect.height -= 3;
				if (layout.Button(buttonLabel, rect:buttonRect) && OnChooseEnter!=null) OnChooseEnter(this, type);
			}
		}
	}
}
