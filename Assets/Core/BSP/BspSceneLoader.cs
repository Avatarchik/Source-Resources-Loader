using UnityEngine;
using UnityEditor;
using Ionic.Zip;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.IO;
using System;

[CustomEditor(typeof(BspSceneLoader))]
public class BspEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        BspSceneLoader BspLoader = (BspSceneLoader)target;

        if (GUILayout.Button("Load Source Map"))
            BspLoader.Load();

        if (GUILayout.Button("Clear Source Map"))
            BspLoader.Clear();
    }
}

public class BspSceneLoader : MonoBehaviour
{
    private CustomReader CRead;
    public string LevelName;

    private BspSpecification.dheader_t BSP_Header;
    private List<string> BSP_Entities;

    private List<BspSpecification.dface_t> BSP_Faces;
    private List<BspSpecification.dmodel_t> BSP_Models;

    private List<BspSpecification.dDispVert> BSP_DispVerts;
    private List<BspSpecification.ddispinfo_t> BSP_DispInfo;

    private List<string> BSP_TexStrData;
    private List<BspSpecification.dtexdata_t> BSP_TexData;
    private List<BspSpecification.texinfo_t> BSP_TexInfo;

    private List<BspSpecification.dedge_t> BSP_Edges;
    private List<Vector3> BSP_Vertices;
    private List<int> BSP_Surfedges;

    private GameObject BSP_WorldSpawn;

    public void Clear()
    {
        BSP_Entities = new List<string>();
        BSP_TexStrData = new List<string>();

        BSP_Faces = new List<BspSpecification.dface_t>();
        BSP_Models = new List<BspSpecification.dmodel_t>();

        BSP_DispInfo = new List<BspSpecification.ddispinfo_t>();
        BSP_DispVerts = new List<BspSpecification.dDispVert>();

        BSP_TexData = new List<BspSpecification.dtexdata_t>();
        BSP_TexInfo = new List<BspSpecification.texinfo_t>();

        BSP_Edges = new List<BspSpecification.dedge_t>();
        BSP_Vertices = new List<Vector3>();
        BSP_Surfedges = new List<int>();

        if (BSP_WorldSpawn != null)
            DestroyImmediate(BSP_WorldSpawn);

        RenderSettings.skybox = null;
        Resources.UnloadUnusedAssets();
    }

    public void Load()
    {
        Clear();

        if (!File.Exists(Configuration.GameFld + Configuration.Mod + "/maps/" + LevelName + ".bsp"))
            throw new FileNotFoundException();

        Configuration.Initialize(LevelName);

        CRead = new CustomReader(File.OpenRead(Configuration.GameFld + Configuration.Mod + "/maps/" + LevelName + ".bsp"));
        BSP_Header = CRead.ReadType<BspSpecification.dheader_t>();

        if (BSP_Header.ident != (('P' << 24) + ('S' << 16) + ('B' << 8) + 'V'))
            throw new FileLoadException("Wrong magic number");

        if (BSP_Header.lumps[0].fileofs == 0)
        {
            Debug.Log("Found Left 4 Dead 2 header");
            for (int i = 0; i < BSP_Header.lumps.Length; i++)
            {
                BSP_Header.lumps[i].fileofs = BSP_Header.lumps[i].filelen;
                BSP_Header.lumps[i].filelen = BSP_Header.lumps[i].version;
            }
        }

        string input = Encoding.ASCII.GetString(CRead.GetBytes(BSP_Header.lumps[0].filelen, BSP_Header.lumps[0].fileofs));
        foreach (Match match in Regex.Matches(input, @"{[^}]*}", RegexOptions.IgnoreCase))
            BSP_Entities.Add(match.Value);

        if (BSP_Header.version <= 20) BSP_Faces.AddRange(CRead.ReadType<BspSpecification.dface_t>(BSP_Header.lumps[7].filelen / 56, BSP_Header.lumps[7].fileofs));
        else BSP_Faces.AddRange(CRead.ReadType<BspSpecification.dface_t>(BSP_Header.lumps[58].filelen / 56, BSP_Header.lumps[58].fileofs));

        BSP_Models.AddRange(CRead.ReadType<BspSpecification.dmodel_t>(BSP_Header.lumps[14].filelen / 48, BSP_Header.lumps[14].fileofs));

        BSP_DispInfo.AddRange(CRead.ReadType<BspSpecification.ddispinfo_t>(BSP_Header.lumps[26].filelen / 176, BSP_Header.lumps[26].fileofs));
        BSP_DispVerts.AddRange(CRead.ReadType<BspSpecification.dDispVert>(BSP_Header.lumps[33].filelen / 20, BSP_Header.lumps[33].fileofs));

        BSP_TexData.AddRange(CRead.ReadType<BspSpecification.dtexdata_t>(BSP_Header.lumps[2].filelen / 32, BSP_Header.lumps[2].fileofs));
        BSP_TexInfo.AddRange(CRead.ReadType<BspSpecification.texinfo_t>(BSP_Header.lumps[6].filelen / 72, BSP_Header.lumps[6].fileofs));

        int[] BSP_TexStrTable = CRead.ReadType<int>(BSP_Header.lumps[44].filelen / 4, BSP_Header.lumps[44].fileofs);
        BSP_TexStrData.AddRange(CRead.ReadNullTerminatedString(BSP_TexStrTable, BSP_Header.lumps[43].fileofs));

        BSP_Edges.AddRange(CRead.ReadType<BspSpecification.dedge_t>(BSP_Header.lumps[12].filelen / 4, BSP_Header.lumps[12].fileofs));
        BSP_Vertices.AddRange(CRead.ReadType<Vector3>(BSP_Header.lumps[3].filelen / 12, BSP_Header.lumps[3].fileofs));
        BSP_Surfedges.AddRange(CRead.ReadType<int>(BSP_Header.lumps[13].filelen / 4, BSP_Header.lumps[13].fileofs));

        UnpackPakFile(CRead.GetBytes(BSP_Header.lumps[40].filelen, BSP_Header.lumps[40].fileofs));

        for (int i = 0; i < BSP_Entities.Count; i++)
            LoadEntity(i);

        CRead.Dispose();
    }

    private void WorldSpawn()
    {
        BSP_WorldSpawn = new GameObject(LevelName);

        for (int i = 0; i < BSP_Models.Count; i++)
            CreateModel(i);

        if (BSP_DispInfo.Count != 0)
            CreateDispSurface();
    }

    private void LoadEntity(int id)
    {
        List<string> data = new List<string>();

        foreach (Match match in Regex.Matches(BSP_Entities[id], "\"[^\"]*\"", RegexOptions.IgnoreCase))
            data.Add(match.Value.Trim('"'));

        if (data[data.FindIndex(n => n == "classname") + 1] == "worldspawn")
        {
            WorldSpawn();

            CreateSkybox(data);
            LoadStaticProps();

            return;
        }

        if (data[0] == "model")
        {
            GameObject EntityObject = GameObject.Find(data[1]);
            EntityObject.AddComponent<SourceEntityInfo>().Configure(data);
        }
        else
        {
            GameObject EntityObject = new GameObject();
            EntityObject.transform.parent = BSP_WorldSpawn.transform;

            EntityObject.AddComponent<SourceEntityInfo>().Configure(data);
        }
    }

    private BspSpecification.face CreateFace(int Index)
    {
        List<Vector3> FaceVertices = new List<Vector3>();
        List<Vector2> TextureCoordinates = new List<Vector2>();
        List<Vector2> LightmapCoordinates = new List<Vector2>();

        int StartEdgeIndex = BSP_Faces[Index].firstedge;
        int EdgesCount = BSP_Faces[Index].numedges;

        BspSpecification.texinfo_t FaceTexInfo = BSP_TexInfo[BSP_Faces[Index].texinfo];
        BspSpecification.dtexdata_t FaceTexData = BSP_TexData[FaceTexInfo.texdata];

        for (int i = StartEdgeIndex; i < StartEdgeIndex + EdgesCount; i++)
            FaceVertices.Add(Configuration.SwapZY(BSP_Surfedges[i] > 0 ? BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].v[0]] : BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].v[1]]) * Configuration.WorldScale);

        List<int> Templist = new List<int>();
        for (int i = 1; i < FaceVertices.Count - 1; i++)
        {
            Templist.Add(0);
            Templist.Add(i);
            Templist.Add(i + 1);
        }

        Vector3 texs = Configuration.SwapZY(new Vector3(FaceTexInfo.textureVecs[0].x, FaceTexInfo.textureVecs[0].y, FaceTexInfo.textureVecs[0].z));
        Vector3 text = Configuration.SwapZY(new Vector3(FaceTexInfo.textureVecs[1].x, FaceTexInfo.textureVecs[1].y, FaceTexInfo.textureVecs[1].z));

        for (int i = 0; i < FaceVertices.Count; i++)
        {
            float s = (Vector3.Dot(FaceVertices[i], texs) + FaceTexInfo.textureVecs[0].w * Configuration.WorldScale) / (FaceTexData.width * Configuration.WorldScale);
            float t = (Vector3.Dot(FaceVertices[i], text) + FaceTexInfo.textureVecs[1].w * Configuration.WorldScale) / (FaceTexData.height * Configuration.WorldScale);
            TextureCoordinates.Add(new Vector2(s, t));
        }

        Vector3 l_s = Configuration.SwapZY(new Vector3(FaceTexInfo.lightmapVecs[0].x, FaceTexInfo.lightmapVecs[0].y, FaceTexInfo.lightmapVecs[0].z));
        Vector3 l_t = Configuration.SwapZY(new Vector3(FaceTexInfo.lightmapVecs[1].x, FaceTexInfo.lightmapVecs[1].y, FaceTexInfo.lightmapVecs[1].z));

        for (int i = 0; i < FaceVertices.Count; i++)
        {
            float s = (Vector3.Dot(FaceVertices[i], l_s) + (FaceTexInfo.lightmapVecs[0].w + 0.5f - BSP_Faces[Index].LightmapTextureMinsInLuxels[0]) * Configuration.WorldScale) / ((BSP_Faces[Index].LightmapTextureSizeInLuxels[0] + 1) * Configuration.WorldScale);
            float t = (Vector3.Dot(FaceVertices[i], l_t) + (FaceTexInfo.lightmapVecs[1].w + 0.5f - BSP_Faces[Index].LightmapTextureMinsInLuxels[1]) * Configuration.WorldScale) / ((BSP_Faces[Index].LightmapTextureSizeInLuxels[1] + 1) * Configuration.WorldScale);
            LightmapCoordinates.Add(new Vector2(s, t));
        }

        return new BspSpecification.face()
        {
            index = Index,

            points = FaceVertices.ToArray(),
            triangles = Templist.ToArray(),

            uv = TextureCoordinates.ToArray(),
            uv2 = LightmapCoordinates.ToArray(),

            lightMapW = BSP_Faces[Index].LightmapTextureSizeInLuxels[0] + 1,
            lightMapH = BSP_Faces[Index].LightmapTextureSizeInLuxels[1] + 1
        };
    }

    private void CreateModel(int modelIndex)
    {
        Dictionary<int, List<int>> SubMeshData = new Dictionary<int, List<int>>();
        GameObject Model = new GameObject("*" + modelIndex);
        Model.transform.parent = BSP_WorldSpawn.transform;

        int FirstFace = BSP_Models[modelIndex].firstface;
        int Faces = BSP_Models[modelIndex].numfaces;

        for (int i = FirstFace; i < FirstFace + Faces; i++)
        {
            if (!SubMeshData.ContainsKey(BSP_TexData[BSP_TexInfo[BSP_Faces[i].texinfo].texdata].nameStringTableID))
                SubMeshData.Add(BSP_TexData[BSP_TexInfo[BSP_Faces[i].texinfo].texdata].nameStringTableID, new List<int>());

            SubMeshData[BSP_TexData[BSP_TexInfo[BSP_Faces[i].texinfo].texdata].nameStringTableID].Add(i);
        }

        for (int i = 0; i < BSP_TexStrData.Count; i++)
        {
            if (!SubMeshData.ContainsKey(i))
                continue;

            List<BspSpecification.face> FaceList = new List<BspSpecification.face>();

            List<Vector3> Vertices = new List<Vector3>();
            List<int> Triangles = new List<int>();
            List<Vector2> UV = new List<Vector2>();

            for (int k = 0; k < SubMeshData[i].Count; k++)
            {
                if (BSP_Faces[SubMeshData[i][k]].dispinfo == -1)
                {
                    BspSpecification.face f = CreateFace(SubMeshData[i][k]);
                    int PointOffset = Vertices.Count;

                    for (int j = 0; j < f.triangles.Length; j++)
                        Triangles.Add(f.triangles[j] + PointOffset);

                    Vertices.AddRange(f.points);
                    UV.AddRange(f.uv);
                    FaceList.Add(f);
                }
            }

            GameObject MeshObject = new GameObject(BSP_TexStrData[i]);
            MeshObject.transform.parent = Model.transform;

            MeshObject.isStatic = true;

            MeshRenderer MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
            MeshFilter MeshFilter = MeshObject.AddComponent<MeshFilter>();

            List<Vector2> UV2 = new List<Vector2>();
            Texture2D LightMap = new Texture2D(1, 1);

            CreateLightMap(FaceList, ref LightMap, ref UV2);

            MeshRenderer.sharedMaterial = MaterialLoader.Load(BSP_TexStrData[i]);
            MeshRenderer.sharedMaterial.SetTexture("_LightMap", LightMap);

            MeshFilter.sharedMesh = new Mesh();

            MeshCollider MeshCollider 
                = MeshObject.AddComponent<MeshCollider>();

            if (BSP_TexStrData[i].Contains("TOOLS/"))
            {
                MeshCollider.enabled = false;
                MeshRenderer.enabled = false;
            }

            MeshFilter.sharedMesh.vertices = Vertices.ToArray();
            MeshFilter.sharedMesh.triangles = Triangles.ToArray();

            MeshFilter.sharedMesh.uv = UV.ToArray();
            MeshFilter.sharedMesh.uv2 = UV2.ToArray();

            MeshFilter.sharedMesh.RecalculateNormals();
            MeshFilter.sharedMesh.Optimize();
        }
    }

    private void CreateDispSurface()
    {
        Dictionary<int, List<int>> SubMeshData = new Dictionary<int, List<int>>();

        for (int i = 0; i < BSP_DispInfo.Count; i++)
        {
            if (!SubMeshData.ContainsKey(BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].texinfo].texdata].nameStringTableID))
                SubMeshData.Add(BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].texinfo].texdata].nameStringTableID, new List<int>());

            SubMeshData[BSP_TexData[BSP_TexInfo[BSP_Faces[BSP_DispInfo[i].MapFace].texinfo].texdata].nameStringTableID].Add(BSP_DispInfo[i].MapFace);
        }

        for (int i = 0; i < BSP_TexStrData.Count; i++)
        {
            if (!SubMeshData.ContainsKey(i))
                continue;

            List<BspSpecification.face> FaceList = new List<BspSpecification.face>();

            List<Vector3> Vertices = new List<Vector3>();
            List<Color32> Colors = new List<Color32>();
            List<int> Triangles = new List<int>();
            List<Vector2> UV = new List<Vector2>();

            for (int k = 0; k < SubMeshData[i].Count; k++)
            {
                if (BSP_Faces[SubMeshData[i][k]].dispinfo != -1)
                {
                    BspSpecification.face f = CreateDispSurface(BSP_Faces[SubMeshData[i][k]].dispinfo);
                    int PointOffset = Vertices.Count;

                    for (int j = 0; j < f.triangles.Length; j++)
                        Triangles.Add(f.triangles[j] + PointOffset);

                    Vertices.AddRange(f.points);
                    Colors.AddRange(f.colors);
                    UV.AddRange(f.uv);
                    FaceList.Add(f);
                }
            }

            GameObject MeshObject = new GameObject(BSP_TexStrData[i]);
            MeshObject.transform.localScale = new Vector3(1, 1, -1);
            MeshObject.transform.parent = BSP_WorldSpawn.transform;

            MeshObject.isStatic = true;

            MeshRenderer MeshRenderer = MeshObject.AddComponent<MeshRenderer>();
            MeshFilter MeshFilter = MeshObject.AddComponent<MeshFilter>();

            List<Vector2> UV2 = new List<Vector2>();
            Texture2D LightMap = new Texture2D(1, 1);

            CreateLightMap(FaceList, ref LightMap, ref UV2);

            MeshRenderer.sharedMaterial = MaterialLoader.Load(BSP_TexStrData[i]);
            MeshRenderer.sharedMaterial.SetTexture("_LightMap", LightMap);

            MeshFilter.sharedMesh = new Mesh();
            MeshObject.AddComponent<MeshCollider>();

            MeshFilter.sharedMesh.vertices = Vertices.ToArray();
            MeshFilter.sharedMesh.triangles = Triangles.ToArray();
            MeshFilter.sharedMesh.colors32 = Colors.ToArray();

            MeshFilter.sharedMesh.uv = UV.ToArray();
            MeshFilter.sharedMesh.uv2 = UV2.ToArray();

            MeshFilter.sharedMesh.RecalculateNormals();
            MeshFilter.sharedMesh.Optimize();
        }
    }

    private BspSpecification.face CreateDispSurface(int dispIndex)
    {
        List<Vector3> FaceVertices = new List<Vector3>();
        List<Color32> VertColors = new List<Color32>();

        List<Vector3> DispVertices = new List<Vector3>();
        List<int> DispIndices = new List<int>();

        List<Vector2> TextureCoordinates = new List<Vector2>();
        List<Vector2> LightmapCoordinates = new List<Vector2>();

        BspSpecification.dface_t FaceInfo = BSP_Faces[BSP_DispInfo[dispIndex].MapFace];

        BspSpecification.texinfo_t FaceTexInfo = BSP_TexInfo[FaceInfo.texinfo];
        BspSpecification.dtexdata_t FaceTexData = BSP_TexData[FaceTexInfo.texdata];

        Vector3 texs = new Vector3(FaceTexInfo.textureVecs[0].x, FaceTexInfo.textureVecs[0].y, FaceTexInfo.textureVecs[0].z);
        Vector3 text = new Vector3(FaceTexInfo.textureVecs[1].x, FaceTexInfo.textureVecs[1].y, FaceTexInfo.textureVecs[1].z);

        for (int i = FaceInfo.firstedge; i < (FaceInfo.firstedge + FaceInfo.numedges); i++)
            FaceVertices.Add((BSP_Surfedges[i] > 0 ? BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].v[0]] : BSP_Vertices[BSP_Edges[Mathf.Abs(BSP_Surfedges[i])].v[1]]) * Configuration.WorldScale);

        int MinIndex = 0;
        float MinDist = 1.0e9f;

        for (int i = 0; i < 4; i++)
        {
            float Distance = Vector3.Distance(FaceVertices[i], BSP_DispInfo[dispIndex].startPosition * Configuration.WorldScale);

            if (Distance < MinDist)
            {
                MinDist = Distance;
                MinIndex = i;
            }
        }

        for (int i = 0; i < MinIndex; i++)
        {
            Vector3 Temp = FaceVertices[0];
            FaceVertices[0] = FaceVertices[1];
            FaceVertices[1] = FaceVertices[2];
            FaceVertices[2] = FaceVertices[3];
            FaceVertices[3] = Temp;
        }

        Vector3 LeftEdge = FaceVertices[1] - FaceVertices[0];
        Vector3 RightEdge = FaceVertices[2] - FaceVertices[3];

        int NumEdgeVertices = (1 << BSP_DispInfo[dispIndex].power) + 1;
        float SubdivideScale = 1.0f / (NumEdgeVertices - 1);

        float LightDeltaU = (1f) / (NumEdgeVertices - 1);
        float LightDeltaV = (1f) / (NumEdgeVertices - 1);

        Vector3 LeftEdgeStep = LeftEdge * SubdivideScale;
        Vector3 RightEdgeStep = RightEdge * SubdivideScale;

        for (int i = 0; i < NumEdgeVertices; i++)
        {
            Vector3 LeftEnd = LeftEdgeStep * i;
            LeftEnd += FaceVertices[0];

            Vector3 RightEnd = RightEdgeStep * i;
            RightEnd += FaceVertices[3];

            Vector3 LeftRightSeg = RightEnd - LeftEnd;
            Vector3 LeftRightStep = LeftRightSeg * SubdivideScale;

            for (int j = 0; j < NumEdgeVertices; j++)
            {
                int DispVertIndex = BSP_DispInfo[dispIndex].DispVertStart + (i * NumEdgeVertices + j);
                BspSpecification.dDispVert DispVertInfo = BSP_DispVerts[DispVertIndex];

                Vector3 FlatVertex = LeftEnd + (LeftRightStep * j);
                Vector3 DispVertex = DispVertInfo.vec * (DispVertInfo.dist * Configuration.WorldScale);
                DispVertex += FlatVertex;

                float s = (Vector3.Dot(FlatVertex, texs) + FaceTexInfo.textureVecs[0].w * Configuration.WorldScale) / (FaceTexData.width * Configuration.WorldScale);
                float t = (Vector3.Dot(FlatVertex, text) + FaceTexInfo.textureVecs[1].w * Configuration.WorldScale) / (FaceTexData.height * Configuration.WorldScale);
                TextureCoordinates.Add(new Vector2(s, t));

                float l_s = (LightDeltaU * j * FaceInfo.LightmapTextureSizeInLuxels[0] + 0.5f) / (FaceInfo.LightmapTextureSizeInLuxels[0] + 1);
                float l_t = (LightDeltaV * i * FaceInfo.LightmapTextureSizeInLuxels[1] + 0.5f) / (FaceInfo.LightmapTextureSizeInLuxels[1] + 1);
                LightmapCoordinates.Add(new Vector2(l_s, l_t));

                VertColors.Add(new Color32(0, 0, 0, (byte)(DispVertInfo.alpha / 255.0f)));
                DispVertices.Add(new Vector3(-DispVertex.x, DispVertex.z, DispVertex.y));
            }
        }

        for (int i = 0; i < NumEdgeVertices - 1; i++)
        {
            for (int j = 0; j < NumEdgeVertices - 1; j++)
            {
                int Index = i * NumEdgeVertices + j;

                if ((Index % 2) == 1)
                {
                    DispIndices.Add(Index);
                    DispIndices.Add(Index + 1);
                    DispIndices.Add(Index + NumEdgeVertices);
                    DispIndices.Add(Index + 1);
                    DispIndices.Add(Index + NumEdgeVertices + 1);
                    DispIndices.Add(Index + NumEdgeVertices);
                }
                else
                {
                    DispIndices.Add(Index);
                    DispIndices.Add(Index + NumEdgeVertices + 1);
                    DispIndices.Add(Index + NumEdgeVertices);
                    DispIndices.Add(Index);
                    DispIndices.Add(Index + 1);
                    DispIndices.Add(Index + NumEdgeVertices + 1);
                }
            }
        }

        return new BspSpecification.face()
        {
            index = BSP_DispInfo[dispIndex].MapFace,

            points = DispVertices.ToArray(),
            triangles = DispIndices.ToArray(),
            colors = VertColors.ToArray(),

            uv = TextureCoordinates.ToArray(),
            uv2 = LightmapCoordinates.ToArray(),

            lightMapW = FaceInfo.LightmapTextureSizeInLuxels[0] + 1,
            lightMapH = FaceInfo.LightmapTextureSizeInLuxels[1] + 1
        };
    }

    private void CreateLightMap(List<BspSpecification.face> inpFaces, ref Texture2D LightMap, ref List<Vector2> LightMapUV)
    {
        Texture2D[] LightMaps = new Texture2D[inpFaces.Count];

        for (int i = 0; i < inpFaces.Count; i++)
        {
            LightMaps[i] = new Texture2D(inpFaces[i].lightMapW, inpFaces[i].lightMapH, TextureFormat.RGB24, false);

            Color32[] TexPixels = new Color32[inpFaces[i].lightMapW * inpFaces[i].lightMapH];
            int LightMapOffset = BSP_Faces[inpFaces[i].index].lightofs;

            if (LightMapOffset == -1) continue;
            for (int n = 0; n < TexPixels.Length; n++)
            {
                BspSpecification.ColorRGBExp32 ColorRGBExp32 = TexLightToLinear(LightMapOffset + (n * 4));
                TexPixels[n] = new Color32(ColorRGBExp32.r, ColorRGBExp32.g, ColorRGBExp32.b, 255);
            }

            LightMaps[i].SetPixels32(TexPixels);
        }

        Rect[] UVs2 = LightMap.PackTextures(LightMaps, 1);

        for (int i = 0; i < inpFaces.Count; i++)
        {
            for (int l = 0; l < inpFaces[i].uv2.Length; l++)
                LightMapUV.Add(new Vector2((inpFaces[i].uv2[l].x * UVs2[i].width) + UVs2[i].x, (inpFaces[i].uv2[l].y * UVs2[i].height) + UVs2[i].y));
        }
    }

    private BspSpecification.ColorRGBExp32 TexLightToLinear(long Offset)
    {
        if (BSP_Header.version <= 20) Offset += BSP_Header.lumps[8].fileofs;
        else Offset += BSP_Header.lumps[53].fileofs;

        BspSpecification.ColorRGBExp32 ColorRGBExp32 = CRead.ReadType<BspSpecification.ColorRGBExp32>(Offset);

        ColorRGBExp32.r = (byte)Mathf.Clamp(ColorRGBExp32.r * Mathf.Pow(2, ColorRGBExp32.exponent), 0, 255);
        ColorRGBExp32.g = (byte)Mathf.Clamp(ColorRGBExp32.g * Mathf.Pow(2, ColorRGBExp32.exponent), 0, 255);
        ColorRGBExp32.b = (byte)Mathf.Clamp(ColorRGBExp32.b * Mathf.Pow(2, ColorRGBExp32.exponent), 0, 255);

        return ColorRGBExp32;
    }

    private void LoadStaticProps()
    {
        CRead.BR().BaseStream.Position = BSP_Header.lumps[35].fileofs;

        GameObject StaticProps = new GameObject(BSP_WorldSpawn.name + "_props");
        StaticProps.transform.parent = BSP_WorldSpawn.transform;

        int GameLumpCount = CRead.BR().ReadInt32();

        BspSpecification.dgamelump_t[] GameLumps =
            CRead.ReadType<BspSpecification.dgamelump_t>(GameLumpCount, 0);

        for (int i = 0; i < GameLumpCount; i++)
        {
            if (GameLumps[i].id == 1936749168)
            {
                CRead.BR().BaseStream.Position = GameLumps[i].fileofs;

                int DictEntries = CRead.BR().ReadInt32();
                string[] Entries = new string[DictEntries];

                for (int l = 0; l < DictEntries; l++)
                {
                    Entries[l] = new string(CRead.BR().ReadChars(128));

                    if (Entries[l].Contains(Convert.ToChar(0)))
                        Entries[l] = Entries[l].Remove(Entries[l].IndexOf(Convert.ToChar(0)));
                }

                int LeafEntries = CRead.BR().ReadInt32();
                CRead.ReadType<ushort>(LeafEntries, 0);

                int nStaticProps = CRead.BR().ReadInt32();
                for (int l = 0; l < nStaticProps; l++)
                {
                    BspGameLump.StaticPropLumpV4_t StaticPropLump_t =
                        CRead.ReadType<BspGameLump.StaticPropLumpV4_t>();

                    switch (GameLumps[i].version)
                    {
                        case 5: CRead.ReadType<BspGameLump.StaticPropLumpV5_t>(); break;
                        case 6: CRead.ReadType<BspGameLump.StaticPropLumpV6_t>(); break;
                        case 7: CRead.ReadType<BspGameLump.StaticPropLumpV7_t>(); break;
                        case 8: CRead.ReadType<BspGameLump.StaticPropLumpV8_t>(); break;
                        case 9: CRead.ReadType<BspGameLump.StaticPropLumpV9_t>(); break;
                        case 10: CRead.ReadType<BspGameLump.StaticPropLumpV10_t>(); break;
                    }

                    Transform mdlTransform = default(Transform);
                    string StaticPropName = Entries[StaticPropLump_t.m_PropType].Replace(".mdl", "");

                    if (Configuration.Models.ContainsKey(StaticPropName))
                        mdlTransform = Instantiate(Configuration.Models[StaticPropName]) as Transform;
                    else
                        mdlTransform = StudioMdlLoader.Load(StaticPropName);

                    mdlTransform.gameObject.isStatic = true;
                    mdlTransform.localPosition = Configuration.SwapZY(StaticPropLump_t.m_Origin) * Configuration.WorldScale;

                    Vector3 mdlRotation = new Vector3(StaticPropLump_t.m_Angles.z, -StaticPropLump_t.m_Angles.y, StaticPropLump_t.m_Angles.x);
                    mdlTransform.eulerAngles = mdlRotation;

                    mdlTransform.parent = StaticProps.transform;
                }
            }
        }
    }

    private void CreateSkybox(List<string> data)
    {
        string skyname = data[data.FindIndex(n => n == "skyname") + 1].Replace("_hdr", "");
        Material SkyMaterial = new Material(Shader.Find("Mobile/Skybox"));

        foreach (string alpString in new string[] { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex" })
        {
            SkyMaterial.SetTextureScale(alpString, new Vector2(1, -1));
            SkyMaterial.SetTextureOffset(alpString, new Vector2(0, 1));
        }

        Texture2D FrontTex = TextureLoader.Load("skybox/" + skyname + "rt");
        FrontTex.wrapMode = TextureWrapMode.Clamp;

        Texture2D BackTex = TextureLoader.Load("skybox/" + skyname + "lf");
        BackTex.wrapMode = TextureWrapMode.Clamp;

        Texture2D LeftTex = TextureLoader.Load("skybox/" + skyname + "ft");
        LeftTex.wrapMode = TextureWrapMode.Clamp;

        Texture2D RightTex = TextureLoader.Load("skybox/" + skyname + "bk");
        RightTex.wrapMode = TextureWrapMode.Clamp;

        Texture2D UpTex = TextureLoader.Load("skybox/" + skyname + "up");
        UpTex.wrapMode = TextureWrapMode.Clamp;

        SkyMaterial.SetTexture("_FrontTex", FrontTex);
        SkyMaterial.SetTexture("_BackTex", BackTex);
        SkyMaterial.SetTexture("_LeftTex", LeftTex);
        SkyMaterial.SetTexture("_RightTex", RightTex);
        SkyMaterial.SetTexture("_UpTex", UpTex);

        RenderSettings.skybox = SkyMaterial;
    }

    private void UnpackPakFile(byte[] BSP_PakFile)
    {
        if (Directory.Exists(Application.persistentDataPath + "/" + LevelName + "_pakFile"))
            return;

        File.WriteAllBytes(Application.persistentDataPath + "/" + LevelName + "_pakFile.zip", BSP_PakFile);
        Directory.CreateDirectory(Application.persistentDataPath + "/" + LevelName + "_pakFile");

        ZipFile PakFile = ZipFile.Read(Application.persistentDataPath + "/" + LevelName + "_pakFile.zip");
        PakFile.ExtractAll(Application.persistentDataPath + "/" + LevelName + "_pakFile"); PakFile.Dispose();

        File.Delete(Application.persistentDataPath + "/" + LevelName + "_pakFile.zip");
    }
}
