using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using Plugins;

namespace MapMagic
{
	[System.Serializable]
	public class GeneratorsAsset : ScriptableObject , ISerializationCallbackReceiver
	{
		public Generator[] list = new Generator[0];
		//public Generator[] outputs = new Generator[0];

		//"public HeightOutput heightOut = null;" - can fail because null cannot be serialized on undo
		//it fills with default on serialize (no matter what OnBeforeSerialize does) and since OnAfterDeserialize is not called it does not return back to null

			
		public T GetGenerator<T> () where T:Generator
		{
			for (int i=0; i<list.Length; i++)
				if (list[i] is T) return (T)list[i];
			return null; 
		}

		public Generator GetGenerator (System.Type type) //the same but not generic
		{
			for (int i=0; i<list.Length; i++)
				if (list[i].GetType() == type) return list[i];
			return null;
		}

		public IEnumerable GeneratorsOfType<T> () where T:Generator
		{
			for (int i=0; i<list.Length; i++)
				if (list[i] is T) yield return list[i];
		}

		public IEnumerable<Generator> AllGensThenGroups (bool inverse=false)
		{
			if (!inverse)
			{
				for (int i=0; i<list.Length; i++) if (!(list[i] is Group)) yield return list[i];
				for (int i=0; i<list.Length; i++) if (list[i] is Group) yield return list[i];
			}
			else
			{
				for (int i=list.Length-1; i>=0; i--) if (!(list[i] is Group)) yield return list[i];
				for (int i=list.Length-1; i>=0; i--) if (list[i] is Group) yield return list[i];
			}
		}


		public void ChangeGenerator (Generator gen)
		{
			//if save intermediate - cleaing this generator only
			if (gen!=null && MapMagic.instance.saveIntermediate)
				foreach (MapMagic.Chunk chunk in MapMagic.instance.terrains.Objects()) //iterating in all terrain wrappers
				{
					chunk.ready.CheckRemove(gen);
					foreach (Generator.Output output in gen.Outputs())
						chunk.results.CheckRemove(output);
				}
			
			//if do not save intermediate - clearing all generators
			if (!MapMagic.instance.saveIntermediate)
				foreach (MapMagic.Chunk chunk in MapMagic.instance.terrains.Objects())
					{ chunk.results.Clear(); chunk.ready.Clear(); } 
			
			//starting rebuild
			if (MapMagic.instance.instantGenerate && MapMagic.instance.enabled) { MapMagic.instance.terrains.start = true; MapMagic.instance.Update(); }
		}


		public Generator CreateGenerator (System.Type type, Vector2 guiPos=new Vector2())
		{
			Generator gen = (Generator)System.Activator.CreateInstance(type);
 
			gen.guiRect.x = guiPos.x - gen.guiRect.width/2;
			gen.guiRect.y = guiPos.y - 10;
			if (gen is Group)
			{
				gen.guiRect.width = 300;
				gen.guiRect.height = 200;
			}

			//adding to outputs
			if (gen is IOutput && GetGenerator(type) != null) 
				{ Debug.LogError("MapMagic: Trying to add Output Generator while it already present in generators list"); return null; }
					
			//adding to list
			ArrayTools.Add(ref list, gen);

			return gen;
		}


		public void DeleteGenerator (Generator gen)
		{
			//removing generator from 'ready' and 'results' arrays 
			ChangeGenerator(gen);
	
			//manually resetting all dependent generators ready stae
			for (int g=0; g<list.Length; g++)
				if (list[g].IsDependentFrom(gen)) ChangeGenerator(list[g]);
				
			//clearing if it is output gen
			//if (gen is IOutput) 
			//	foreach (MapMagic.Chunk chunk in MapMagic.instance.terrains.Objects())
			//		(gen as IOutput).Clear(chunk);
			
			//removing group members if it is group
			#if UNITY_EDITOR
			if (gen is Group &&
				UnityEditor.EditorUtility.DisplayDialog("Remove Containing Generators", "Do you want to remove a contaning generators as well?", "Remove Generators", "Remove Group Only"))
				{
					Group group = gen as Group;
					group.Populate();
					for (int g=group.generators.Count-1; g>=0; g--) MapMagic.instance.gens.DeleteGenerator(group.generators[g]);
				}
			#endif

			//unlinking and removing it's reference in inputs and outputs
			//for (int g=0; g<list.Length; g++)
			//	foreach (Generator.Input input in list[g].Inputs())
			//		if (input.linkGen == gen) input.Unlink();
			UnlinkGenerator(gen);

			//removing from output generators list
			//if (gen is IOutput) 
			//	Extensions.ArrayRemove(ref outputs, gen);

			//removing from array
			ArrayTools.Remove(ref list, gen);

			//if it is preview - applying splats
			if (gen is PreviewOutput)
			{
				SplatOutput splatOut = GetGenerator<SplatOutput>();
				if (splatOut != null) ChangeGenerator(splatOut);
				else 
					foreach (MapMagic.Chunk chunk in MapMagic.instance.terrains.Objects())
						chunk.ClearSplats(); 
			}
		}

		public void ClearGenerators ()
		{
			list = new Generator[0];
			//outputs = new Generator[0];
		}

		public void UnlinkGenerator (Generator gen, bool unlinkGroup=false)
		{
			//unlinking
			foreach (Generator.Input input in gen.Inputs()) { if (input != null) input.Unlink(); }

			//removing it's reference in inputs and outputs
			for (int g=0; g<list.Length; g++)
				foreach (Generator.Input input in list[g].Inputs())
					if (input != null && input.linkGen == gen) input.Unlink();
			
			//unlinking group
			Group grp = gen as Group;
			if (grp != null && unlinkGroup)
			{
				for (int g=0; g<list.Length; g++)
				{
					//if generator in group - unlinking it from non-group gens
					if (grp.guiRect.Contains(list[g].guiRect)) 
						foreach (Generator.Input input in list[g].Inputs())
							if (!grp.guiRect.Contains(input.linkGen.guiRect)) input.Unlink();

					//if generator not in group - unlinking it from non-group gens
					if (!grp.guiRect.Contains(list[g].guiRect)) 
						foreach (Generator.Input input in list[g].Inputs())
							if (grp.guiRect.Contains(input.linkGen.guiRect)) input.Unlink();
				} 
			}
		}

		public void SortGroups ()
		{
			for (int i=list.Length-1; i>=0; i--)
			{
				Generator grp = list[i];
				if (!(grp is Group)) continue;

				for (int g=0; g<list.Length; g++)
				{
					Generator grp2 = list[g];
					if (!(grp2 is Group)) continue;

					if (grp2.layout.field.Contains(grp.layout.field)) ArrayTools.Switch(list, grp, grp2);
				}
			}
		}


		#region Serialization

			public Serializer serializer = new Serializer();
			public int serializedVersion = 0;
			public int test = 0;

			public int listNum = 0;

			public bool setDirty;

			public void OnBeforeSerialize () { OnBeforeSerializeManual(); }

			public void OnBeforeSerializeManual () //saving gens only on generator change to avoid each frame lag when MM is in inspector
			{ 
				//System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch(); timer.Start();
			
				//Debug.Log(list.Length); 

				serializer.Clear(); 
				listNum = serializer.Store(list); 
				//outputsNum = serializer.Store(outputs);

				serializedVersion = MapMagic.version;
				
				/*Tester tester = GameObject.FindObjectOfType<Tester>();
				if (tester == null)
				{
					GameObject testerObj = new GameObject();
					testerObj.name = "Tester";
					tester = testerObj.AddComponent<Tester>();
				}
				tester.ser = serializer;*/
				
				//timer.Stop(); Debug.Log("Serialize Time: " + timer.ElapsedMilliseconds + "ms");
			}

			public void OnAfterDeserialize ()
			{
				//if (guiDebug) { timer.Start(); }

				if (serializedVersion < 10) Debug.LogError("MapMagic: trying to load unknow version scene (v." + serializedVersion/10f + "). " +
					"This may cause errors or drastic drop in performance. " +  
					"Delete this MapMagic object and create the new one from scratch when possible."); 

				serializer.ClearLinks();

				list = (Generator[])serializer.Retrieve(listNum);
				//outputs = (Generator[])serializer.Retrieve(outputsNum);
				
				serializer.ClearLinks();

				//if (guiDebug) { timer.Stop(); Debug.Log("Deserialize Time: " + timer.ElapsedMilliseconds + "ms"); }
			}

		#endregion
	}
}
