using System.Threading.Tasks;
using Gemelo.Voice.Editor.Preview;
using UnityEditor;

namespace Gemelo.Voice.Editor.Configuration
{
	[InitializeOnLoad]
	public static class Bootstrapper
	{
		static Bootstrapper()
		{
			if (!Gemelo.Voice.Configuration.Exists())
			{
				ApiWindow.ShowWindow();
				return;
			}
			InitializeLibrary();
		}

		private static async void InitializeLibrary()
		{
			await DownloadPreviews();
			
			if (!VoicesDatabase.Validate())
				EditorUtility.DisplayDialog("Error", "Something went wrong!", "OK");
		}
		
		private static async Task DownloadPreviews()
		{
			if (ShowCreateDatabaseDialog())
			{
				var instance = VoicesDatabase.Load();
				await DatabaseInspector.UpdateLibraryInstance(instance);
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