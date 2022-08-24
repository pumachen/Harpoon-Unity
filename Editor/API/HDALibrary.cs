using System;
using System.Collections;
using System.Collections.Generic;
using Harpoon.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Harpoon
{
	public static class HDALibrary
	{
		public struct HDAMeta
		{
			public string hda;
			public string name;
		}
		public static void GetHDALibraryAsync(Action<HDAMeta[]> completed, Action failed = null)
		{
			Uri uri = new Uri(HarpoonUriBuilder.Root, "api/hdalibrary");
			UnityWebRequest request = UnityWebRequest.Get(uri);
			request.SendWebRequest((request) =>
			{
				if (request.result == UnityWebRequest.Result.Success)
				{
					dynamic hdaLibrary = JsonConvert.DeserializeObject(request.downloadHandler.text);
					Dictionary<string, string> tops = hdaLibrary.Top.ToObject<Dictionary<string, string>>();
					HDAMeta[] hdaMeta = new HDAMeta[tops.Count];
					int i = 0;
					foreach (var top in tops)
					{
						hdaMeta[i++] = new HDAMeta()
						{
							hda = top.Key,
							name = top.Value
						};
					}

					completed?.Invoke(hdaMeta);
				}
				else
				{
					Debug.Log(request.error);
					failed?.Invoke();
				}
			});
		}
	}
}
