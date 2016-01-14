using UnityEngine;
using System.Collections.Generic;

public class Configuration
{
	public const string GameFld = "W:/SteamLibrary/steamapps/common/Half-Life 2/", Mod = "hl2/";
	public static string GameResources = GameFld + Mod + "/materials/", PakArchive = string.Empty;

	public static Dictionary<string, Transform> Models;
	public static List<GameObject> Brushes;

	public const float WorldScale = 0.0254f;

	public static void Initialize (string LevelName)
	{
		PakArchive = Application.persistentDataPath + "/" + LevelName + "_pakFile/materials/";
		RenderSettings.ambientLight = new Color32 (150, 150, 150, 255);

		Models = new Dictionary<string, Transform> ();
		Brushes = new List<GameObject> ();
	}

	public static Vector3 SwapZY (Vector3 inp)
	{
		return new Vector3 (-inp.x, inp.z, -inp.y);
	}
}