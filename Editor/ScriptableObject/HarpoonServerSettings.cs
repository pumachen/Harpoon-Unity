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
		public string m_host = "127.0.0.1";
		[SerializeField]
		public string m_scheme = "http";
		[SerializeField]
		public int m_port = 80;
		
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

		public static string host
		{
			get => instance.m_host;
			set
			{
				if (string.Compare(host, value) != 0)
				{
					instance.m_host = value;
					File.WriteAllText(path, JsonConvert.SerializeObject(instance));
				}
			}
		}
		
		public static string scheme
		{
			get => instance.m_scheme;
		}

		public static int port
		{
			get => instance.m_port;
			set
			{
				if (value != port)
				{
					instance.m_port = value;
					File.WriteAllText(path, JsonConvert.SerializeObject(instance));
				}
			}
		}
	}
}