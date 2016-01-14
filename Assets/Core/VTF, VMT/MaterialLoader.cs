using UnityEngine;

using System.Collections.Generic;
using System.IO;

public class MaterialLoader
{
    private static Dictionary<string, string> Items;

    public static Material Load(string MaterialName)
    {
        string MaterialDestinationPath = Configuration.GameResources + MaterialName + ".vmt";

        if (File.Exists(Configuration.PakArchive + MaterialName + ".vmt"))
            MaterialDestinationPath = Configuration.PakArchive + MaterialName + ".vmt";

        if (string.IsNullOrEmpty(MaterialDestinationPath) || !File.Exists(MaterialDestinationPath))
		{
			Items = new Dictionary<string, string>();
            return new Material(GetShader());
		}

        Items = KeyValueParse.Load(File.ReadAllLines(MaterialDestinationPath));

        if (Items.ContainsKey("include"))
        {
            Load(Items["include"]
                 .Replace("materials/", "")
                 .Replace(".vmt", ""));
        }

        try
        {
            if (Items.ContainsKey("$fallbackmaterial"))
                Load(Items["$fallbackmaterial"]);
        }
        catch (FileNotFoundException)
        {
            if (Items.ContainsKey("$bottommaterial"))
                Load(Items["$bottommaterial"]);
        }

        Material material = new Material(GetShader());
        material.color = GetColor();

        string mainTexture = string.Empty;

        if (Items.ContainsKey("$basetexture"))
            mainTexture = Items["$basetexture"];

        else if (Items.ContainsKey("$baseTexture"))
            mainTexture = Items["$baseTexture"];

        mainTexture = mainTexture.Replace(".vtf", "");
        material.mainTexture = TextureLoader.Load(mainTexture);

        if (Items.ContainsKey("$surfaceprop"))
            material.name = Items["$surfaceprop"];

        return material;
    }

    private static Shader GetShader()
    {
        string[] ADictionary = { "$translucent", "$alphatest", "$Alphatest", "$AlphaTest" };

        foreach (string Key in ADictionary)
        {
            if (Items.ContainsKey(Key) && Items[Key] == "1")
				return Shader.Find("Legacy Shaders/Lightmapped/Alpha");
        }

		if (Items.ContainsKey("UnlitGeneric"))
			return Shader.Find("Mobile/Unlit (Supports Lightmap)");

        if (Items.ContainsKey("VertexLitGeneric"))
            return Shader.Find("Mobile/VertexLit");

		return Shader.Find("Lightmapped/Diffuse");
    }

    private static Color32 GetColor()
    {
		Color32 MaterialColor = new Color32(255, 255, 255, 255);

        if (Items.ContainsKey("$color"))
        {
            string[] color = Items["$color"].Replace('.'.ToString(), "").Trim('[', ']').Trim('{', '}').Trim().Split(' ');
            MaterialColor = new Color32(byte.Parse(color[0]), byte.Parse(color[1]), byte.Parse(color[2]), 255);
        }

		if (Items.ContainsKey("$alpha"))
			MaterialColor.a = (byte)(255 * float.Parse(Items["$color"]));

		return MaterialColor;
    }
}
