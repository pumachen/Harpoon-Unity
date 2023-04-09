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
	public class HDAProcessorWindow : EditorWindow
	{
		private int hdaIdx = 0;
		private string[] hdas = new string[0];
		private string[] hdaNames = new string[0];
		private List<HouParm> parms;
		private float progress = 1.0f;

		private string hda => hdas[hdaIdx];
	
		[MenuItem("Window/Harpoon/HDAProcessor")]
		public static void Open()
		{
			HDAProcessorWindow window = CreateWindow<HDAProcessorWindow>();
			window.titleContent = new GUIContent("HDA Processor");
			window.Show();
		}

		private void OnEnable()
		{
			HDALibrary.GetHDALibraryAsync(hdaLibrary =>
			{
				hdas = new string[hdaLibrary.Length];
				hdaNames = new string[hdaLibrary.Length];
				for (int i = 0; i < hdaLibrary.Length; ++i)
				{
					hdas[i] = hdaLibrary[i].hda;
					hdaNames[i] = hdaLibrary[i].name;
				}
				UpdateHDAParms();
			});
		}

		void UpdateHDAParms()
		{
			HDAProcessor.GetHDAHeaderAsync(hda, (hdaHeader) =>
			{
				var parmTemplates = hdaHeader.parmTemplateGroup.parmTemplates;
				parms = new List<HouParm>();
				for (int i = 0; i < parmTemplates.Count; ++i)
				{
					ParmTemplate parmTemplate = parmTemplates[i].ToObject<ParmTemplate>();
					if (parmTemplate.isHidden)
						continue;
					switch (parmTemplate.dataType)
					{
						case (ParmData.Int):
						{
							parms.Add(new IntParm(parmTemplates[i].ToObject<IntParmTemplate>()));
							break;
						}
						case (ParmData.Float):
						{
							parms.Add(new FloatParm(parmTemplates[i].ToObject<FloatParmTemplate>()));
							break;
						}
						case (ParmData.String):
						{
							parms.Add(new StringParm(parmTemplates[i].ToObject<StringParmTemplate>()));
							break;
						}
					}
				}
			});
		}

		private void OnGUI()
		{
			int selectedIdx = EditorGUILayout.Popup("HDA", hdaIdx, hdaNames);
			if (selectedIdx != hdaIdx)
			{
				hdaIdx = selectedIdx;
				UpdateHDAParms();
			}

			if (parms != null)
			{
				foreach (var parm in parms)
				{
					parm.GUILayout();
				}
			}

			if (GUILayout.Button("Save As Preset"))
			{
				CreateHDAProcessorPreset();
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

		void CreateHDAProcessorPreset()
		{
			var hdaProcessorJob = ScriptableObject.CreateInstance<HDAProcessorPreset>();
			hdaProcessorJob.intParms = parms.Where(p => p is IntParm).Select(p => p as IntParm).ToArray();
			hdaProcessorJob.floatParms = parms.Where(p => p is FloatParm).Select(p => p as FloatParm).ToArray();
			hdaProcessorJob.stringParms = parms.Where(p => p is StringParm).Select(p => p as StringParm).ToArray();
			hdaProcessorJob.hda = hda;
			string fileName = EditorUtility.SaveFilePanelInProject(
				"Save HDA Processor Preset", 
				$"{Path.GetFileNameWithoutExtension(hda)}", "asset", "");
			if (string.IsNullOrEmpty(fileName))
				return;
			if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fileName)))
			{
				AssetDatabase.DeleteAsset(fileName);
			}
			AssetDatabase.CreateAsset(hdaProcessorJob, fileName);
		}

		void Cook()
		{
			progress = 0.0f;
			HDAProcessor.ProcessHDAAsync(hda, parms, 
				zip =>
				{
					string outputDir = EditorUtility.SaveFolderPanel("Output Dir", Application.dataPath, "Output");
					if (outputDir != null)
					{
						zip.ExtractToDirectory(outputDir, true);
						AssetDatabase.Refresh();
					}
					progress = 1.0f;
				});
		}
	}
}