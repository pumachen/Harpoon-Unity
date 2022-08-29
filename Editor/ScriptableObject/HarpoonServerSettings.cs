using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace Harpoon
{
	[Serializable]
	public class HarpoonServerSettings
	{
		[SerializeField]
		public string m_url = "http://127.0.0.1:80";
		
		static string path => Path.Combine(Application.dataPath, "../ProjectSettings/Packages/com.pum4ch3n.harpoon/HarpoonServerSettings.json");

		private static HarpoonServerSettings m_instance;
		private static HarpoonServerSettings instance
		{
			get
			{
				if (m_instance == null)
				{
					string jsonPath = path;
					if (!File.Exists(jsonPath))
					{
						string dirName = Path.GetDirectoryName(jsonPath);
						if (!Directory.Exists(dirName))
						{
							Directory.CreateDirectory(dirName);
						}
						var setting = new HarpoonServerSettings();
						string json = JsonConvert.SerializeObject(setting);
						File.WriteAllText(jsonPath, json);
						m_instance = setting;
					}
					else
					{
						m_instance = JsonConvert.DeserializeObject<HarpoonServerSettings>(File.ReadAllText(jsonPath));
					}	
				}
				return m_instance;
			}
		}

		public static string url
		{
			get => instance.m_url;
			set
			{
				if (string.Compare(url, value) != 0)
				{
					instance.m_url = value;
					File.WriteAllText(path, JsonConvert.SerializeObject(instance));
				}
			}
		}
	}
}