using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;

namespace Harpoon.Utils
{
	public static class HarpoonUriBuilder
	{
		public static Uri Root => new Uri(HarpoonServerSettings.url);
	}

    public static class WebRequestUtility
    {
        public static void SendWebRequest(this UnityWebRequest request, Action<UnityWebRequest> completed)
		{
			EditorCoroutineUtility.StartCoroutineOwnerless(SendWebRequestRoutine(request, completed));
		}

		private static IEnumerator SendWebRequestRoutine(UnityWebRequest request, Action<UnityWebRequest> completed)
		{
			yield return request.SendWebRequest();
			completed?.Invoke(request);
		}
    }
}
