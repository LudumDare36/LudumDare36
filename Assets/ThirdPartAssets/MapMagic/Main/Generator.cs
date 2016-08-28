
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using Plugins;

namespace MapMagic 
{
	public class GeneratorMenuAttribute : System.Attribute
	{
		public string menu { get; set; }
		public string name { get; set; }
		public bool disengageable { get; set; }
		public bool disabled { get; set; }
		public int priority { get; set; }
	}

	[System.Serializable]
	public abstract class Generator
	{
		#region Inout

			public enum InoutType { Map, Objects, Spline }

			public interface IGuiInout
			{
				Rect guiRect { get; set; }
				string guiName { get; set; }
				Color guiColor { get; }
				Vector2 guiConnectionPos { get; }

				void DrawIcon (Layout layout, bool drawLabel);
			}

			public class Input : IGuiInout
			{
				public InoutType type;
				public bool write = false;
				public bool mandatory = false;
				
				//linking
				public Output link; //{get;}
				public Generator linkGen; //{get;}

				//gui
				public Rect guiRect { get; set; }
				public string guiName { get; set; }
				public Color guiColor 
				{get{
					bool isProSkin = false;
					#if UNITY_EDITOR
					if (UnityEditor.EditorGUIUtility.isProSkin) isProSkin = true;
					#endif

					switch (type)
					{
						case InoutType.Map: return isProSkin? new Color(0.23f, 0.5f, 0.652f) : new Color(0.05f, 0.2f, 0.35f);
						case InoutType.Objects: return isProSkin? new Color(0.15f, 0.6f, 0.15f) : new Color(0.1f, 0.4f, 0.1f);
						default: return Color.black; 
					}
				}}
				public Vector2 guiConnectionPos {get{ return new Vector2(guiRect.xMin, guiRect.center.y); }}
				public string guiObjId { get; set; }
				public void DrawIcon (Layout layout, bool drawLabel=true)
				{ 
					string textureName = "";
					switch (type) 
					{ 
						case InoutType.Map: textureName = "MapMagicMatrix"; break;
						case InoutType.Objects: textureName = "MapMagicScatter"; break;
						case InoutType.Spline: textureName = "MapMagicSpline"; break;
					}

					guiRect = new Rect(layout.field.x-5, layout.cursor.y+layout.field.y, 18,18);
					layout.Icon(textureName,guiRect);

					if (drawLabel)
					{
						Rect nameRect = guiRect;
						nameRect.width = 100; nameRect.x += guiRect.width + 2;
						layout.Label(guiName, nameRect,  fontSize:10);
					}

					if (mandatory && linkGen==null) 
						layout.Icon("MapMagic_Mandatory", new Rect (guiRect.x+10+2, guiRect.y-2, 8,8));
				}


				public Input () {} //default constructor to create with activator
				public Input (string n, InoutType t, bool write=false, bool mandatory=false) { guiName = n; type = t; this.write = write; this.mandatory = mandatory; }
				
				public Generator GetGenerator (Generator[] gens) 
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Input input in gens[g].Inputs())
							if (input == this) return gens[g];
					return null;
				}
				public void Link (Output output, Generator outputGen) { 
				link = output; linkGen = outputGen; }
				public void Unlink () { link = null; linkGen = null; }

				public object GetObject (MapMagic.Chunk tw) { return GetObject(tw, write:write); }
				public object GetObject (MapMagic.Chunk tw, bool write)
				{ 
					if (link == null) return null;
					if (!tw.results.ContainsKey(link)) return null;
					return tw.results[link];
				}
			}

			public class Output : IGuiInout
			{
				public InoutType type;
				
				//gui
				public Rect guiRect { get; set; }
				public string guiName { get; set; }
				public Color guiColor { get; set; }
				public Vector2 guiConnectionPos {get{ return new Vector2(guiRect.xMax, guiRect.center.y); }}
				public void DrawIcon (Layout layout, bool drawLabel=true) 
				{ 
					string textureName = "";
					switch (type) 
					{ 
						case InoutType.Map: textureName = "MapMagicMatrix"; break;
						case InoutType.Objects: textureName = "MapMagicScatter"; break;
						case InoutType.Spline: textureName = "MapMagicSpline"; break;
					}

					guiRect = new Rect(layout.field.x+layout.field.width-18+5, layout.cursor.y+layout.field.y, 18,18);

					if (drawLabel)
					{
						Rect nameRect = guiRect;
						nameRect.width = 100; nameRect.x-= 103;
						layout.Label(guiName, nameRect, textAnchor:TextAnchor.LowerRight, fontSize:10);
					}

					layout.Icon(textureName, guiRect); //detail:resolution.ToString());

					//drawing obj id
					if (MapMagic.instance.guiDebug)
					{
						Rect idRect = guiRect;
						idRect.width = 100; idRect.x += 25;
						MapMagic.Chunk closest = MapMagic.instance.terrains.GetClosestObj(new Coord(0,0));
						if (closest != null)
						{
							object obj = closest.results.CheckGet(this);
							layout.Label(obj!=null? obj.GetHashCode().ToString() : "null", idRect, textAnchor:TextAnchor.LowerLeft);
						}
					}
				}

				public Output () {} //default constructor to create with activator
				public Output (string n, InoutType t) { guiName = n; type = t; }

				public Generator GetGenerator (Generator[] gens) 
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Output output in gens[g].Outputs())
							if (output == this) return gens[g];
					return null;
				}

				public Input GetConnectedInput (Generator[] gens)
				{
					for (int g=0; g<gens.Length; g++) 
						foreach (Input input in gens[g].Inputs())
							if (input.link == this) return input;
					return null;
				}

				public void SetObject (MapMagic.Chunk terrain, object obj) //TODO: maybe better replace with CheckAdd
				{
					if (terrain.results.ContainsKey(this))
					{
						if (obj == null) terrain.results.Remove(this);
						else terrain.results[this] = obj;
					}
					else
					{
						if (obj != null) terrain.results.Add(this, obj);
					}
				}
			}
		#endregion

		public bool enabled = true;

		//gui
		[System.NonSerialized] public Layout layout = new Layout();
		public Rect guiRect; //just for serialization
		[System.NonSerialized] public System.Diagnostics.Stopwatch timer = null; //debug timer

		public virtual void Move (Vector2 delta, bool moveChildren=true) 
		{
			layout.field.position += delta;
			guiRect = layout.field;

			//moving inouts to remove lag
			foreach (Generator.IGuiInout inout in Inouts()) 
				inout.guiRect = new Rect(inout.guiRect.position+delta, inout.guiRect.size);
		}

		//inputs and outputs
		public virtual IEnumerable<Output> Outputs() { yield break; }
		public virtual IEnumerable<Input> Inputs() { yield break; }
		public IEnumerable<IGuiInout> Inouts() 
		{ 
			foreach (Output i in Outputs()) yield return i; 
			foreach (Input i in Inputs()) yield return i;
		}

		//connection states
		public static bool CanConnect (Output output, Input input) { return output.type == input.type; } //temporary out of order, before implementing resolutions

		/*public bool ValidateConnectionsRecursive ()
		{
			foreach (Input input in Inputs())
			{
				if (input.link != null)  
				{ 
					if (!CanConnect(input.link, input) || 
						input.linkGen == this ||
						!input.linkGen.ValidateConnectionsRecursive()) return false; 
				}
				else if (input.mandatory) return false;
			}
			return true;
		}*/

		public bool IsDependentFrom (Generator prior)
		{
			foreach (Input input in Inputs())
			{
				if (input==null || input.linkGen==null) continue;
				if (prior == input.linkGen) return true;
				if (input.linkGen.IsDependentFrom(prior)) return true;
			}
			return false;
		}

		public void CheckClearRecursive (MapMagic.Chunk tw) //checks if prior generators were clearied, and if they were - clearing this one
		{
			//if (!tw.ready.Contains(this)) //TODO: optimize here
			foreach (Input input in Inputs())
			{
				if (input.linkGen==null) continue;

				//recursive first
				input.linkGen.CheckClearRecursive(tw);

				//checking if clear
				if (!tw.ready.Contains(input.linkGen))
				{
					if (tw.ready.Contains(this)) tw.ready.Remove(this);
					//break; do not break, go on checking in case of branching-then-connecting
				}
			}
		}

		public void GenerateRecursive (MapMagic.Chunk tw)
		{
			//generating input generators
			foreach (Input input in Inputs())
			{
				if (input.linkGen==null) continue;
				if (tw.stop) return; //before entry stop
				input.linkGen.GenerateRecursive(tw);
			}

			if (tw.stop) return; //before generate stop for time economy

			//generating this
			if (!tw.ready.Contains(this))
			{
				//starting timer
				if (MapMagic.instance.guiDebug)
				{
					if (timer==null) timer = new System.Diagnostics.Stopwatch(); 
					else timer.Reset();
					timer.Start();
				}

				Generate(tw);
				if (!tw.stop) tw.ready.Add(this);

				//stopping timer
				if (timer != null) timer.Stop();
			}
		}

		public abstract void Generate (MapMagic.Chunk terrain);

		//gui
		public abstract void OnGUI ();
		public void OnGUIBase()
		{
			//drawing background
			layout.Element("MapMagic_Window", layout.field, new RectOffset(34,34,34,34), new RectOffset(33,33,33,33));

			//resetting layout
			layout.field.height = 0;
			layout.field.width =160;
			layout.cursor = new Rect();
			layout.change = false;
			layout.margin = 1; layout.rightMargin = 1;
			layout.fieldSize = 0.4f;              

			//drawing window header
			layout.Icon("MapMagic_Window_Header", new Rect(layout.field.x, layout.field.y, layout.field.width, 16));

			//drawing eye icon
			//if (layout.zoom > 0.5f)
			//{
				layout.Par(14); layout.Inset(2);
				Rect eyeRect = layout.Inset(18);
				GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(GetType(), typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;

				if (attribute != null && attribute.disengageable) 
					layout.Toggle(ref enabled, rect:eyeRect, onIcon:"MapMagic_GeneratorEnabled", offIcon:"MapMagic_GeneratorDisabled");
				else layout.Icon("MapMagic_GeneratorAlwaysOn", eyeRect, Layout.IconAligment.center, Layout.IconAligment.center);
			//}

			//drawing label
			string genName = attribute==null? "Unknown" : attribute.name;

			bool generated = true;
			foreach (MapMagic.Chunk tw in MapMagic.instance.terrains.Objects())
				if (!tw.ready.Contains(this)) generated = false;
			if (!generated) genName+="*";
			
			Rect labelRect = layout.Inset(); labelRect.height = 25; labelRect.y -= (1f-layout.zoom)*6 + 2;
			layout.Label(genName, labelRect, fontStyle:FontStyle.Bold, fontSize:19-layout.zoom*8);

			layout.Par(1);

			//gen params
			layout.Par(3);
			if (!MapMagic.instance.guiDebug)
			{
				try {OnGUI();}
				catch (System.Exception e) 
					{if (e is System.ArgumentOutOfRangeException || e is System.NullReferenceException) Debug.LogError("Error drawing generator " + GetType() + "\n" + e);}
			}
			else OnGUI();
			layout.Par(3);

			//drawing debug generate time
			if (MapMagic.instance.guiDebug && timer != null)
			{
				Rect timerRect = new Rect(layout.field.x, layout.field.y+layout.field.height, 200, 20);
				layout.Label(timer.ElapsedMilliseconds + "ms", timerRect);
				//EditorGUI.LabelField(gen.layout.ToLocal(timerRect), gen.timer.ElapsedMilliseconds + "ms");
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="", name ="Group", priority = 1)]
	public class Group : Generator
	{
		public string name = "Group";
		public string comment = "Drag in generators to group them";
		public bool locked;

		[System.NonSerialized] public List<Generator> generators = new List<Generator>();

		public override void Generate (MapMagic.Chunk chunk) {}

		public override void OnGUI () 
		{
			//initializing layout
			layout.cursor = new Rect();
			layout.change = false;

			//drawing background
			layout.Element("MapMagic_Group", layout.field, new RectOffset(16,16,16,16), new RectOffset(0,0,0,0));

			//lock sign
			/*Rect lockRect = new Rect(guiRect.x+guiRect.width-14-6, field.y+6, 14, 12); 
			layout.Icon(locked? "MapMagic_LockLocked":"MapMagic_LockUnlocked", lockRect, verticalAlign:Layout.IconAligment.center);
			bool wasLocked = locked;
			#if UNITY_EDITOR
			locked = UnityEditor.EditorGUI.Toggle(layout.ToDisplay(lockRect.Extend(3)), locked, GUIStyle.none);
			#endif
			if (locked && !wasLocked) LockContents();
			if (!locked && wasLocked) UnlockContents();*/

			//name and comment
			layout.margin = 5;
			layout.CheckStyles();
			float nameWidth = layout.boldLabelStyle.CalcSize( new GUIContent(name) ).x * 1.1f / layout.zoom + 10f;
			float commentWidth = layout.labelStyle.CalcSize( new GUIContent(comment) ).x / layout.zoom + 10;
			nameWidth = Mathf.Min(nameWidth,guiRect.width-5); commentWidth = Mathf.Min(commentWidth, guiRect.width-5);

			if (!locked)
			{
				layout.fontSize = 13; layout.Par(22); name = layout.Field(name, rect:layout.Inset(nameWidth), useEvent:true, style:layout.boldLabelStyle); 
				layout.fontSize = 11; layout.Par(18); comment = layout.Field(comment, rect:layout.Inset(commentWidth), useEvent:true, style:layout.labelStyle);
			}
			else
			{
				layout.fontSize = 13; layout.Par(22); layout.Label(name, rect:layout.Inset(nameWidth), fontStyle:FontStyle.Bold); 
				layout.fontSize = 11; layout.Par(18); layout.Label(comment, rect:layout.Inset(commentWidth)); 
			}
		}

		public void Populate ()
		{
			generators.Clear();

			for (int i=0; i<MapMagic.instance.gens.list.Length; i++)
			{
				Generator gen = MapMagic.instance.gens.list[i];
				if (layout.field.Contains(gen.layout.field)) generators.Add(gen); 
			}
		}

		public override void Move (Vector2 delta, bool moveChildren=true)
		{
			base.Move(delta,true);
			if (moveChildren) for (int g=0; g<generators.Count; g++) generators[g].Move(delta,false);
		}


	}




}