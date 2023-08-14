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
	public static class TORLibrary
	{
		/*public static string[] TorLibrary()
		{
			GetTORLibraryAsync((tors) =>
			{
				foreach (var tor in tors)
				{
					Debug.Log(tor);
				}
			});
		}*/
		
		public static void GetTORLibraryAsync(Action<string[]> completed, Action failed = null)
		{
			Uri uri = new Uri(HarpoonUriBuilder.Root, "api/torlibrary");
			UnityWebRequest request = UnityWebRequest.Get(uri);
			request.SendWebRequest((request) =>
			{
				if (request.result == UnityWebRequest.Result.Success)
				{
					dynamic torLibrary = JsonConvert.DeserializeObject(request.downloadHandler.text);
					string[] tors = torLibrary.ToObject<string[]>();
					completed?.Invoke(tors);
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
