using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

namespace Harpoon
{
	public class TORProcessorWindow : EditorWindow
	{
		private int torIdx = 0;
		private string[] tors = new string[0];
		private string[] torNames = new string[0];
		private Parm[] parms;
		private float progress = 1.0f;
		private static int timeout = 3000;

		private string tor => tors[torIdx];
	
		[MenuItem("Harpoon/TORProcessor")]
		public static void Open()
		{
			TORProcessorWindow window = CreateWindow<TORProcessorWindow>();
			window.titleContent = new GUIContent("TOR Processor");
			window.Show();
		}

		private void OnEnable()
		{
			TORLibrary.GetTORLibraryAsync(torLibrary =>
			{
				tors = new string[torLibrary.Length];
				torNames = new string[torLibrary.Length];
				for (int i = 0; i < torLibrary.Length; ++i)
				{
					tors[i] = torLibrary[i];
					torNames[i] = Path.GetFileNameWithoutExtension(torLibrary[i]);
				}
				UpdateTORParms();
			});
		}

		void UpdateTORParms()
		{
			TORProcessor.GetTORHeaderAsync(tor, (torHeader) =>
			{
				IEnumerable<Parm> _parms = Parm.CreateParms(torHeader);
				parms = _parms.ToArray();
			});
		}

		private void OnGUI()
		{
			int selectedIdx = EditorGUILayout.Popup("TOR", torIdx, torNames);
			if (selectedIdx != torIdx)
			{
				torIdx = selectedIdx;
				UpdateTORParms();
			}

			if (parms != null)
			{
				foreach (var parm in parms)
				{
					parm.GUILayout();
				}
			}
			
			timeout = Mathf.Max(30, EditorGUILayout.IntField("Timeout(s)", timeout));

			if (GUILayout.Button("Save As Preset"))
			{
				//CreateTORProcessorPreset();
			}

			if (progress < 1.0f)
			{
				Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
				EditorGUI.ProgressBar(rect, progress, "Cooking");
			}
			else if (GUILayout.Button("Cook"))
            {
            	Cook();
            }
		}

		/*void CreateTORProcessorPreset()
		{
			var torProcessorJob = ScriptableObject.CreateInstance<TORProcessorPreset>();
			torProcessorJob.intParms = parms.Where(p => p is IntParm).Select(p => p as IntParm).ToArray();
			torProcessorJob.floatParms = parms.Where(p => p is FloatParm).Select(p => p as FloatParm).ToArray();
			torProcessorJob.stringParms = parms.Where(p => p is StringParm).Select(p => p as StringParm).ToArray();
			torProcessorJob.tor = tor;
			string fileName = EditorUtility.SaveFilePanelInProject(
				"Save TOR Processor Preset", 
				$"{Path.GetFileNameWithoutExtension(tor)}", "asset", "");
			if (string.IsNullOrEmpty(fileName))
				return;
			if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fileName)))
			{
				AssetDatabase.DeleteAsset(fileName);
			}
			AssetDatabase.CreateAsset(torProcessorJob, fileName);
		}*/

		void Cook()
		{
			progress = 0.0f;
			TORProcessor.ProcessTORAsync(tor, parms, 
				zip =>
				{
					string outputDir = EditorUtility.SaveFolderPanel("Output Dir", Application.dataPath, "Output");
					if (!string.IsNullOrEmpty(outputDir))
					{
						zip.ExtractToDirectory(outputDir, true);
						AssetDatabase.Refresh();
					}
					progress = 1.0f;
				}, timeout: timeout);
		}
	}
}