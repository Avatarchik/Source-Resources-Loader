using UnityEngine;
using System.Collections.Generic;

public class Configuration
{
    public const string GameFld = "W:/SteamLibrary/steamapps/common/Counter-Strike Source/", Mod = "cstrike/";
    public static string GameResources = string.Empty, PakArchive = string.Empty;

    public static Dictionary<string, Transform> Models;

    public const float WorldScale = 0.0254f;

    public static void Initialize(string LevelName)
    {
        GameResources = GameFld + Mod + "/materials/";
        PakArchive = Application.persistentDataPath + "/" + LevelName + "_pakFile/materials/";

        RenderSettings.ambientLight = new Color32(150, 150, 150, 255);

        Models = new Dictionary<string, Transform>();
    }

    public static Vector3 SwapZY(Vector3 inp)
    {
        return new Vector3(-inp.x, inp.z, -inp.y);
    }
}