using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Harpoon
{
    public class HarpoonServerSettingsWindow : EditorWindow
    {
        [MenuItem("Window/Harpoon/ServerSettings")]
        public static void OpenWindow()
        {
            EditorWindow.GetWindow<HarpoonServerSettingsWindow>().Show();
        }

        private string url;

        private void ReloadServerSettings()
        {
            url = HarpoonServerSettings.url;
        }

        private void OnEnable()
        {
            ReloadServerSettings();
        }

        private void OnGUI()
        {
            url = EditorGUILayout.TextField("URL", url);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Revert"))
            {
                ReloadServerSettings();
            }
            if (GUILayout.Button("Apply"))
            {
                HarpoonServerSettings.url = url;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}