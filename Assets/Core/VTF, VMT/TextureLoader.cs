using UnityEngine;
using System.IO;

public class TextureLoader : VtfSpecification
{
    private static CustomReader CRead;
    private static tagVTFHEADER VTF_Header;

    public static Texture2D Load(string TextureName)
    {
        if (!File.Exists(Configuration.GameFld + Configuration.Mod + "/materials/" + TextureName + ".vtf"))
            return new Texture2D(1, 1);

        CRead = new CustomReader(File.OpenRead(Configuration.GameFld + Configuration.Mod + "/materials/" + TextureName + ".vtf"));
        VTF_Header = CRead.ReadType<tagVTFHEADER>();

        Texture2D VTF_Texture = default(Texture2D); TextureFormat ImageFormat;
        long OffsetInFile = VTF_Header.Width * VTF_Header.Height * uiBytesPerPixels[(int)VTF_Header.HighResImageFormat];

        switch (VTF_Header.HighResImageFormat)
        {
            case VTFImageFormat.IMAGE_FORMAT_DXT1:
                OffsetInFile = ((VTF_Header.Width + 3) / 4) * ((VTF_Header.Height + 3) / 4) * 8;
                ImageFormat = TextureFormat.DXT1; break;

            case VTFImageFormat.IMAGE_FORMAT_DXT3:
            case VTFImageFormat.IMAGE_FORMAT_DXT5:
                OffsetInFile = ((VTF_Header.Width + 3) / 4) * ((VTF_Header.Height + 3) / 4) * 16;
                ImageFormat = TextureFormat.DXT5; break;

            case VTFImageFormat.IMAGE_FORMAT_RGB888:
            case VTFImageFormat.IMAGE_FORMAT_BGR888:
                ImageFormat = TextureFormat.RGB24; break;

            case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
                ImageFormat = TextureFormat.RGBA32; break;

            case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
                ImageFormat = TextureFormat.ARGB32; break;

            case VTFImageFormat.IMAGE_FORMAT_BGRA8888:
                ImageFormat = TextureFormat.BGRA32; break;

            default: return new Texture2D(1, 1);
        }

        VTF_Texture = new Texture2D(VTF_Header.Width, VTF_Header.Height, ImageFormat, false);
        byte[] VTF_File = CRead.GetBytes((int)OffsetInFile, CRead.InputStream.Length - OffsetInFile);

        if (VTF_Header.HighResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGR888)
        {
            for (int i = 0; i < VTF_File.Length - 1; i += 3)
            {
                byte Temp = VTF_File[i];
                VTF_File[i] = VTF_File[i + 2];
                VTF_File[i + 2] = Temp;
            }
        }

        VTF_Texture.LoadRawTextureData(VTF_File);

        Texture2D Mip_Texture = new Texture2D(VTF_Header.Width, VTF_Header.Height, TextureFormat.RGBA32, true);
        Mip_Texture.SetPixels32(VTF_Texture.GetPixels32());

        Mip_Texture.Apply(); Mip_Texture.Compress(false);
        Object.DestroyImmediate(VTF_Texture);

        CRead.Dispose();
        return Mip_Texture;
    }
}