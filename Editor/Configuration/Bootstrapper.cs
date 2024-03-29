using System.Threading.Tasks;
using Gemelo.Voice.Editor.Preview;
using UnityEditor;
using UnityEngine;

namespace Gemelo.Voice.Editor.Configuration
{
	[InitializeOnLoad]
	public static class Bootstrapper
	{
		const int WAIT_TIME_SECS = 10;
		static Bootstrapper() => InitializeLibrary();

		public static async void InitializeLibrary()
		{
			await WaitForStartup(WAIT_TIME_SECS);
			
			if (!Gemelo.Voice.Configuration.Exists())
			{
				Debug.LogError("Please use Tools->Gemelo.ai Voice->Configuration menu to setup API access");
				return;
			}

			var instance = VoicesDatabase.Load();
			
			if (instance.Validate())
				return;
			
#if !DEVELOPMENT 		
			if (ShowCreateDatabaseDialog())
			{
				await DatabaseInspector.UpdateLibraryInstance(instance);
				
				if (!instance.Validate())
					EditorUtility.DisplayDialog("Error", "Something went wrong!", "OK");
			}
#endif
		}
		
		private static async Task WaitForStartup(int secs)
		{
			while (EditorApplication.timeSinceStartup < secs)
			{
				await Task.Yield();
			}
		}
		
		private static bool ShowCreateDatabaseDialog()
		{
			return EditorUtility.DisplayDialog("Voices database creation", 
				"Voices database is not yet set!\n" +
				"Click OK to start downloading available previews.\n (Internet connection required)",
				"OK", "cancel");
		}
	}
}