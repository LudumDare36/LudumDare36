using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

//using Plugins;

namespace MapMagic 
{
	[SelectionBase]
	[ExecuteInEditMode]
	public class MapMagic : MonoBehaviour
	{
		public static readonly int version = 14; 

		#region TerrainGrid
		[System.Serializable]
		public class TerrainGrid : ObjectGrid<Chunk>
		{
			//public float enableRange = 1400; //for switchable lods
			//public float detailRange = 400;
			
			//wrapper params applied to all terrains
			public bool start { 
				get { bool allStart = true; foreach(KeyValuePair<int,Chunk> kvp in grid) allStart = allStart && kvp.Value.start; return allStart; }
				set { foreach(KeyValuePair<int,Chunk> kvp in grid) kvp.Value.start = value; } }
			public bool stop { 
				get { bool allStop = true; foreach(KeyValuePair<int,Chunk> kvp in grid) allStop = allStop && kvp.Value.stop; return allStop; }
				set { foreach(KeyValuePair<int,Chunk> kvp in grid) kvp.Value.stop = value; } }
			public bool running { 
				get { foreach(KeyValuePair<int,Chunk> kvp in grid) if (kvp.Value.running) return true; return false; }}
			public bool complete { 
				get { foreach(KeyValuePair<int,Chunk> kvp in grid) if (!kvp.Value.complete) return false; return true; }}

			//public Chunk closestChunk { get{ return MapMagic.instance.terrains.GetClosestObj(MapMagic.instance.camPos.FloorToCoord(MapMagic.instance.terrainSize)); }}

			//guards for checking if rects changed on deploy
			public CoordRect[] prevRects = new CoordRect[0];
			public CoordRect[] currRects = new CoordRect[0];
			public Coord[] currCenters = new Coord[0];
			public Coord[] prevCenters = new Coord[0]; //not a guard, used for reset only


			public override Chunk Construct () { return new Chunk(); }

			public override void OnCreate (Chunk chunk, Coord coord)
			{
				//creating gameobject
				GameObject go = new GameObject();
				go.name = "Terrain " + coord.x + "," + coord.z;
				go.transform.parent = instance.transform;
				go.transform.localPosition = coord.ToVector3(instance.terrainSize);

				//creating terrain
				chunk.terrain = go.AddComponent<Terrain>();
				TerrainCollider terrainCollider = go.AddComponent<TerrainCollider>();

				TerrainData terrainData = new TerrainData();
				chunk.terrain.terrainData = terrainData;
				terrainCollider.terrainData = terrainData;
				terrainData.size = new Vector3(instance.terrainSize, instance.terrainHeight, instance.terrainSize);

				//chunk settings
				chunk.coord = coord;
				chunk.SetSettings(instance);
				chunk.clear = true;
				//if (!instance.isEditor || instance.instantGenerate) { chunk.start = true; }

				if (instance.isEditor) if (RepaintWindow != null) RepaintWindow();
			}

			public override void OnMove (Chunk chunk, Coord newCoord) 
			{
				//chunk.terrain.gameObject.SetActive(false); //wrapper.terrain.enabled = false; 
				chunk.coord = newCoord;
				chunk.terrain.transform.localPosition = newCoord.ToVector3(instance.terrainSize);

				//stopping all generate and all apply (they will be resumed in processthread)
				chunk.stop = true;
				MapMagic.instance.StopAllCoroutines(); MapMagic.instance.applyRunning = false;

				//resetting results
				chunk.results.Clear();
				chunk.ready.Clear();
				chunk.clear = true;

				//for (int i=0; i<generators.Length; i++) 
				//	if (wrapper.generated.Contains(generators[i])) wrapper.generated.Remove(generators[i]); //generators.array[i].SetGenerated(wrapper, false);
				//if (!instance.isEditor || instance.instantGenerate) { chunk.start = true; }
			}

			public override void OnRemove (Chunk chunk) 
			{ 
				//stopping all generate and all apply
				chunk.stop = true;
				MapMagic.instance.StopAllCoroutines(); MapMagic.instance.applyRunning = false;

				if (chunk.terrain != null) //it could be destroyed by undo
					GameObject.DestroyImmediate(chunk.terrain.gameObject);
			}

			public void Deploy (Vector3 pos, bool allowMove=true) { Deploy( new Vector3[] {pos}, allowMove:allowMove); }

			public void Deploy (Vector3[] poses, bool allowMove=true)
			{
				bool reDeploy = false;

				//checking guard arrays
				if (prevRects == null || prevRects.Length != poses.Length) 
				{ 
					reDeploy = true; 
					prevRects = new CoordRect[poses.Length]; currRects = new CoordRect[poses.Length]; 
					prevCenters = new Coord[poses.Length]; currCenters = new Coord[poses.Length]; 
				}

				for (int p=0; p<poses.Length; p++)
				{
					Vector3 pos = poses[p];

					//finding rect and center
					//currCenters[p]	= new Coord( Mathf.RoundToInt(pos.x / instance.terrainSize),		Mathf.RoundToInt(pos.z / instance.terrainSize)  );
					//Coord min		= new Coord( Mathf.FloorToInt((pos.x-lodRange)/instance.terrainSize),	Mathf.FloorToInt((pos.z-lodRange)/instance.terrainSize)  );
					//Coord max		= new Coord( Mathf.FloorToInt((pos.x+lodRange)/instance.terrainSize),	Mathf.FloorToInt((pos.z+lodRange)/instance.terrainSize) )  +  1;
					//currRects[p]	= new CoordRect(min, max-min);
					currCenters[p] = pos.RoundToCoord(instance.terrainSize);
					currRects[p] = pos.ToCoordRect(instance.generateRange, instance.terrainSize);

					//checking a need to re-deploy
					if (currRects[p] != prevRects[p]) reDeploy = true;
					prevRects[p] = currRects[p];
					prevCenters[p] = currCenters[p];
				}

				//deploy
				if (!reDeploy) return;
				base.Deploy(currRects, currCenters, allowMove:allowMove);
				
				//checking if chunk in ranege to hide/show it
				//foreach (Chunk chunk in Objects())
				//	{ chunk.inRange = false; for (int r=0; r<currRects.Length; r++) if (currRects[r].CheckInRange(chunk.coord)) chunk.inRange = true; }
			}

			public void SwitchState (Vector3[] poses)
			{
				Rect[] enableRects = new Rect[poses.Length];
				//Rect[] detailRects = new Rect[poses.Length];

				for (int p=0; p<poses.Length; p++)
				{
					Vector3 pos = poses[p];
					enableRects[p] = new Rect(pos.x-instance.generateRange, pos.z-instance.generateRange, instance.generateRange*2, instance.generateRange*2);
					//enableRects[p] = new Rect(pos.x-enableRange, pos.z-enableRange, enableRange*2, enableRange*2);
					//detailRects[p] = new Rect(pos.x-detailRange, pos.z-detailRange, detailRange*2, detailRange*2);
				}
					
				
				foreach (Chunk chunk in Objects())
				{
					//determining in what rect the chunk is
					Rect chunkRect = new Rect(chunk.coord.x*instance.terrainSize, chunk.coord.z*instance.terrainSize, instance.terrainSize, instance.terrainSize); //new Rect(chunk.terrain.transform.position, new Vector2(instance.terrainSize, instance.terrainSize));
					bool inEnableRect = chunkRect.Intersects(enableRects);
					//bool inDetailRect = chunkRect.Intersects(detailRects);

					//unpinning chunk if it's terrain was removed somehow
					if (chunk.terrain == null) 
					{
						Unnail(chunk.coord, remove: true);
						break;
					}

					//showing full detail and enabled terrains only in editor
					if (instance.isEditor) 
					{ 
						if (!chunk.running && chunk.clear && instance.instantGenerate) chunk.start = true; 
						if (!chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(true);
					}
					
					//generating lods in playmode
					else 
					{
						//generating terrain
						if (inEnableRect && !chunk.running && chunk.clear) chunk.start = true; //{ chunk.lod = true; chunk.start = true; }
						//if (inDetailRect && chunk.lod && chunk.complete) { chunk.lod = false; chunk.start = true; } //only when it was generated in lores
					}

					//enabling/disabling
					if (((inEnableRect||!instance.hideFarTerrains) && chunk.complete) || instance.isEditor) 
						{ if (!chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(true); }
					else
						{ if (chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(false); }

					//generating lod
					//if (inLodRect && !inDetRect && !(chunk.rea

					


					//if (isEditor) //if editor - enabling all the terrains
					//	{ if (!chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(true); }
					/*else if (chunk.complete && (chunk.inRange || !hideFarTerrains || isEditor)) //if complete and in range - enabling
						{ if (!chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(true); }
					else 
						{ if (chunk.terrain.gameObject.activeSelf) chunk.terrain.gameObject.SetActive(false); }*/
				}
			}

			public virtual void Reset () 
			{
				foreach (Chunk chunk in Objects()) 
					if (chunk != null) OnRemove(chunk);
				grid = new Dictionary<int,Chunk>();

				HashSet<int> oldNailedHashes = nailedHashes;
				nailedHashes = new HashSet<int>();

				//if (reDeploy)
				//{
					foreach (int nailedHash in oldNailedHashes) Nail(nailedHash.ToCoord());
					Deploy(prevRects, prevCenters);
				//}
			}

			public void CheckEmpty ()
			{
				foreach (Chunk chunk in Objects()) 
					if (chunk==null || chunk.terrain==null || chunk.terrain.terrainData==null) { Reset(); break; }
			}
		}
		#endregion

		//terrains and generators
		public TerrainGrid terrains = new TerrainGrid();
		public GeneratorsAsset gens;

		[System.NonSerialized] public int runningThreadsCount = 0;
		[System.NonSerialized] public bool applyRunning = false; 

		//other static parameters
		public int seed = 12345;
		public int terrainSize = 1000; //should be int to avoid terrain start between pixels
		public int terrainHeight = 200;
		public int resolution = 512;
		public int lodResolution = 128;

		//public static MapMagic _instance;
		//public static MapMagic instance {get{if (_instance==null) _instance=FindObjectOfType<MapMagic>(); return _instance; }}
		public static MapMagic instance = null;

		//public Vector3 camPos = Vector3.zero;
		private Vector3[] camPoses; //this arrays will be reused and will never be used directly
		private Coord[] camCoords; 
		public int mouseButton = -1; //mouse button in MapMagicWindow, not scene view. So it is not scene view delegate, but assigned from window script

		public bool generateInfinite = true;
		public int generateRange = 300;
		//trail count is in ObjectGrid script

		//events
		public delegate void ApplyEvent (Terrain terrain, object obj);
		public delegate void ChangeEvent (Terrain terrain);
		public static event ApplyEvent OnApply;
		public static event ChangeEvent OnGenerateStarted;
		public static event ChangeEvent OnGenerateCompleted;
		public static event ChangeEvent OnApplyCompleted;

		//preview
		#if UNITY_EDITOR
		//public bool preview {get{ return (previewInWindow || previewOnTerrain) && previewGenerator!=null && previewGenerator.enabled; }}
		[System.NonSerialized] public Generator previewGenerator = null;
		[System.NonSerialized] public Generator.Output previewOutput = null;
		#endif

		//settings
		public bool multiThreaded = true;
		public int maxThreads = 2;
		public bool instantGenerate = true;
		public bool saveIntermediate = true;
		public int heightWeldMargins = 5;
		public int splatsWeldMargins = 2;
		public bool hideWireframe = true;
		public bool hideFarTerrains = true;
		public bool useAllCameras = false;
		public bool copyLayersTags = true;
		public bool copyComponents = true;

		//terrain settings
		public int pixelError = 1;
		public int baseMapDist = 1000;
		public bool castShadows = false;
		public Material terrainMaterial = null;

		//details and trees
		public bool detailDraw = true;
		public float detailDistance = 80;
		public float detailDensity = 1;
		public float treeDistance = 1000;
		public float treeBillboardStart = 200;
		public float treeFadeLength = 5;
		public int treeFullLod = 150;

		public float windSpeed = 0.5f;
		public float windSize = 0.5f;
		public float windBending = 0.5f;
		public Color grassTint = Color.gray;

		[System.NonSerialized] public System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

		public int selected=0;
		public Vector2 guiScroll = new Vector2(0,0);
		public float guiZoom = 1;
		public bool guiGenerators = true;
		public bool guiSettings = false;
		public bool guiTerrainSettings = false;
		public bool guiTreesGrassSettings = false;
		public bool guiDebug = false;
		public bool guiAbout = false;
		public GameObject sceneRedrawObject;
	
		public delegate void RepaintWindowAction();
		public static event RepaintWindowAction RepaintWindow;

		public bool setDirty; //registering change for undo. Inverting this value if Unity Undo does not see a change, but actually there is one (for example when saving data)

		#region Demo lock

			[SerializeField] private int processId;

			public bool locked { get{ return false; }} 
			//	if (processId == -42) processId = System.Diagnostics.Process.GetCurrentProcess().Id;
			//	return System.Diagnostics.Process.GetCurrentProcess().Id != processId; 
			//}}

			public void UnlockAndReset ()
			{
			//	if (gens!=null) gens.ClearGenerators();
			//	processId = System.Diagnostics.Process.GetCurrentProcess().Id;
			}

		#endregion


		#region isEditor isSelected
		public bool isEditor 
		{get{
			#if UNITY_EDITOR
				return 
					!UnityEditor.EditorApplication.isPlaying; //if not playing
					//(UnityEditor.EditorWindow.focusedWindow != null && UnityEditor.EditorWindow.focusedWindow.GetType() == System.Type.GetType("UnityEditor.GameView,UnityEditor")) //if game view is focused
					//UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.focusedWindow; //if scene view is focused
			#else
				return false;
			#endif
		}}

		public bool isSelected 
		{get{
			#if UNITY_EDITOR
				return UnityEditor.Selection.activeTransform == this.transform;
			#else
				return false;
			#endif
		}}
		#endregion


		public void OnEnable ()
		{
			#if UNITY_EDITOR
			//adding delegates
			UnityEditor.EditorApplication.update -= Update;	
			//UnityEditor.SceneView.onSceneGUIDelegate -= GetEditorCamPos; 
			
			if (isEditor) 
			{
				//UnityEditor.SceneView.onSceneGUIDelegate += GetEditorCamPos;
				UnityEditor.EditorApplication.update += Update;	
			}
			#endif

			//finding singleton instance
			instance = FindObjectOfType<MapMagic>();

			//checking terrains consistency
			terrains.CheckEmpty();
		}

		public void OnDisable ()
		{
			//removing delegates
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= Update;	
			//UnityEditor.SceneView.onSceneGUIDelegate -= GetEditorCamPos; 
			#endif
		}

		public Vector3[] GetCamPoses ()
		{
			
			//Vector3[] camPoses = null;

			if (isEditor) 
			{
				#if UNITY_EDITOR
				if (UnityEditor.SceneView.lastActiveSceneView==null) return new Vector3[0];
				if (camPoses==null || camPoses.Length!=1) camPoses = new Vector3[1];
				camPoses[0] = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position;
				#else
				camPoses = new Vector3[1];
				#endif
			}
			else if (useAllCameras) //playmode, multi-camera
			{
				Camera[] allCameras = Camera.allCameras;
				if (allCameras.Length == 0) { if (guiDebug) Debug.LogError("No Cameras to deploy MapMagic"); return new Vector3[0]; }
				if (camPoses==null || camPoses.Length!=allCameras.Length) camPoses = new Vector3[allCameras.Length];
				for (int c=0; c<camPoses.Length; c++) camPoses[c] = allCameras[c].transform.position;
			}
			else //playmode, default case - only main camera
			{
				Camera mainCam = Camera.main;
				if (mainCam==null) mainCam = FindObjectOfType<Camera>(); //in case it was destroyed or something
				if (mainCam==null) { if (guiDebug) Debug.LogError("No Main Camera to deploy MapMagic"); return new Vector3[0]; }
				if (camPoses==null || camPoses.Length!=1) camPoses = new Vector3[1];
				camPoses[0] = mainCam.transform.position;
			}

			//transforming cameras position to local
			for (int c=0; c<camPoses.Length; c++) camPoses[c] =  transform.InverseTransformPoint(camPoses[c]);
			
			return camPoses;		
		}

		public Coord[] GetCamCoords ()
		{
			Vector3[] camPoses = GetCamPoses();
			if (camCoords==null || camCoords.Length!=camPoses.Length) camCoords = new Coord[camPoses.Length];
			for (int c=0; c<camPoses.Length; c++)
				camCoords[c] = camPoses[c].FloorToCoord(terrainSize);
			return camCoords;
		}

		public void Update () 
		{ 
			//checking if instance already exists and disabling if it is another mm
			if (instance != null && instance != this) { Debug.LogError("MapMagic object already present in scene. Disabling duplicate"); this.enabled = false; return; }
		
			//loading old non-asset data
			if (gens == null)
			{
				if (serializer != null && serializer.entities != null && serializer.entities.Count != 0) 
				{	
					Debug.Log("MapMagic: Loading outdated scene format. Please check node consistency and re-save the scene.");
					LoadOldNonAssetData();
					serializer = null;
				}
				else Debug.Log("MapMagic: Could not find the proper graph data. Please assign it manually.");
			}
			
			//checking gens asset
			if (gens == null) gens = ScriptableObject.CreateInstance<GeneratorsAsset>();

			//finding camera positions
			Vector3[] camPoses = GetCamPoses();
			if (camPoses.Length==0) return;

			//displaying debug range
			if (guiDebug && !isEditor) 
				for (int c=0; c<camPoses.Length; c++) 
					transform.TransformPoint(camPoses[c]).DrawDebug(generateRange, Color.green);

			//deploying terrain matrix
			if (!isEditor && generateInfinite) terrains.Deploy(camPoses, allowMove:true);

			//enabling/disabling, switching lods, starting threads
			terrains.SwitchState(camPoses);

			//transforming cam poses to coords
			Coord[] camCoords = GetCamCoords();
			if (camCoords.Length==0) return;

			//calculating number of running threads and checking a need to prepare generators
			runningThreadsCount = 0;
			bool prepareGenerators = false;
			foreach (Chunk tw in terrains.Objects())
			{
				if (tw.running) runningThreadsCount++;
				if (tw.start) prepareGenerators = true; //if any of chunks started this frame
			}

			//preparing generators
			if (prepareGenerators)
			{
				foreach (TextureInput tin in gens.GeneratorsOfType<TextureInput>()) tin.CheckLoadTexture();
			}

			//updating chunks
			foreach (Chunk tw in terrains.ObjectsFromCoords(camCoords)) tw.Update();
		}

		public void OnDrawGizmos ()
		{
			float pixelSize = 1f * terrainSize / resolution;
			foreach (Chunk tw in terrains.Objects())
			{
				if (tw.previewObjs == null) continue;
				foreach (SpatialObject obj in tw.previewObjs)
				{
					Vector3 pos = new Vector3(obj.pos.x*pixelSize, obj.height*terrainHeight, obj.pos.y*pixelSize);

					Gizmos.DrawLine(new Vector3(pos.x, 0, pos.z), new Vector3(pos.x, terrainHeight, pos.z));
					Gizmos.DrawLine(pos+new Vector3(obj.size,0,0), pos-new Vector3(obj.size,0,0));
					Gizmos.DrawLine(pos+new Vector3(0,0,obj.size), pos-new Vector3(0,0,obj.size));

					Vector3 oldPoint = pos;
					foreach (Vector3 point in pos.CircleAround(obj.size, 12, true))
					{
						Gizmos.DrawLine(oldPoint,point);
						oldPoint = point;
					}
				}
			}
		}



		public void ForceGenerate ()
		{
			foreach (Chunk tw in terrains.Objects()) 
			{
				tw.stop = true;
				tw.results.Clear();
				tw.ready.Clear();
				//tw.applyHeight = false;
			}

			applyRunning = false;
			terrains.start=true; 
			Update(); 
		}




		#region Terrain Wrapper
		[System.Serializable]
		public class Chunk
		{
			public Coord coord;
			public Terrain terrain;
			public TransformPool[] pools;
			[System.NonSerialized] public Thread thread;
			
			public bool locked;
			public bool start;
			public bool stop; //start and stop at the same time will make the thread restart 
			public bool clear = true; //when generate has not been started
			public bool purge; //purge (when no output or output disabled) is needed after generate stage
			public bool running { get {return thread != null && thread.IsAlive; }}
			public bool complete { get {return (!start && !running && apply.Count==0 && !clear) || locked; }} //when apply complete and no changes to this terrain will be made infuture
			
			//public bool lod; //generate low detail mesh instead

			[System.NonSerialized] public HashSet<Generator> ready = new HashSet<Generator>();
			[System.NonSerialized] public Dictionary<Generator.Output, object> results = new Dictionary<Generator.Output, object>();
			[System.NonSerialized] public Dictionary<IOutput, object> apply = new Dictionary<IOutput, object>(); //apply queue 

			//defaults
			public Matrix defaultMatrix 
			{get{
				int res = instance.resolution; //lod? instance.lodResolution : instance.resolution;
				return new Matrix( new CoordRect(coord.x*res, coord.z*res, res, res) );;
			}}
			public SpatialHash defaultSpatialHash
			{get{
				SpatialHash spatialHash = new SpatialHash(new Vector2(coord.x*instance.resolution,coord.z*instance.resolution), instance.resolution, 16);
				return spatialHash;
			}}

			public object locker = new object();

			[System.NonSerialized] public SpatialHash previewObjs = null;

			//processing threads
			public void Update ()
			{
				//starting threads
				if (start)
				{		
					//calling before-gen event
					if (MapMagic.OnGenerateStarted != null) MapMagic.OnGenerateStarted(terrain);
								
					//generating
					if (!instance.multiThreaded) ThreadFn();
					else
					{
						//restarting thread if it is still alive
						if (running) stop=true;

						//if dead and thread limit not reached
						else if (instance.runningThreadsCount < instance.maxThreads)
						{ 
							thread = new Thread(ThreadFn);
							thread.IsBackground = true;
							//thread.Priority = System.Threading.ThreadPriority.BelowNormal;
							thread.Start(instance.gens);
							instance.runningThreadsCount++;
						}
					}
				}

				//sending terrain on apply
				if (!instance.applyRunning && apply.Count!=0  &&  !start && !stop && !running) //if apply not running and got something to apply. Do not apply when terrain stopped, restarted or still producing
				{
					//iteraing routine manually in editor (in one frame)
					if (instance.isEditor)
					{
						IEnumerator e = ApplyRoutine();
						while (e.MoveNext());
					}

					//starting routine in playmode
					else instance.StartCoroutine(ApplyRoutine());
				}

				
				//purging
				if (purge) Purge();
			}

			//generating
			public void ThreadFn ()
			{	
				if (locked) return;
				
				lock (locker) while (true)
				{
					stop=false;  //in case it was restarted
					start = false; 
					clear = false;

					GeneratorsAsset generators = MapMagic.instance.gens;

					try 
					{
						//checking connections validity
						//foreach (Generator outGen in generators.list)
						//	if (!outGen.ValidateConnectionsRecursive()) { Debug.Log("Error Validating Connections"); return; }
						
						//clearing ready state
						foreach (Generator outGen in generators.list)
						{
							//skipping disabled outputs
							if (!(outGen is IOutput)) continue;
							if (!outGen.enabled) continue;

							//checking and resetting ready state recursive for all outputs
							outGen.CheckClearRecursive(this);
						}
						
						//generating outputs
						lock (results) //to avoid access from apply if generate is quick
						{
							//generating
							foreach (Generator outGen in generators.list)
							{
								if (!(outGen is IOutput)) continue;
								if (!outGen.enabled) continue;
								if (stop) break; //exit foreach outGen
								
								outGen.GenerateRecursive(this); //excluding from queue on generateRecursive
							}
						}

						//generating preview generator   
						#if UNITY_EDITOR
						if (instance.previewGenerator!=null && instance.previewGenerator.enabled)
						{
							instance.previewGenerator.CheckClearRecursive(this);
							instance.previewGenerator.GenerateRecursive(this);
						}
						#endif
					}

					catch (System.Exception e) { Debug.LogError("Generate Thread Error:\n" + e); }

					//if (!stop) Thread.Sleep(2000);
					//exiting thread - only if it should not be restared
					purge = true;
					if (!start) break;
				}
			}

			// applying
			public IEnumerator ApplyRoutine ()
			{
				//calling before-apply event
				if (MapMagic.OnGenerateCompleted != null) MapMagic.OnGenerateCompleted(terrain);
				
				MapMagic.instance.applyRunning = true;

				//apply
				foreach (KeyValuePair<IOutput,object> kvp in apply)
				{
					IOutput output = kvp.Key;
					if (!(output as Generator).enabled) continue;
					if (output is SplatOutput && MapMagic.instance.gens.GetGenerator<PreviewOutput>()!=null) continue; //skip splat out if preview exists

					//callback
					if (OnApply!=null) OnApply(terrain, kvp.Value);

					//apply enumerator
					IEnumerator e = kvp.Key.Apply(this, terrain.terrainData);
					while (e.MoveNext()) 
					{				
						if (terrain==null) yield break; //guard in case max terrains count < actual terrains: terrain destroyed or still processing
						yield return null;
					}
				}

				//creating initial texture if splatmap count is 0 - just to look good
				if (terrain.terrainData.splatPrototypes.Length == 0) ClearSplats();

				//clearing intermediate results
				apply.Clear();
				if (!MapMagic.instance.isEditor || !MapMagic.instance.saveIntermediate) { results.Clear(); ready.Clear(); } //this should be done in thread, but thread has no access to isPlaying

				//if (!terrain.gameObject.activeSelf) terrain.gameObject.SetActive(true); //terrain.enabled = true;

				MapMagic.instance.applyRunning = false;

				//copy layer, tag, scripts from mm to terrains
				if (MapMagic.instance.copyLayersTags)
				{
					GameObject go = terrain.gameObject;
					go.layer = MapMagic.instance.gameObject.layer;
					go.isStatic = MapMagic.instance.gameObject.isStatic;
					go.tag = MapMagic.instance.gameObject.tag;
					//#if UNITY_EDITOR
					//UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.GameObjectUtility.GetStaticEditorFlags(MapMagic.instance.gameObject));
					//#endif
				}
				if (MapMagic.instance.copyComponents)
				{
					GameObject go = terrain.gameObject;
					MonoBehaviour[] components = MapMagic.instance.GetComponents<MonoBehaviour>();
					for (int i=0; i<components.Length; i++)
					{
						if (components[i] is MapMagic || components[i] == null) continue; //if MapMagic itself or script not assigned
						if (terrain.gameObject.GetComponent(components[i].GetType()) == null) Extensions.CopyComponent(components[i], go);
					}
				}

				//calling after-apply event
				if (MapMagic.OnApplyCompleted != null) MapMagic.OnApplyCompleted(terrain);
			}

			public void Purge ()
			{
				HeightOutput ho = MapMagic.instance.gens.GetGenerator<HeightOutput>(); if (ho==null || !ho.enabled) HeightOutput.Purge(this);
				
				SplatOutput so = MapMagic.instance.gens.GetGenerator<SplatOutput>();
				PreviewOutput po = MapMagic.instance.gens.GetGenerator<PreviewOutput>();
				if ( (po == null || !po.enabled) && (so==null || !so.enabled) ) SplatOutput.Purge(this);
				if (po == null || !po.enabled) previewObjs = null;
				
				GrassOutput go = MapMagic.instance.gens.GetGenerator<GrassOutput>(); if (go==null || !go.enabled) GrassOutput.Purge(this);
				ObjectOutput oo = MapMagic.instance.gens.GetGenerator<ObjectOutput>(); if (oo==null || !oo.enabled) ObjectOutput.Purge(this);
				TreesOutput to = MapMagic.instance.gens.GetGenerator<TreesOutput>(); if (to==null || !to.enabled) TreesOutput.Purge(this);
				purge = false;
			}


			public void SetSettings (MapMagic magic)
			{
				terrain.heightmapPixelError = magic.pixelError;
				terrain.basemapDistance = magic.baseMapDist;
				terrain.castShadows = magic.castShadows;

				if (magic.terrainMaterial != null) { terrain.materialTemplate=magic.terrainMaterial; terrain.materialType=Terrain.MaterialType.Custom; }
				else terrain.materialType=Terrain.MaterialType.BuiltInStandard;

				terrain.drawTreesAndFoliage = magic.detailDraw;
				terrain.detailObjectDistance = magic.detailDistance;
				terrain.detailObjectDensity = magic.detailDensity;
				terrain.treeDistance = magic.treeDistance;
				terrain.treeBillboardDistance = magic.treeBillboardStart;
				terrain.treeCrossFadeLength = magic.treeFadeLength;
				terrain.treeMaximumFullLODCount = magic.treeFullLod;

				terrain.terrainData.wavingGrassSpeed = magic.windSpeed;
				terrain.terrainData.wavingGrassAmount = magic.windSize;
				terrain.terrainData.wavingGrassStrength = magic.windBending;
				terrain.terrainData.wavingGrassTint = magic.grassTint;
			}

			public void ClearSplats () //same as SplatOutput.Clear
			{
				terrain.terrainData.splatPrototypes = new SplatPrototype[] { new SplatPrototype() { texture = Extensions.ColorTexture(2,2,new Color(0.5f, 0.5f, 0.5f, 0f)) } };

				float[,,] emptySplats = new float[16,16,1];
				for (int x=0; x<16; x++)
					for (int z=0; z<16; z++)
						emptySplats[z,x,0] = 1;

				terrain.terrainData.alphamapResolution = 16;
				terrain.terrainData.SetAlphamaps(0,0,emptySplats);
			}
		}

		#endregion

		#region Demo Limitation
		/*void OnLevelWasLoaded(int level) 
		{
			generateInfinite = false;
			terrains.Reset();
			generators = new GeneratorsList();
			UnityEditor.EditorUtility.DisplayDialog("MapMagic Demo Limitation",
				"You are using the Demo version of MapMagic plugin, which does not allow scene loading in playmode.", "OK");
		}*/
		#endregion

		#region Outdated
		[System.Serializable]
			public class GeneratorsList //one class is easier to serialize than multiple arrays
			{
				public Generator[] list = new Generator[0];
				public Generator[] outputs = new Generator[0];
			}

			public Serializer serializer = null;

			public void LoadOldNonAssetData ()
			{
				serializer.ClearLinks();
				GeneratorsList generators = new GeneratorsList();
				generators = (GeneratorsList)serializer.Retrieve(0);
				serializer.ClearLinks();

				gens = ScriptableObject.CreateInstance<GeneratorsAsset>();
				gens.list = generators.list;
				//gens.outputs = generators.outputs; 
			}
		#endregion

	}//class

}//namespace