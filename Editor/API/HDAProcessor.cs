using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using DotLiquid.Util;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using Harpoon.Utils;
using UnityEditor.PackageManager;

namespace Harpoon
{
	public static class HDAProcessor
	{
		private static Uri GetUri(string hdaName)
		{
			return new Uri(HarpoonUriBuilder.Root, $"api/hdaprocessor/{hdaName}");
		}
		
		public static void GetHDAHeaderAsync(string hdaName, Action<dynamic> completed, Action failed = null)
		{
			Uri uri = GetUri(hdaName);
			UnityWebRequest request = UnityWebRequest.Get(uri);
			request.SendWebRequest((request) =>
			{
				if (request.result == UnityWebRequest.Result.Success)
				{
					completed?.Invoke(JsonConvert.DeserializeObject(request.downloadHandler.text));
				}
				else
				{
					Debug.LogError(request.error);
					failed?.Invoke();
				}
			});
		}

		public static IEnumerator GetHDAHeaderRoutine(string hdaName, Action<dynamic> completed, Action failed = null)
		{
			Uri uri = GetUri(hdaName);
			using (UnityWebRequest get = UnityWebRequest.Get(uri))
			{
				var request = get.SendWebRequest();
				while (!request.isDone)
					yield return null;
				if (get.result != UnityWebRequest.Result.Success)
				{
					if (failed == null)
						Debug.LogError(get.error);
					else
						failed?.Invoke();
				}
				completed?.Invoke(JsonConvert.DeserializeObject(get.downloadHandler.text));
			}
		}

		public static void ProcessHDAAsync(this HDAProcessorPreset preset, Action<ZipArchive> completed, Action failed = null, int timeout = 120)
		{
			ProcessHDAAsync(preset.hda, preset.parms, completed, failed, timeout);
		}
		
		public static void ProcessHDAAsync(string hda, IEnumerable<HouParm> parms, Action<ZipArchive> completed,
			Action failed = null, int timeout = 120)
		{
			Uri uri = GetUri(hda);
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			foreach (var parm in parms)
			{
				formData.Add(parm.formSection);
			}
		
			string downloadedFile = Path.Combine(Path.GetDirectoryName(Application.temporaryCachePath), "Temp", $"Harpoon_Response{DateTime.Now.Ticks}.zip");
			UnityWebRequest post = UnityWebRequest.Post(uri, formData);
			post.useHttpContinue = false;
			post.timeout = timeout;
			post.downloadHandler = new DownloadHandlerFile(downloadedFile);
			post.SendWebRequest((request) =>
			{
				if (request.result == UnityWebRequest.Result.Success)
				{
					using (var zipArchive = ZipFile.OpenRead(downloadedFile))
					{
						completed?.Invoke(zipArchive);
					}

					File.Delete(downloadedFile);
				}
				else
				{
					failed?.Invoke();
				}
			});
		}
		
		public static IEnumerator ProcessHDARoutine(string hda, IEnumerable<HouParm> parms, Action<ZipArchive> completed,
        			Action failed = null, int timeout = 120)
		{
			Uri uri = GetUri(hda);
			List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
			foreach (var parm in parms)
			{
				formData.Add(parm.formSection);
			}
			string downloadedFile = Path.Combine(Path.GetDirectoryName(Application.temporaryCachePath), "Temp", $"Harpoon_Response{DateTime.Now.Ticks}.zip");
			using (UnityWebRequest post = UnityWebRequest.Post(uri, formData))
            {
				post.useHttpContinue = false;
				post.timeout = timeout;
				post.downloadHandler = new DownloadHandlerFile(downloadedFile);
            	var request = post.SendWebRequest();
                while (!request.isDone)
	                yield return null;
            	if (post.result == UnityWebRequest.Result.Success)
            	{
	                using (var zipArchive = ZipFile.OpenRead(downloadedFile))
                    {
                        completed?.Invoke(zipArchive);
                    }
            	}
                else
                {
            		if (failed == null)
            			Debug.LogError(post.error);
            		else
            			failed?.Invoke();
                }
                File.Delete(downloadedFile);
            }
		}
	}
}
