using UnityEngine;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

public class StudioMdlLoader : MdlSpecification
{
    private static CustomReader CRead;

    private static studiohdr_t MDL_Header;
    private static List<mstudiobodyparts_t> MDL_BodyParts;
    private static List<mstudiomodel_t> MDL_Models;
    private static List<mstudiomesh_t> MDL_Meshes;

    private static List<string> MDL_TDirectories;
    private static List<string> MDL_Textures;

    private static List<Transform> MDL_Bones;

    private static vertexFileHeader_t VVD_Header;
    private static List<mstudiovertex_t> VVD_Vertexes;
    private static List<vertexFileFixup_t> VVD_Fixups;

    private static FileHeader_t VTX_Header;
    private static List<MeshHeader_t> VTX_Meshes;

    private static GameObject ModelObject;

    private static void Clear()
    {
        MDL_BodyParts = new List<mstudiobodyparts_t>();
        MDL_Models = new List<mstudiomodel_t>();
        MDL_Meshes = new List<mstudiomesh_t>();

        MDL_TDirectories = new List<string>();
        MDL_Textures = new List<string>();

        MDL_Bones = new List<Transform>();

        VVD_Vertexes = new List<mstudiovertex_t>();
        VVD_Fixups = new List<vertexFileFixup_t>();

        VTX_Meshes = new List<MeshHeader_t>();
        ModelObject = null;
    }

    public static Transform Load(string ModelName)
    {
        Clear();

        string OpenPath = string.Concat(Configuration.GameFld, Configuration.Mod, ModelName);
        ModelObject = new GameObject(ModelName);

        if (!File.Exists(OpenPath + ".mdl"))
            return ModelObject.transform;

        CRead = new CustomReader(File.OpenRead(OpenPath + ".mdl"));
        MDL_Header = CRead.ReadType<studiohdr_t>();
        ParseMdlFile();

        CRead = new CustomReader(File.OpenRead(OpenPath + ".vvd"));
        VVD_Header = CRead.ReadType<vertexFileHeader_t>();
        ParseVvdFile();

        CRead = new CustomReader(File.OpenRead(OpenPath + ".dx90.vtx"));
        VTX_Header = CRead.ReadType<FileHeader_t>();
        ParseVtxFile();

        if (!Configuration.Models.ContainsKey(ModelObject.name))
            Configuration.Models.Add(ModelName, ModelObject.transform);

        CRead.Dispose();
        return ModelObject.transform;
    }

    private static void ParseMdlFile()
    {
        MDL_BodyParts.AddRange(CRead.ReadType<mstudiobodyparts_t>(MDL_Header.bodypart_count, MDL_Header.bodypart_offset));

        int ModelInputFilePosition = MDL_Header.bodypart_offset + MDL_BodyParts[0].modelindex;
        MDL_Models.AddRange(CRead.ReadType<mstudiomodel_t>(MDL_BodyParts[0].nummodels, ModelInputFilePosition));

        int MeshInputFilePosition = ModelInputFilePosition + MDL_Models[0].meshindex;
        MDL_Meshes.AddRange(CRead.ReadType<mstudiomesh_t>(MDL_Models[0].nummeshes, MeshInputFilePosition));

        List<mstudiotexture_t> MDL_TexturesInfo = new List<mstudiotexture_t>();
        MDL_TexturesInfo.AddRange(CRead.ReadType<mstudiotexture_t>(MDL_Header.texture_count, MDL_Header.texture_offset));

        for (int i = 0; i < MDL_Header.texture_count; i++)
        {
            int StringInputFilePosition = MDL_Header.texture_offset + (Marshal.SizeOf(typeof(mstudiotexture_t)) * i) + MDL_TexturesInfo[i].sznameindex;
            MDL_Textures.Add(CRead.ReadNullTerminatedString(StringInputFilePosition));
        }

        int[] TDirOffsets = CRead.ReadType<int>(MDL_Header.texturedir_count, MDL_Header.texturedir_offset);

        for (int i = 0; i < MDL_Header.texturedir_count; i++)
            MDL_TDirectories.Add(CRead.ReadNullTerminatedString(TDirOffsets[i]));

        List<mstudiobone_t> MDL_BonesInfo = new List<mstudiobone_t>();
        MDL_BonesInfo.AddRange(CRead.ReadType<mstudiobone_t>(MDL_Header.bone_count, MDL_Header.bone_offset));

        for (int i = 0; i < MDL_Header.bone_count; i++)
        {
            int StringInputFilePosition = MDL_Header.bone_offset + (Marshal.SizeOf(typeof(mstudiobone_t)) * i) + MDL_BonesInfo[i].sznameindex;

            GameObject BoneObject = new GameObject(CRead.ReadNullTerminatedString(StringInputFilePosition));
            BoneObject.transform.parent = ModelObject.transform;

            MDL_Bones.Add(BoneObject.transform);

            if (MDL_BonesInfo[i].parent >= 0)
                MDL_Bones[i].transform.parent = MDL_Bones[MDL_BonesInfo[i].parent].transform;
        }
    }

    private static void ParseVtxFile()
    {
        List<BoneWeight> pBoneWeight = new List<BoneWeight>();
        List<Vector3> pVertices = new List<Vector3>();
        List<Vector3> pNormals = new List<Vector3>();
        List<Vector2> pUvBuffer = new List<Vector2>();

        List<Material> pMaterials = new List<Material>();

        mstudiomodel_t pModel = MDL_Models[0]; mstudiomesh_t pStudioMesh;
        BodyPartHeader_t vBodypart = CRead.ReadType<BodyPartHeader_t>((long)VTX_Header.bodyPartOffset);

        int ModelInputFilePosition = VTX_Header.bodyPartOffset + vBodypart.modelOffset;
        ModelHeader_t vModel = CRead.ReadType<ModelHeader_t>((long)ModelInputFilePosition);

        int ModelLODInputFilePosition = ModelInputFilePosition + vModel.lodOffset;
        ModelLODHeader_t vLod = CRead.ReadType<ModelLODHeader_t>((long)ModelLODInputFilePosition);

        int MeshInputFilePosition = ModelLODInputFilePosition + vLod.meshOffset;
        VTX_Meshes.AddRange(CRead.ReadType<MeshHeader_t>(vLod.numMeshes, MeshInputFilePosition));

        for (int i = 0; i < pModel.numvertices; i++)
        {
            pBoneWeight.Add(GetBoneWeight(VVD_Vertexes[pModel.vertexindex + i].m_BoneWeights));

            pVertices.Add(Configuration.SwapZY(VVD_Vertexes[pModel.vertexindex + i].m_vecPosition * Configuration.WorldScale));
            pNormals.Add(Configuration.SwapZY(VVD_Vertexes[pModel.vertexindex + i].m_vecNormal));
            pUvBuffer.Add(VVD_Vertexes[pModel.vertexindex + i].m_vecTexCoord);
        }

        Mesh pMesh = new Mesh();
        ModelObject.AddComponent<MeshCollider>().sharedMesh = pMesh;

        pMesh.subMeshCount = vLod.numMeshes;
        pMesh.vertices = pVertices.ToArray();
        pMesh.normals = pNormals.ToArray();
        pMesh.uv = pUvBuffer.ToArray();

        pMesh.Optimize();

        if (MDL_Bones.Count > 1)
        {
            SkinnedMeshRenderer smr = ModelObject.AddComponent<SkinnedMeshRenderer>();
            Matrix4x4[] bindPoses = new Matrix4x4[MDL_Bones.Count];

            for (int i = 0; i < bindPoses.Length; i++)
                bindPoses[i] = MDL_Bones[i].worldToLocalMatrix * ModelObject.transform.localToWorldMatrix;

            pMesh.boneWeights = pBoneWeight.ToArray();
            pMesh.bindposes = bindPoses;

            smr.sharedMesh = pMesh;

            smr.bones = MDL_Bones.ToArray();
            smr.updateWhenOffscreen = true;
        }
        else
        {
            MeshFilter MeshFilter = ModelObject.AddComponent<MeshFilter>();
            ModelObject.AddComponent<MeshRenderer>();

            MeshFilter.sharedMesh = pMesh;
        }

        for (int i = 0; i < vLod.numMeshes; i++)
        {
            List<int> pIndices = new List<int>();

            List<StripGroupHeader_t> StripGroups = new List<StripGroupHeader_t>();
            int StripGroupFilePosition = MeshInputFilePosition + (Marshal.SizeOf(typeof(MeshHeader_t)) * i) + VTX_Meshes[i].stripGroupHeaderOffset;
            StripGroups.AddRange(CRead.ReadType<StripGroupHeader_t>(VTX_Meshes[i].numStripGroups, StripGroupFilePosition)); pStudioMesh = MDL_Meshes[i];

            for (int l = 0; l < VTX_Meshes[i].numStripGroups; l++)
            {
                List<Vertex_t> pVertexBuffer = new List<Vertex_t>();
                pVertexBuffer.AddRange(CRead.ReadType<Vertex_t>(StripGroups[l].numVerts, StripGroupFilePosition + (Marshal.SizeOf(typeof(StripGroupHeader_t)) * l) + StripGroups[l].vertOffset));

                List<ushort> Indices = new List<ushort>();
                Indices.AddRange(CRead.ReadType<ushort>(StripGroups[l].numIndices, StripGroupFilePosition + (Marshal.SizeOf(typeof(StripGroupHeader_t)) * l) + StripGroups[l].indexOffset));

                for (int n = 0; n < Indices.Count; n++)
                    pIndices.Add(pVertexBuffer[Indices[n]].origMeshVertID + pStudioMesh.vertexoffset);
            }

            pMesh.SetTriangles(pIndices.ToArray(), i);
            string MaterialPath = string.Empty;

            foreach (string TDir in MDL_TDirectories)
            {
                if (File.Exists(Configuration.GameResources + TDir + MDL_Textures[pStudioMesh.material] + ".vmt"))
                    MaterialPath = TDir + MDL_Textures[pStudioMesh.material];
            }

            if (!string.IsNullOrEmpty(MaterialPath))
                pMaterials.Add(MaterialLoader.Load(MaterialPath));
        }

        ModelObject.renderer.sharedMaterials = pMaterials.ToArray();
    }

    private static BoneWeight GetBoneWeight(mstudioboneweight_t mBoneWeight)
    {
        BoneWeight boneWeight = new BoneWeight();

        boneWeight.boneIndex0 = mBoneWeight.bone[0];
        boneWeight.boneIndex1 = mBoneWeight.bone[1];
        boneWeight.boneIndex2 = mBoneWeight.bone[2];

        boneWeight.weight0 = mBoneWeight.weight[0];
        boneWeight.weight1 = mBoneWeight.weight[1];
        boneWeight.weight2 = mBoneWeight.weight[2];

        return boneWeight;
    }

    private static void ParseVvdFile()
    {
        VVD_Fixups.AddRange(CRead.ReadType<vertexFileFixup_t>(VVD_Header.numFixups, VVD_Header.fixupTableStart));

        if (VVD_Header.numFixups == 0)
            VVD_Vertexes.AddRange(CRead.ReadType<mstudiovertex_t>(VVD_Header.numLODVertexes[0], VVD_Header.vertexDataStart));

        for (int i = 0; i < VVD_Header.numFixups; i++)
        {
            if (VVD_Fixups[i].lod >= 0)
                VVD_Vertexes.AddRange(CRead.ReadType<mstudiovertex_t>(VVD_Fixups[i].numVertexes, VVD_Header.vertexDataStart + (VVD_Fixups[i].sourceVertexID * 48)));
        }
    }
}
