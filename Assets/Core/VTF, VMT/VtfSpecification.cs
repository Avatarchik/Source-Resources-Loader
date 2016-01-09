using UnityEngine;
using System.Runtime.InteropServices;

public class VtfSpecification
{
    public enum VTFImageFormat : int
    {
        IMAGE_FORMAT_NONE = -1,
        IMAGE_FORMAT_RGBA8888 = 0,
        IMAGE_FORMAT_ABGR8888,
        IMAGE_FORMAT_RGB888,
        IMAGE_FORMAT_BGR888,
        IMAGE_FORMAT_RGB565,
        IMAGE_FORMAT_I8,
        IMAGE_FORMAT_IA88,
        IMAGE_FORMAT_P8,
        IMAGE_FORMAT_A8,
        IMAGE_FORMAT_RGB888_BLUESCREEN,
        IMAGE_FORMAT_BGR888_BLUESCREEN,
        IMAGE_FORMAT_ARGB8888,
        IMAGE_FORMAT_BGRA8888,
        IMAGE_FORMAT_DXT1,
        IMAGE_FORMAT_DXT3,
        IMAGE_FORMAT_DXT5,
        IMAGE_FORMAT_BGRX8888,
        IMAGE_FORMAT_BGR565,
        IMAGE_FORMAT_BGRX5551,
        IMAGE_FORMAT_BGRA4444,
        IMAGE_FORMAT_DXT1_ONEBITALPHA,
        IMAGE_FORMAT_BGRA5551,
        IMAGE_FORMAT_UV88,
        IMAGE_FORMAT_UVWQ8888,
        IMAGE_FORMAT_RGBA16161616F,
        IMAGE_FORMAT_RGBA16161616,
        IMAGE_FORMAT_UVLX8888
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct tagVTFHEADER
    {
        public int Signature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] Version;

        public uint HeaderSize;

        public ushort Width;
        public ushort Height;
        public uint Flags;

        public ushort Frames;
        public ushort FirstFrame;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Padding0;

        public Vector3 Reflectivity;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Padding1;

        public float BumpmapScale;

        public VTFImageFormat HighResImageFormat;
        public byte MipmapCount;

        public VTFImageFormat LowResImageFormat;
        public byte LowResImageWidth;
        public byte LowResImageHeight;

        public ushort Depth;
    }

    public static int[] uiBytesPerPixels = new int[]
    {
        4, 4, 3, 3, 2, 1,
        2, 1, 1, 3, 3, 4,
        4, 1, 1, 1, 4, 2,
        2, 2, 1, 2, 2, 4,
        8, 8, 4
    };
}
