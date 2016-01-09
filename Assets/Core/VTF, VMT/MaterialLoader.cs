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

        if (string.IsNullOrEmpty(MaterialDestinationPath))
            return null;

        Items = KeyValueParse.Load(File.ReadAllLines(MaterialDestinationPath));

        if (Items.ContainsKey("include"))
        {
            Load(Items["include"]
                 .Replace("materials/", "")
                 .Replace(".vmt", ""));
        }

        if (Items.ContainsKey("$fallbackmaterial"))
            Load(Items["$fallbackmaterial"]);

        Material material = new Material(GetShader());
        material.color = GetColor();

        string mainTexture = string.Empty;

        if (Items.ContainsKey("$basetexture"))
            mainTexture = Items["$basetexture"];

        else if (Items.ContainsKey("$baseTexture"))
            mainTexture = Items["$baseTexture"];

        material.mainTexture = TextureLoader.Load(mainTexture);

        if (Items.ContainsKey("$surfaceprop"))
            material.name = Items["$surfaceprop"];

        return material;
    }

    private static Shader GetShader()
    {
        string[] ADictionary = { "$translucent", "$alphatest", "$AlphaTest" };

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
        if (Items.ContainsKey("$color"))
        {
            string[] color = Items["$color"].Trim('[', ']').Trim('{', '}').Split(' ');
            return new Color32(byte.Parse(color[0]), byte.Parse(color[1]), byte.Parse(color[2]), 255);
        }

        return new Color32(200, 200, 200, 255);
    }
}
