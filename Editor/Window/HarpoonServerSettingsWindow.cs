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

        //private string scheme;
        private string host;
        private int port;

        private void ReloadServerSettings()
        {
            //scheme = HarpoonServerSettings.scheme;
            host = HarpoonServerSettings.host;
            port = HarpoonServerSettings.port;
        }

        private void OnEnable()
        {
            ReloadServerSettings();
        }

        private void OnGUI()
        {
            host = EditorGUILayout.TextField("Host", host);
            port = EditorGUILayout.IntField("Port", port);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Revert"))
            {
                ReloadServerSettings();
            }
            if (GUILayout.Button("Apply"))
            {
                HarpoonServerSettings.host = host;
                HarpoonServerSettings.port = port;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}