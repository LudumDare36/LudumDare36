using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace MapMagic
{
	public class MapMagicWindow : EditorWindow
	{
		private Layout layout;
		private Layout toolbarLayout;

		private GUIStyle generatorWindowStyle;
		private GUIStyle groupWindowStyle;

		#region Undo

			void PerformUndo ()
			{
				Repaint(); //just to make curve undo work.
				//modifying curve with a layout writes undo the usual way, and it is displayed in undo stack as "MapMaic Generator Change"
				//but somehow GetCurrentGroupName returns the previous action instead, like "Selection Change"
			

				if (!Undo.GetCurrentGroupName().Contains("MapMagic")) return;

				foreach (MapMagic.Chunk tw in MapMagic.instance.terrains.Objects()) tw.results.Clear();
				MapMagic.instance.gens.ChangeGenerator(null);
				if (MapMagic.instance.instantGenerate) { MapMagic.instance.ForceGenerate(); }

				Repaint();
			}
		#endregion

		#region Right-click actions
	
			void CreateGenerator (System.Type type, Vector2 guiPos)
			{
				/*//System.Type type = (System.Type) boxedType;
				Generator newGen = (Generator)System.Activator.CreateInstance(type);
				Extensions.ArrayAdd(ref script.generators, newGen);
 
				newGen.guiRect.x = guiPos.x - newGen.guiRect.width/2;
				newGen.guiRect.y = guiPos.y - 10;*/

				Undo.RecordObject (MapMagic.instance.gens, "MapMagic Create Generator");
				MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;

				Generator gen = MapMagic.instance.gens.CreateGenerator(type, guiPos);
				MapMagic.instance.gens.ChangeGenerator(gen);

				repaint=true; Repaint(); 

				EditorUtility.SetDirty(MapMagic.instance.gens);
			}

			void DeleteGenerator (Generator gen)
			{
				/*//unlinking and removing it's reference in inputs and outputs
				for (int g=0; g<script.generators.Length; g++)
					foreach (Generator.Input input in script.generators[g].Inputs())
						if (input.linkGen == gen) input.Unlink();

				Extensions.ArrayRemove(ref script.generators, gen);*/
				Undo.RecordObject (MapMagic.instance.gens, "MapMagic Delete Generator"); 
				MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;
				
				MapMagic.instance.gens.DeleteGenerator(gen);
				MapMagic.instance.gens.ChangeGenerator(null);
				Repaint();

				EditorUtility.SetDirty(MapMagic.instance.gens);
			}

			void PreviewOutput (Generator gen, Generator.Output output, bool inWindow)
			{
				PreviewOutput previewOutput = MapMagic.instance.gens.GetGenerator<PreviewOutput>();
				
				//removing an old one preview generator
				if (previewOutput != null)
					MapMagic.instance.gens.DeleteGenerator(previewOutput);

				//do nothing else if it is a 'clear' command (gen is null)
				if (gen==null || output==null) { PreviewWindow.CloseWindow(); MapMagic.instance.gens.ChangeGenerator(null); return; }
				
				//creating new preview and connecting it
				previewOutput = (PreviewOutput)MapMagic.instance.gens.CreateGenerator(typeof(PreviewOutput), gen.guiRect.position + new Vector2(200,10));
				previewOutput.input.Link(output, gen);

				if (inWindow)
				{
					previewOutput.inWindow = true;
					PreviewWindow.ShowWindow();
				}
				
				else previewOutput.onTerrain = true;

				MapMagic.instance.gens.ChangeGenerator(previewOutput);

				/*if (gen==null) 
				{ 
					MapMagic.instance.previewInWindow=false; MapMagic.instance.previewOnTerrain=false; 
					MapMagic.instance.previewGenerator=null; MapMagic.instance.previewOutput=null;

					if (MapMagic.instance.generators.GetOutput<SplatOutput>() != null) OnGeneratorChanged(MapMagic.instance.generators.GetOutput<SplatOutput>());
					else 
						foreach (MapMagic.Chunk chunk in MapMagic.instance.terrains.Objects())
							chunk.terrain.terrainData.splatPrototypes = new SplatPrototype[] { new SplatPrototype() { texture = Extensions.ColorTexture(2,2,new Color(0.5f, 0.5f, 0.5f, 0f)) } };

					return; 
				}
				
				RecordGeneratorUndo();
				MapMagic.instance.previewGenerator = gen;
				MapMagic.instance.previewOutput = previewOutput;
				
				if (inWindow) { MapMagic.instance.previewInWindow = true; PreviewWindow.ShowWindow(); }
				else { MapMagic.instance.previewOnTerrain = true; }

				OnGeneratorChanged(gen);*/
			} 

			void ResetGenerator (Generator gen)
			{
				Undo.RecordObject (MapMagic.instance.gens, "MapMagic Reset Generator"); 
				MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;

				gen.ReflectionReset();
				MapMagic.instance.gens.ChangeGenerator(gen);

				EditorUtility.SetDirty(MapMagic.instance.gens);
			}

			private GeneratorsAsset SmartCopyGenerator (Generator gen)
			{
				GeneratorsAsset copyGens = ScriptableObject.CreateInstance<GeneratorsAsset>();

				//saving all gens if clicked to background
				if (gen == null) 
				{
					copyGens.list = (Generator[])Serializer.DeepCopy(MapMagic.instance.gens.list);
				}

				//saving group
				else if (gen is Group)
				{
					Group copyGroup = (Group)Serializer.DeepCopy(gen);
					copyGens.UnlinkGenerator(copyGroup, unlinkGroup: true);
					Generator[] copyAllGens= (Generator[])Serializer.DeepCopy(MapMagic.instance.gens.list);
					copyGens.list = new Generator[] { copyGroup };

					for (int i=0; i<copyAllGens.Length; i++)
					{
						if (!copyGroup.guiRect.Contains(copyAllGens[i].guiRect)) continue;
						
						//un-linking copy gens
						foreach (Generator.Input input in copyAllGens[i].Inputs())
						{
							if (input.link == null) continue;
							if (!copyGroup.guiRect.Contains(input.linkGen.guiRect)) input.Unlink();
						}
						
						ArrayUtility.Add(ref copyGens.list, copyAllGens[i]);
					}
				}

				//saving single generator
				else
				{
					Generator copyGen = (Generator)Serializer.DeepCopy(gen);
					copyGens.UnlinkGenerator(copyGen);
					copyGens.list = new Generator[] { copyGen };
				}

				return copyGens;
			}

			void DuplicateGenerator (Generator gen)
			{
				GeneratorsAsset copyGens = SmartCopyGenerator(gen);
				
				//ignoring already existing outputs
				for (int i=copyGens.list.Length-1; i>=0; i--)
				{
					Generator copyGen = copyGens.list[i];

					if (copyGen is IOutput && MapMagic.instance.gens.GetGenerator(gen.GetType())!=null)
					{
						Debug.Log ("MapMagic: tried to copy Output which already exists (" + copyGen + "). Skipping.");
						copyGens.UnlinkGenerator(copyGen);
						ArrayUtility.RemoveAt(ref copyGens.list, i);
					}

					copyGen.guiRect.position += new Vector2(0, gen.guiRect.height + 10);
				}

				ArrayUtility.AddRange(ref MapMagic.instance.gens.list, copyGens.list);
				MapMagic.instance.gens.ChangeGenerator(null);
				repaint=true; Repaint();
			}

			void SaveGenerator (Generator gen, Vector2 pos)
			{
				string path= UnityEditor.EditorUtility.SaveFilePanel(
						"Save Node as Unity Asset",
						"Assets",
						"MapMagicNode.asset", 
						"asset");
				if (path==null || path.Length==0) return;
				path = path.Replace(Application.dataPath, "Assets");

				GeneratorsAsset saveGens = SmartCopyGenerator(gen);
				if (gen != null) for (int i=0; i<saveGens.list.Length; i++) saveGens.list[i].guiRect.position -= gen.guiRect.position;

				AssetDatabase.CreateAsset(saveGens, path);
				AssetDatabase.SaveAssets();
			}

			void LoadGenerator (Vector2 pos)
			{
				string path= UnityEditor.EditorUtility.OpenFilePanel(
						"Load Node",
						"Assets",
						"asset");
				if (path==null || path.Length==0) return;
				path = path.Replace(Application.dataPath, "Assets");

				GeneratorsAsset loadedGens = (GeneratorsAsset)AssetDatabase.LoadAssetAtPath(path, typeof(GeneratorsAsset));

				for (int i=loadedGens.list.Length-1; i>=0; i--)
				{
					//cloning
					//loadedGens.list[i] = loadedGens.list[i].ReflectionCopy();
					Generator gen = loadedGens.list[i];

					//offset
					gen.guiRect.position += pos;
					
					//ignoring already existing outputs
					if (gen is IOutput && MapMagic.instance.gens.GetGenerator(gen.GetType())!=null)
					{
						Debug.Log ("MapMagic: tried to load Output which already exists (" + gen + "). Skipping.");
						loadedGens.UnlinkGenerator(gen);
						ArrayUtility.RemoveAt(ref loadedGens.list, i);
					}

				}

				ArrayUtility.AddRange(ref MapMagic.instance.gens.list, loadedGens.list);
				MapMagic.instance.gens.ChangeGenerator(null);
				repaint=true; Repaint();
			}


		#endregion
		
		//repainting gui on generate state changed (or if running to make a animated indicator)
		private void OnInspectorUpdate () 
		{ 	
			if (MapMagic.instance == null) return;
			if (!MapMagic.instance.terrains.complete) Repaint();

			//testing serialization
			/*if (MapMagic.instance.guiDebug)
			{
				Serializer ser = new Serializer();
				ser.Store(MapMagic.instance.gens); 
				if (!ser.Equals(MapMagic.instance.serializer)) Debug.LogError("Serialization Difference");
			}*/ //old way to test serialization
		}

		private void OnDisable ()
		{
			//removing callbacks
			Portal.OnChooseEnter -= DrawPortalSelector;
		}

		private bool repaint = false;
		private void OnGUI() { DrawWindow(); if (repaint) DrawWindow(); repaint = false; } //drawing window, or doing it twice if repaint is needed
		private void DrawWindow()
		{		
			if (MapMagic.instance == null) MapMagic.instance = FindObjectOfType<MapMagic>();
			MapMagic script = MapMagic.instance;
			if (script==null) return;
			//if (script.gens==null) script.gens = ScriptableObject.CreateInstance<GeneratorsAsset>();

			//starting layout
			if (layout==null) layout = new Layout();
			//layout.window = this;
			layout.maxZoom = 1f;
			layout.scroll = script.guiScroll; layout.zoom = script.guiZoom; //loading saved scroll and zoom (to avoid resetting on deserialize)
			layout.Zoom(); layout.Scroll(); //scrolling and zooming
			layout.margin = 5; layout.rightMargin = 5+3;
			script.guiScroll = layout.scroll; script.guiZoom = layout.zoom; //saving

			//unity 5.4 beta
			if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.layout) return; 

			//using middle mouse click events
			if (Event.current.button == 2) Event.current.Use();

			//undo
			Undo.undoRedoPerformed -= PerformUndo;
			Undo.undoRedoPerformed += PerformUndo;

			//setting title content
			titleContent = new GUIContent("Map Magic");
			titleContent.image = layout.GetIcon("MapMagic_WindowIcon");

			//drawing background
			Vector2 windowZeroPos = layout.ToInternal(Vector2.zero);
			windowZeroPos.x = ((int)(windowZeroPos.x/64f)) * 64; 
			windowZeroPos.y = ((int)(windowZeroPos.y/64f)) * 64; 
			layout.Icon( 
				"MapMagic_Background",
				new Rect(windowZeroPos - new Vector2(64,64), 
				position.size + new Vector2(128,128)), 
				tile:true);

			//calculating visible area
			Rect visibleArea = layout.ToInternal( new Rect(0,0,position.size.x,position.size.y) );
			//visibleArea = new Rect(visibleArea.x+100, visibleArea.y+100, visibleArea.width-200, visibleArea.height-200);
			//layout.Label("Area", helpBox:true, rect:visibleArea);

			//checking if all generators are loaded, and none of them is null
			for (int i=MapMagic.instance.gens.list.Length-1; i>=0; i--)
			{
				if (MapMagic.instance.gens.list[i] == null) { ArrayTools.RemoveAt(ref MapMagic.instance.gens.list, i); continue; }
				foreach (Generator.Input input in MapMagic.instance.gens.list[i].Inputs()) 
				{
					if (input == null) continue;
					if (input.linkGen == null) input.Link(null, null);
				}
			}

			#region Drawing groups
				for(int i=0; i<MapMagic.instance.gens.list.Length; i++)
				{
					if (!(MapMagic.instance.gens.list[i] is Group)) continue;
					Group group = MapMagic.instance.gens.list[i] as Group;

					//checking if this is within layout field
					if (group.guiRect.x > visibleArea.x+visibleArea.width || group.guiRect.y > visibleArea.y+visibleArea.height ||
						group.guiRect.x+group.guiRect.width < visibleArea.x || group.guiRect.y+group.guiRect.height < visibleArea.y) 
							if (group.guiRect.width > 0.001f && layout.dragState != Layout.DragState.Drag) continue; //if guiRect initialized and not dragging

					//setting layout data
					group.layout.field = group.guiRect;
					group.layout.scroll = layout.scroll;
					group.layout.zoom = layout.zoom;

					group.OnGUI();

					group.guiRect = group.layout.field;
				}
			#endregion

			#region Drawing connections (before generators to make them display under nodes)
				foreach(Generator gen in MapMagic.instance.gens.list)
				{
					if (gen is PreviewOutput && !MapMagic.instance.guiDebug) continue;
					foreach (Generator.Input input in gen.Inputs())
					{
						if (input==null || input.link == null) continue; //input could be null in layered generators
						if (gen is Portal && (gen as Portal).form == Portal.PortalForm.Out) continue; //do not draw connections for exit portals
						layout.Spline(input.link.guiConnectionPos, input.guiConnectionPos, color:Generator.CanConnect(input.link,input)? input.guiColor : Color.red);
					}
				}
			#endregion

			#region creating connections (after generators to make clicking in inout work)
			int dragIdCounter = MapMagic.instance.gens.list.Length+1;
				foreach (Generator gen in MapMagic.instance.gens.list)
					foreach (Generator.IGuiInout inout in gen.Inouts())
				{
					if (gen is PreviewOutput && !MapMagic.instance.guiDebug) continue;
					if (inout == null) continue;
					if (layout.DragDrop(inout.guiRect, dragIdCounter))
					{
						//finding target
						Generator.IGuiInout target = null;
						foreach (Generator gen2 in MapMagic.instance.gens.list)
							foreach (Generator.IGuiInout inout2 in gen2.Inouts())
								if (inout2.guiRect.Contains(layout.dragPos)) target = inout2;

						//converting inout to Input (or Output) and target to Output (or Input)
						Generator.Input input = inout as Generator.Input;		if (input==null) input = target as Generator.Input;
						Generator.Output output = inout as Generator.Output;	if (output==null) output = target as Generator.Output;

						//connection validity test
						bool canConnect = input!=null && output!=null && Generator.CanConnect(output,input);

						//infinite loop test
						if (canConnect)
						{ 
							Generator outputGen = output.GetGenerator(MapMagic.instance.gens.list);
							Generator inputGen = input.GetGenerator(MapMagic.instance.gens.list);
							if (inputGen == outputGen || outputGen.IsDependentFrom(inputGen)) canConnect = false;
						}

						//drag
						//if (layout.dragState==Layout.DragState.Drag) //commented out because will not be displayed on repaint otherwise
						//{
							if (input == null) layout.Spline(output.guiConnectionPos, layout.dragPos, color:Color.red);
							else if (output == null) layout.Spline(layout.dragPos, input.guiConnectionPos, color:Color.red);
							else layout.Spline(output.guiConnectionPos, input.guiConnectionPos, color:canConnect? inout.guiColor : Color.red);
						//}

						//release
						if (layout.dragState==Layout.DragState.Released && input!=null) //on release. Do nothing if input not defined
						{
							Undo.RecordObject (MapMagic.instance.gens, "MapMagic Connection"); 
							MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;

							input.Unlink();
							if (canConnect) input.Link(output, output.GetGenerator(MapMagic.instance.gens.list));
							MapMagic.instance.gens.ChangeGenerator(gen);

							EditorUtility.SetDirty(MapMagic.instance.gens);
						}
					}
					dragIdCounter++;
				}
			#endregion

			#region Drawing generators
				for(int i=0; i<MapMagic.instance.gens.list.Length; i++)
				{
					Generator gen = MapMagic.instance.gens.list[i];
					if (gen is Group) continue; //skipping groups
					if (gen is PreviewOutput && !MapMagic.instance.guiDebug) continue;

					//checking if this generator is within layout field
					if (gen.guiRect.x > visibleArea.x+visibleArea.width || gen.guiRect.y > visibleArea.y+visibleArea.height ||
						gen.guiRect.x+gen.guiRect.width < visibleArea.x || gen.guiRect.y+gen.guiRect.height < visibleArea.y) 
							if (gen.guiRect.width > 0.001f && layout.dragState != Layout.DragState.Drag) continue; //if guiRect initialized and not dragging

					if (gen.layout == null) gen.layout = new Layout();
					gen.layout.field = gen.guiRect;
				
					//gen.layout.OnBeforeChange -= RecordGeneratorUndo;
					//gen.layout.OnBeforeChange += RecordGeneratorUndo;
					gen.layout.undoObject = MapMagic.instance.gens;
					gen.layout.undoName = "MapMagic Generators Change"; 
					gen.layout.dragChange = true;
					gen.layout.disabled = script.locked;

					//copy layout params
					gen.layout.scroll = layout.scroll;
					gen.layout.zoom = layout.zoom;

					//drawing
					gen.OnGUIBase();

					//instant generate on params change
					if (gen.layout.change) 
					{
						//EditorUtility.SetDirty(script); //already done via undo in layout
						MapMagic.instance.gens.ChangeGenerator(gen);
						repaint=true; Repaint();

						EditorUtility.SetDirty(MapMagic.instance.gens);
					}
			
					if (gen.guiRect.width<1 && gen.guiRect.height<1) { repaint=true;  Repaint(); } //repainting if some of the generators rect is 0
					gen.guiRect = gen.layout.field;
				}
			#endregion

			#region Toolbar
				if (toolbarLayout==null) toolbarLayout = new Layout();
				toolbarLayout.margin = 0; toolbarLayout.rightMargin = 0;
				toolbarLayout.field.width = this.position.width;
				toolbarLayout.field.height = 18;
				toolbarLayout.cursor = new Rect();
				//toolbarLayout.window = this;
				toolbarLayout.Par(18, padding:0);

				EditorGUI.LabelField(toolbarLayout.field, "", EditorStyles.toolbarButton);

				toolbarLayout.Inset(3);
				if (GUI.Button(toolbarLayout.Inset(100), "Generate", EditorStyles.toolbarButton) && MapMagic.instance.enabled) MapMagic.instance.ForceGenerate();

				Rect seedLabelRect = toolbarLayout.Inset(34); seedLabelRect.y+=1; seedLabelRect.height-=4;
				Rect seedFieldRect = toolbarLayout.Inset(64); seedFieldRect.y+=2; seedFieldRect.height-=4;
				EditorGUI.LabelField(seedLabelRect, "Seed:", EditorStyles.miniLabel);
				int newSeed = EditorGUI.IntField(seedFieldRect, MapMagic.instance.seed, EditorStyles.toolbarTextField);
				if (newSeed != MapMagic.instance.seed) { MapMagic.instance.seed = newSeed; if (MapMagic.instance.instantGenerate) MapMagic.instance.ForceGenerate(); }
			
				//drawing state icon
				if (!MapMagic.instance.terrains.complete) { toolbarLayout.Icon("MapMagic_Loading", new Rect(10,0,16,16), animationFrames:12); Repaint(); }
				else toolbarLayout.Icon("MapMagic_Success", new Rect(10,0,16,16));
			#endregion

			#region Draging

				//dragging generators
				for(int i=MapMagic.instance.gens.list.Length-1; i>=0; i--)
				{
					Generator gen = MapMagic.instance.gens.list[i];
					if (gen is Group) continue;
					gen.layout.field = gen.guiRect;

					//dragging
					if (layout.DragDrop(gen.layout.field, i)) 
					{
						if (layout.dragState == Layout.DragState.Pressed) 
						{
							Undo.RecordObject (MapMagic.instance.gens, "MapMagic Generators Drag");
							MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;
						}
						if (layout.dragState == Layout.DragState.Drag || layout.dragState == Layout.DragState.Released) 
						{ 
							//moving inout rects to remove lag
							//foreach (Generator.IGuiInout inout in gen.Inouts())
							//	inout.guiRect = new Rect(inout.guiRect.position+layout.dragDelta, inout.guiRect.size);
							gen.Move(layout.dragDelta,true);
							repaint=true; Repaint(); 

							EditorUtility.SetDirty(MapMagic.instance.gens);
						}
					}

					//saving all generator rects
					gen.guiRect = gen.layout.field;
				}

				//dragging groups
				for (int i=MapMagic.instance.gens.list.Length-1; i>=0; i--)
				{
					//Generator gen = MapMagic.instance.gens.list[i];
					Group group = MapMagic.instance.gens.list[i] as Group;
					if (group == null) continue;
					group.layout.field = group.guiRect;

					//resizing
					group.layout.field = layout.ResizeRect(group.layout.field, i+20000);

					//dragging
					if (layout.DragDrop(group.layout.field, i)) 
					{
						if (layout.dragState == Layout.DragState.Pressed) 
						{
							Undo.RecordObject (MapMagic.instance.gens, "MapMagic Group Drag");
							MapMagic.instance.gens.setDirty = !MapMagic.instance.gens.setDirty;
							group.Populate();
						}
						if (layout.dragState == Layout.DragState.Drag || layout.dragState == Layout.DragState.Released) 
						{ 
							group.Move(layout.dragDelta,true);
							repaint=true; Repaint(); 

							EditorUtility.SetDirty(MapMagic.instance.gens);
						}
						if (layout.dragState == Layout.DragState.Released && group != null) MapMagic.instance.gens.SortGroups();
					}

					//saving all group rects
					group.guiRect = group.layout.field;
				}
			#endregion

			//right-click menus
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1) DrawPopup();

			//debug center
			//EditorGUI.HelpBox(layout.ToLocal(new Rect(-25,-10,50,20)), "Zero", MessageType.None);

			//assigning portal popup action
			Portal.OnChooseEnter -= DrawPortalSelector; Portal.OnChooseEnter += DrawPortalSelector;


			//producing synthetic lag to test performance
			//for (int i=0; i<10; i++) Debug.LogWarning("Lag");

			//drawing lock warning
			if (MapMagic.instance.locked)
			{
				toolbarLayout.Label("", rect: new Rect(4,20,300,155), helpbox:true);
				toolbarLayout.Label("You are using the demo version of Map Magic. It should be used for evaluation purposes only and has a save limitation: although the node graph could be saved, it could not be edited after the load. " +
					"You can get a fully functional version at the Asset Store or reset your graph to proceed editing:", rect: new Rect(4,20,300,155), helpbox:true);
				if (toolbarLayout.Button("Get MapMagic at the Asset Store", rect: new Rect(20,120, 260, 22))) Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/56762"); 
				if (toolbarLayout.Button("Reset node graph", rect: new Rect(20,145, 260, 18))) { script.UnlockAndReset(); script.gens.OnBeforeSerialize(); }
			}

		}

		public void DrawPopup ()
		{
			Vector2 mousePos = layout.ToInternal(Event.current.mousePosition);
				
			//finding something that was clicked
			Generator clickedGenerator = null;
			for (int i=0; i<MapMagic.instance.gens.list.Length; i++) 
			{
				Generator gen = MapMagic.instance.gens.list[i];
				if (gen.guiRect.Contains(mousePos) && !(gen is Group)) clickedGenerator = MapMagic.instance.gens.list[i];
			}

			Group clickedGroup = null;
			for (int i=0; i<MapMagic.instance.gens.list.Length; i++) 
			{
				Generator gen = MapMagic.instance.gens.list[i];
				if (gen.guiRect.Contains(mousePos) && gen is Group) clickedGroup = MapMagic.instance.gens.list[i] as Group;
			}

			if (clickedGenerator == null) clickedGenerator = clickedGroup;
			
			Generator.Output clickedOutput = null;
			for (int i=0; i<MapMagic.instance.gens.list.Length; i++) 
				foreach (Generator.Output output in MapMagic.instance.gens.list[i].Outputs())
					if (output.guiRect.Contains(mousePos)) clickedOutput = output;

			//create
			Dictionary<string, PopupMenu.MenuItem> itemsDict = new Dictionary<string, PopupMenu.MenuItem>();
			
			List<System.Type> allGeneratorTypes = typeof(Generator).GetAllChildTypes();
			for (int i=0; i<allGeneratorTypes.Count; i++)
			{
				if (System.Attribute.IsDefined(allGeneratorTypes[i], typeof(GeneratorMenuAttribute)))
				{
					GeneratorMenuAttribute attribute = System.Attribute.GetCustomAttribute(allGeneratorTypes[i], typeof(GeneratorMenuAttribute)) as GeneratorMenuAttribute;
					System.Type genType = allGeneratorTypes[i];

					if (attribute.disabled) continue;

					PopupMenu.MenuItem item = new PopupMenu.MenuItem(attribute.name, delegate () { CreateGenerator(genType, mousePos); });
					item.priority = attribute.priority;

					if (attribute.menu.Length != 0)
					{
						if (!itemsDict.ContainsKey(attribute.menu)) itemsDict.Add(attribute.menu, new PopupMenu.MenuItem(attribute.menu, subs:new PopupMenu.MenuItem[0]));
						ArrayTools.Add(ref itemsDict[attribute.menu].subItems, item);
					}
					else itemsDict.Add(attribute.name, item);
				}
			} 
			PopupMenu.MenuItem[] createItems = new PopupMenu.MenuItem[itemsDict.Count];
			itemsDict.Values.CopyTo(createItems, 0);

			//create group
			//PopupMenu.MenuItem createGroupItem = new PopupMenu.MenuItem("Group",  delegate () { CreateGroup(mousePos); });
			//Extensions.ArrayAdd(ref createItems, createItems.Length-1, createGroupItem);

			//additional name
			string additionalName = "All";
			if (clickedGenerator != null) 
			{
				additionalName = "Generator";
				if (clickedGenerator is Group) additionalName = "Group";
			}

			//preview
			PopupMenu.MenuItem[] previewSubs = new PopupMenu.MenuItem[]
			{
				new PopupMenu.MenuItem("On Terrain", delegate() {PreviewOutput(clickedGenerator, clickedOutput, false);}, disabled:clickedOutput==null||clickedGenerator==null), 
				new PopupMenu.MenuItem("In Window", delegate() {PreviewOutput(clickedGenerator, clickedOutput, true);}, disabled:clickedOutput==null||clickedGenerator==null),
				new PopupMenu.MenuItem("Clear", delegate() {PreviewOutput(null, null, false);}, disabled:MapMagic.instance.gens.GetGenerator<PreviewOutput>()==null)
			};

			PopupMenu.MenuItem[] popupItems = new PopupMenu.MenuItem[]
			{
				new PopupMenu.MenuItem("Create", createItems),
				new PopupMenu.MenuItem("Save " + additionalName,	delegate () { SaveGenerator(clickedGenerator, mousePos); }),
				new PopupMenu.MenuItem("Load",						delegate () { LoadGenerator(mousePos); }),
				new PopupMenu.MenuItem("Duplicate",					delegate () { DuplicateGenerator(clickedGenerator); }),
				new PopupMenu.MenuItem("Remove",	delegate () { if (clickedGenerator!=null) DeleteGenerator(clickedGenerator); },	disabled:(clickedGenerator==null)),
				new PopupMenu.MenuItem("Reset",						delegate () { if (clickedGenerator!=null) ResetGenerator(clickedGenerator); },	disabled:clickedGenerator==null), 
				new PopupMenu.MenuItem("Preview", previewSubs)
			};

			PopupMenu.DrawPopup(popupItems, Event.current.mousePosition, closeAllOther:true);
		}

		public void DrawPortalSelector (Portal exit, Generator.InoutType type)
		{
			int entersNum = 0;
			for (int g=0; g<MapMagic.instance.gens.list.Length; g++)
			{
				Portal portal = MapMagic.instance.gens.list[g] as Portal;
				if (portal == null) continue;
				if (portal.form == Portal.PortalForm.Out) continue;
				if (portal.type != type) continue;

				entersNum++;
			}
			
			PopupMenu.MenuItem[] popupItems = new PopupMenu.MenuItem[entersNum];
			int counter = 0;
			for (int g=0; g<MapMagic.instance.gens.list.Length; g++)
			{
				Portal enter = MapMagic.instance.gens.list[g] as Portal;
				if (enter == null) continue;
				if (enter.form == Portal.PortalForm.Out) continue;
				if (enter.type != type) continue;

				popupItems[counter] = new PopupMenu.MenuItem( enter.input.guiName, delegate () { exit.input.Link(enter.output, enter); MapMagic.instance.gens.ChangeGenerator(exit); } );
				counter++;
			}

			PopupMenu.DrawPopup(popupItems, Event.current.mousePosition, closeAllOther:true);

		}

		public void FocusOnOutput ()
		{
			if (MapMagic.instance == null) MapMagic.instance = FindObjectOfType<MapMagic>();
			MapMagic script = MapMagic.instance;
			if (script == null) return;
			
			//finding height output
			Rect outputRect = new Rect();
			if (script.gens.GetGenerator<HeightOutput>() != null) outputRect = script.gens.GetGenerator<HeightOutput>().guiRect;
			else if (script.gens.list.Length != 0) outputRect = script.gens.list[0].guiRect;

			//focusing
			layout = new Layout();
			outputRect = layout.ToDisplay(outputRect);
			layout.scroll = -outputRect.center;
			layout.scroll.y += this.position.height / 2;
			layout.scroll.x += this.position.width - outputRect.width; 

			//saving
			if (script==null) script = FindObjectOfType<MapMagic>();
			script.guiScroll = layout.scroll; script.guiZoom = layout.zoom; //saving
		}

		public void TempNew ()
		{
			/*script.generators = new Generator[2];
			script.generators[0] = new NoiseGenerator();
			script.generators[1] = new HeightOutput();
			NoiseGenerator noiseGen = (NoiseGenerator)script.generators[0];
			HeightOutput heightOut = (HeightOutput)script.generators[1];
			noiseGen.guiRect = new Rect(123,2,200,100);
			//noiseGen.output.resolution = 512;
			heightOut.input.link = noiseGen.output;
			heightOut.guiRect = new Rect(43,76,200,20);*/
		}
	}

}//namespace