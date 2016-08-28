using UnityEditor;
using UnityEngine;
using System.Collections;

//using Plugins;

namespace MapMagic
{
		public class PreviewWindow : EditorWindow
		{
			static PreviewWindow instance;
			
			Layout textureLayout;
			Layout infoLayout;
			
			object currentObj;
			Texture2D texture;
			Matrix matrix;
			string label;

			Vector2 range = new Vector2(0,1);

			static public void ShowWindow ()
			{
				instance = (PreviewWindow)GetWindow (typeof (PreviewWindow));
				//PreviewWindow window = new PreviewWindow();

				instance.position = new Rect(100, 100, instance.position.width, instance.position.height);
				instance.titleContent = new GUIContent("Preview");
				instance.Show();
			}  
			static public void CloseWindow () { if (instance!=null) instance.Close(); }

			void SetObject (object obj)
			{
				if (obj is Matrix)
				{
					matrix = (Matrix)obj;
					texture = new Texture2D(matrix.rect.size.x, matrix.rect.size.z);
					texture.filterMode = FilterMode.Point;
					matrix.SimpleToTexture(texture, rangeMin:range.x, rangeMax:range.y);
				}

				if (obj is SpatialHash)
				{
					texture = null;
					matrix = null;
				}

				currentObj = obj;
				this.Repaint();
			}

			void OnEnable () 
			{ 
				if (MapMagic.instance == null) MapMagic.instance = FindObjectOfType<MapMagic>();
				PreviewOutput previewOutput = MapMagic.instance.gens.GetGenerator<PreviewOutput>();
				if (previewOutput == null) Close();
				previewOutput.OnObjectChanged += SetObject;
			}

			void OnDisable () 
			{ 
				PreviewOutput previewOutput = MapMagic.instance.gens.GetGenerator<PreviewOutput>();
				if (previewOutput == null) return;
				previewOutput.OnObjectChanged -= SetObject;
				MapMagic.instance.gens.DeleteGenerator(previewOutput);
			}
	
			void OnGUI () 
			{ 
				if (currentObj == null)
					{ EditorGUI.LabelField(new Rect(10,10,200,200), "Please wait until preview \nobject is being generated."); return; }
				
				if (textureLayout==null) textureLayout = new Layout();
				textureLayout.maxZoom = 8; textureLayout.minZoom = 0.125f; textureLayout.zoomStep = 0.125f;
				textureLayout.Zoom(); textureLayout.Scroll(); //scrolling and zooming
				UnityEditor.EditorGUI.DrawPreviewTexture(textureLayout.ToDisplay(new Rect(0,0,texture.width,texture.height)), texture);					
				
				if (infoLayout==null) infoLayout = new Layout();
				infoLayout.cursor = new Rect();
				infoLayout.margin = 10; infoLayout.rightMargin = 10;
				infoLayout.field = new Rect(this.position.width - 200 -10, this.position.height - 80 -10, 200, 80);
				UnityEditor.EditorGUI.HelpBox(infoLayout.field,"", UnityEditor.MessageType.None);
				UnityEditor.EditorGUI.HelpBox(infoLayout.field,"", UnityEditor.MessageType.None);

				infoLayout.fieldSize = 0.7f; //infoLayout.inputSize = 0.3f;
				infoLayout.Label("Size: " + texture.width + "x" + texture.height);
				infoLayout.Field(ref textureLayout.zoom, "Zoom: ",  min:textureLayout.minZoom, max:textureLayout.maxZoom, slider:true, quadratic:true);
				if (matrix != null)
				{
					infoLayout.Field(ref range, "Range: ",  min:-1, max:2, slider:true);
					if (infoLayout.lastChange) SetObject(matrix);

					infoLayout.Par(3); 
					if (infoLayout.Button("Save To Texture")) 
					{
						#if !UNITY_WEBPLAYER
						string path= UnityEditor.EditorUtility.SaveFilePanel(
							"Save Output Texture",
							"Assets",
							"OutputTexture.png", 
							"png");
						if (path!=null && path.Length!=0)
						{
							byte[] bytes = texture.EncodeToPNG();
							System.IO.File.WriteAllBytes(path, bytes);
						}
						#endif
					}
				}
			}
		}

		/*
		public class MatrixPreviewWindow : UnityEditor.EditorWindow
		{
			public Matrix matrix;
	
			Layout textureLayout;
			Layout infoLayout;

			public Texture2D texture;
			public Color[] colors; //to reuse on texture generation

			public Vector2 range = new Vector2(0,1);


			static public MatrixPreviewWindow ShowWindow (Matrix matrix, string name=null)
			{
				//MatrixPreviewWindow window = (MatrixPreviewWindow)EditorWindow.GetWindow (typeof (MatrixPreviewWindow));
				MatrixPreviewWindow window = new MatrixPreviewWindow();
				window.matrix = matrix;
				window.position = new Rect(100, 100, window.position.width, window.position.height);
				if (name == null) window.titleContent = new GUIContent("Matrix Preview"); else window.titleContent = new GUIContent(name);
				window.Init();
				window.Show();
				return window;
			}

			void Init ()
			{
				texture = new Texture2D(matrix.rect.size.x, matrix.rect.size.z);
				texture.filterMode = FilterMode.Point;
				matrix.SimpleToTexture(texture, colors, range.x, range.y);
				this.Repaint();
			}
	
			void OnGUI () 
			{ 
				if (textureLayout==null) textureLayout = new Layout(new Rect());
				textureLayout.maxZoom = 8; textureLayout.minZoom = 0.125f; textureLayout.zoomStep = 0.125f;
				textureLayout.Zoom(); textureLayout.Scroll(); //scrolling and zooming
				UnityEditor.EditorGUI.DrawPreviewTexture(textureLayout.ToLocal(new Rect(0,0,texture.width,texture.height)), texture);

				if (infoLayout==null) infoLayout = new Layout(new Rect());
				infoLayout.cursor = new Rect();
				infoLayout.margin = 10; infoLayout.rightMargin = 10;
				infoLayout.field = new Rect(this.position.width - 200 -10, this.position.height - 80 -10, 200, 80);
				UnityEditor.EditorGUI.HelpBox(infoLayout.field,"", UnityEditor.MessageType.None);
				UnityEditor.EditorGUI.HelpBox(infoLayout.field,"", UnityEditor.MessageType.None);

				infoLayout.fieldSize = 0.7f; infoLayout.inputSize = 0.3f;
				infoLayout.Par();
				infoLayout.Label("Size: " + texture.width + "x" + texture.height);
				infoLayout.ComplexSlider(ref textureLayout.zoom, "Zoom: ",  min:textureLayout.minZoom, max:textureLayout.maxZoom, quadratic:true);
				infoLayout.ComplexSlider(ref range, "Range: ",  min:-1, max:2);
				if (infoLayout.lastChange) Init();

				infoLayout.Par(3); infoLayout.Par();
				if (infoLayout.Button("Save To Texture", width:0.5f)) 
				{
					#if !UNITY_WEBPLAYER
					string path= UnityEditor.EditorUtility.SaveFilePanel(
						"Save Output Texture",
						"Assets",
						"OutputTexture.png", 
						"png");
					if (path!=null)
					{
						byte[] bytes = texture.EncodeToPNG();
						System.IO.File.WriteAllBytes(path, bytes);
					}
					#endif
				}
			}
		}

		public class SpatialHashPreviewWindow : UnityEditor.EditorWindow
		{
			public SpatialHash spatialHash;
	
			Layout baseLayout;
			Layout infoLayout;

			static public SpatialHashPreviewWindow ShowWindow (SpatialHash spatialHash, string name=null)
			{
				//SpatialHashPreviewWindow window = (SpatialHashPreviewWindow)EditorWindow.GetWindow (typeof (SpatialHashPreviewWindow));
				SpatialHashPreviewWindow window = new SpatialHashPreviewWindow();
				window.spatialHash = spatialHash;
				window.position = new Rect(100, 100, 1000, 1000);
				if (name == null) window.titleContent = new GUIContent("Spatial Hash Preview"); else window.titleContent = new GUIContent(name);
				window.Show();
				return window;
			}
	
			void OnGUI () 
			{ 
				if (baseLayout==null) baseLayout = new Layout(new Rect());
				baseLayout.maxZoom = 8; baseLayout.minZoom = 0.125f; baseLayout.zoomStep = 0.125f;
				baseLayout.Zoom(); baseLayout.Scroll(); //scrolling and zooming
			
				for (int i=0; i<spatialHash.cells.Length; i++)
				{
					SpatialHash.Cell cell = spatialHash.cells[i];
					
					//drawing grid
					UnityEditor.Handles.color = Color.gray;
					UnityEditor.Handles.DrawPolyLine(  
						ToLocal(cell.min),  
						ToLocal(new Vector2(cell.max.x, cell.min.y)),
						ToLocal(cell.max), 
						ToLocal(new Vector2(cell.min.x, cell.max.y)),
						ToLocal(cell.min) );

					//drawing objects
					UnityEditor.Handles.color = new Color(0.4f, 0.9f, 0.2f);
					for (int j=0; j<cell.objs.Count; j++)
						DrawCircle(ToLocal(cell.objs[j].pos), 5);
				}
			}

			Vector2 ToLocal (Vector2 pos)
				{ return baseLayout.ToLocal( (pos-spatialHash.offset)/spatialHash.size * 1000 ); }

			void DrawCircle (Vector2 pos, float radius)
			{
				UnityEditor.Handles.DrawAAConvexPolygon(  
						pos + new Vector2(0,1)*radius, 
						pos + new Vector2(0.71f,0.71f)*radius,
						pos + new Vector2(1,0)*radius,
						pos + new Vector2(0.71f,-0.71f)*radius,
						pos + new Vector2(0,-1)*radius,
						pos + new Vector2(-0.71f,-0.71f)*radius,
						pos + new Vector2(-1,0)*radius,
						pos + new Vector2(-0.71f,0.71f)*radius);
			}
		}
		*/
}