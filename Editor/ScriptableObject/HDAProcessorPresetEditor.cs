using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEditor;

namespace Harpoon
{
    [CustomEditor(typeof(HDAProcessorPreset))]
    public class HDAProcessorPresetEditor : Editor
    {
        private new HDAProcessorPreset target => base.target as HDAProcessorPreset;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("HDA", target.hda);
            foreach (var parm in target.parms)
            {
                parm.GUILayout();
            }

            target.timeout = EditorGUILayout.IntField("Timeout(s)", target.timeout);

            if (GUILayout.Button("Cook"))
            {
				HDAProcessor.ProcessHDAAsync(target, 
					zip =>
					{
						string outputDir = EditorUtility.SaveFolderPanel("Output Dir", Application.dataPath, "Output");
						zip.ExtractToDirectory(outputDir, true);
					}, timeout: target.timeout);
            }
        }
    }
}