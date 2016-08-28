using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;


namespace MapMagic
{
	public abstract class ObjectGrid<T> : ISerializationCallbackReceiver where T : class
	{
		public Dictionary<int,T> grid = new Dictionary<int,T>();
		public HashSet<int> nailedHashes = new HashSet<int>();
		public int stockMargin = 1;
//public int maxCount = 0; //remove me!

		//public CoordRect lastRect; public Coord lastCenter; //to check change and re-deploy on unnail
		public bool initialized { get{ return grid!=null; } }
		
		public abstract T Construct ();
		public abstract void OnCreate (T obj, Coord coord);
		public abstract void OnMove (T obj, Coord newCoord);
		public abstract void OnRemove (T obj);

		#region Serialization
		[SerializeField] private T[] serializedObjects = new T[0];
		[SerializeField] private int[] serializedHashes = new int[0];
		[SerializeField] private int[] serializedNailedHashes = new int[0];

		public virtual void OnBeforeSerialize () 
		{
			if (serializedObjects.Length != grid.Count) { serializedObjects=new T[grid.Count]; serializedHashes=new int[grid.Count]; }
			
			int counter = 0;
			foreach (KeyValuePair<int,T> kvp in grid)
			{
				serializedObjects[counter] = kvp.Value;
				serializedHashes[counter] = kvp.Key;
				counter++;
			}

			counter = 0;
			if (serializedNailedHashes.Length != nailedHashes.Count) serializedNailedHashes = new int[nailedHashes.Count];
			foreach (int h in nailedHashes)
			{
				serializedNailedHashes[counter] = h;
				counter++;
			}
		}
		public virtual void OnAfterDeserialize () 
		{ 
			grid = new Dictionary<int,T>();
			for (int i=0; i<serializedObjects.Length; i++) grid.Add(serializedHashes[i], serializedObjects[i]);

			nailedHashes = new HashSet<int>();
			for (int i=0; i<serializedNailedHashes.Length; i++) nailedHashes.Add(serializedNailedHashes[i]);
		}
		#endregion


		public T this[int x, int z] { get{return this[new Coord(x,z)];} }//set{this[new Coord(x,z)]=value;} }
		public T this[Coord c] 
		{
			get { int i = c.ToInt(); if (grid.ContainsKey(i)) return grid[i]; else return null; }
			//set {  }
		}

		public void Nail (Coord coord)
		{
			int hash = coord.ToInt();
		
			//looking if already nailed
			if (nailedHashes.Contains(hash)) return;

			//finding object in grid
			T obj = null;
			if (grid.ContainsKey(hash)) obj = grid[hash];

			//creating object if it was not found
			bool isObjNew = obj==null;
			if (isObjNew) obj = Construct();

			//saving obj to serialized nailed
			nailedHashes.Add(hash);
			if (!grid.ContainsKey(hash)) grid.Add(coord.ToInt(), obj);

			//calling onCreate if obj was just created
			if (isObjNew) OnCreate(obj, coord);
		}

		public void Unnail (Coord coord, bool remove=true)
		{
			int hash = coord.ToInt();

			if (!nailedHashes.Contains(hash)) return; //looking if it was ever nailed
			nailedHashes.Remove(hash);

			if (remove && grid.ContainsKey(hash)) 
			{
				T obj = grid[hash];
				if (obj!=null) OnRemove(obj);
				grid.Remove(hash);
			}
		}

		/*public virtual void Reset (bool reDeploy=false) 
		{
			foreach(KeyValuePair<int,T> kvp in grid)
				if (kvp.Value != null) OnRemove(kvp.Value);
			grid = new Dictionary<int,T>();

			HashSet<int> oldNailedHashes = nailedHashes;
			nailedHashes = new HashSet<int>();

			if (reDeploy)
			{
				foreach (int nailedHash in oldNailedHashes) Nail(nailedHash.ToCoord());
				Deploy(lastRect, lastCenter, force: true);
			}
		}*/
		public IEnumerable<T> NailedObjects () { foreach(int h in nailedHashes) yield return grid[h]; }
		public IEnumerable<T> Objects () { foreach(KeyValuePair<int,T> kvp in grid) yield return kvp.Value; }
		public IEnumerable<Coord> Coords () { foreach(KeyValuePair<int,T> kvp in grid) yield return kvp.Key.ToCoord(); }
		public IEnumerable<T> ObjectsFromCoord (Coord coord) 
		{ 
			int counter = 0;
			foreach (Coord c in coord.DistanceArea(20000)) //to infinity
			{
				int hash = c.ToInt();
				if (grid.ContainsKey(hash)) 
				{ 
					yield return grid[hash];
					counter++;	
				}
				if (counter >= grid.Count) break;
			}
		}
		public IEnumerable<T> ObjectsFromCoords (Coord[] coords) 
		{ 
			int counter = 0;
			foreach (Coord c in Coord.MultiDistanceArea(coords,20000)) //to infinity
			{
				int hash = c.ToInt();
				if (grid.ContainsKey(hash)) 
				{ 
					yield return grid[hash];
					counter++;	
				}
				if (counter >= grid.Count) break;
			}
		}
		public T GetClosestObj (Coord coord) { if (grid.Count==0) return null; foreach (T obj in ObjectsFromCoord(coord)) return obj; return null; } 


		public virtual void Deploy (CoordRect rect, bool allowMove=true) { Deploy(rect, rect.Center, allowMove); }
		public virtual void Deploy (CoordRect rect, Coord center, bool allowMove=true)
		{
			Dictionary<int,T> newGrid = new Dictionary<int,T>();

			//adding nailed objs
			foreach (int hash in nailedHashes)
			{
				if (!grid.ContainsKey(hash)) Debug.Log("Could not find nailed object");
				
				T obj = grid[hash];
				if (obj!=null) newGrid.Add(hash,obj);
				grid.Remove(hash);
			}

			//adding objects within stock rect
			Coord min = rect.Min-stockMargin; Coord max = rect.Max+stockMargin;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				Coord coord = new Coord(x,z);
				int hash = coord.ToInt();

				if (grid.ContainsKey(hash))
				{
					T obj = grid[hash];
					if (obj!=null) newGrid.Add(hash,obj);
					grid.Remove(hash);
				}
			}

			//filling empty areas with unused (or new) objects
			foreach (Coord c in center.DistanceArea(rect))
			{
				int hash = c.ToInt();
				if (newGrid.ContainsKey(hash)) continue;

				T obj = null;
				if (grid.Count != 0 && allowMove)
				{
					obj = grid.First().Value;
					OnMove(obj, c);
					grid.Remove(grid.First().Key);
				}

				else 
				{
					obj = Construct();
					OnCreate(obj, c);
				}

				newGrid.Add(hash,obj);
			}

			//removing all other objs left
			foreach(KeyValuePair<int,T> kvp in grid) OnRemove(kvp.Value);

			grid = newGrid;
		}

		public virtual void Deploy (CoordRect[] rects, Coord[] centers, bool allowMove=true)
		{
			Dictionary<int,T> newGrid = new Dictionary<int,T>();

			//adding nailed objs
			foreach (int hash in nailedHashes)
			{
				if (!grid.ContainsKey(hash)) Debug.Log("Could not find nailed object");
				
				T obj = grid[hash];
				if (obj!=null) newGrid.Add(hash,obj);
				grid.Remove(hash);
			}

			//adding objects within rect
			for (int r=0; r<centers.Length; r++)
			{
				CoordRect rect = rects[r];
				Coord min = rect.Min; Coord max = rect.Max;
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
				{
					Coord coord = new Coord(x,z);
					int hash = coord.ToInt();

					if (grid.ContainsKey(hash))
					{
						T obj = grid[hash];
						if (obj!=null) newGrid.Add(hash,obj);
						grid.Remove(hash);
					}
				}
			}

			//creating an unused stack - sorted by distance
			Stack<T> unused = new Stack<T>();
			Stack<int> unusedHashes = new Stack<int>();
			if (grid.Count != 0) foreach (Coord c in Coord.MultiDistanceArea(centers, 20000000))
			{
				int hash = c.ToInt();
				if (grid.ContainsKey(hash))
				{
					if (grid[hash] != null)
						{ unused.Push(grid[hash]); unusedHashes.Push(hash); }
					grid.Remove(hash);
					if (grid.Count == 0) break;
				}	
			}


			//filling empty areas with unused (or new) objects
			for (int r=0; r<centers.Length; r++)
			{
				CoordRect rect = rects[r];
				Coord center = centers[r];

				foreach (Coord c in center.DistanceArea(rect))
				{
					int hash = c.ToInt();
					if (newGrid.ContainsKey(hash)) continue;

					T obj = null;
					if (unused.Count != 0 && allowMove)
					{
						obj = unused.Pop(); unusedHashes.Pop(); //popping the furtherest unused
						OnMove(obj, c);
					}

					else 
					{
						obj = Construct();
						OnCreate(obj, c);
					}

					newGrid.Add(hash,obj);
				}
			}

			//adding other unused to trail or removing them
			while (unused.Count > 0)
			{
				T obj = unused.Pop();
				int hash = unusedHashes.Pop();

				//if (!allowMove) OnRemove(obj);
				//else 
				newGrid.Add(hash,obj); 
			}

			//removing all other objs left
			//foreach(KeyValuePair<int,T> kvp in grid) OnRemove(kvp.Value);

			grid = newGrid;
		}
	}


	[System.Serializable]
	public class GameObjectGrid : ObjectGrid<GameObject>
	{
		public Vector3 objectSize; //in world units
		//public Vector3 centerOffset; //object pivot position in world units. For terrains and voxeland chunks = 0, for objs with central pivot = objSize/2
		public Transform parent;

		public CoordRect[] prevRects = new CoordRect[0];
		public CoordRect[] currRects = new CoordRect[0];
		public Coord[] currCenters = new Coord[0];

		public override GameObject Construct () { return new GameObject(); }

		public override void OnCreate (GameObject obj, Coord coord)
		{
			obj.name = "Chunk " + coord.x + "," + coord.z;
			obj.transform.parent = parent;
			obj.transform.localPosition = CoordToPos(coord);
		}

		public override void OnMove (GameObject obj, Coord newCoord) 
		{
			obj.transform.localPosition = CoordToPos(newCoord);
		}

		public override void OnRemove (GameObject obj) 
		{ 
			GameObject.DestroyImmediate(obj);
		}


		public Coord PosToCoord (Vector3 pos, bool ceil=false)  
		{ 
			if (!ceil) return new Coord(
				Mathf.FloorToInt((pos.x) / objectSize.x),
				Mathf.FloorToInt((pos.z) / objectSize.z) ); 
			else return new Coord(
				Mathf.CeilToInt((pos.x) / objectSize.x),
				Mathf.CeilToInt((pos.z) / objectSize.z) ); 
		}
		public Vector3 CoordToPos (Coord coord) { return new Vector3( coord.x*objectSize.x, 0, coord.z*objectSize.z ); }

		public Coord GetCoord (GameObject obj) { return PosToCoord(obj.transform.position + objectSize/2); }
		public Vector3 GetCenter (GameObject obj) { return obj.transform.position + objectSize/2; }



		public void Deploy (Vector3 pos, float range) { Deploy( new Vector3[] {pos}, range); }

		public void Deploy (Vector3[] poses, float range)
		{
			bool reDeploy = false;

			//checking guard arrays
			if (prevRects == null || prevRects.Length != poses.Length) 
			{ 
				reDeploy = true; 
				prevRects = new CoordRect[poses.Length]; currRects = new CoordRect[poses.Length]; currCenters = new Coord[poses.Length]; 
			}

			for (int p=0; p<poses.Length; p++)
			{
				Vector3 pos = poses[p];

				//finding rect and center
				currCenters[p]	= new Coord( Mathf.RoundToInt(pos.x / objectSize.x),		Mathf.RoundToInt(pos.z / objectSize.z)  );
				Coord min		= new Coord( Mathf.FloorToInt((pos.x-range)/objectSize.x),	Mathf.FloorToInt((pos.z-range)/objectSize.z)  );
				Coord max		= new Coord( Mathf.FloorToInt((pos.x+range)/objectSize.x),	Mathf.FloorToInt((pos.z+range)/objectSize.z) )  +  1;
				currRects[p]	= new CoordRect(min, max-min);

				//checking a need to re-deploy
				if (currRects[p] != prevRects[p]) reDeploy = true;
				prevRects[p] = currRects[p];
			}

			if (reDeploy)
			{Debug.Log("Redeploy");
			base.Deploy(currRects, currCenters);}
		}
	}

}
