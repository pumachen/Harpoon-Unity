using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using pxr;
using USD.NET;
using USD.NET.Unity;
using Unity.Formats.USD;
using UnityEditor.SceneTemplate;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Harpoon.USD
{
    public partial class USDExportContext
    {
        public Scene usdScene;
        public string usdFilePath => usdScene.FilePath;
        public string usdFileDir => Path.GetDirectoryName(usdScene.FilePath);
        public string usdFileName => Path.GetFileName(usdScene.FilePath);
        public string usdFileNameWithoutExtension  => Path.GetFileNameWithoutExtension(usdScene.FilePath);
        public Transform rootXform;
        public Dictionary<Material, SdfPath> usdMaterials = new();
        private BasisTransformation basisTransform = BasisTransformation.SlowAndSafe;
        private SdfPath materialBasePath = new("/Material");
        public USDMaterialBaker materialBaker;
        private TextureExportMode textureExportMode = TextureExportMode.MainTex;
        private bool varying => usdScene.Time != null;

        private Dictionary<Material, List<(SdfPath, CombineInstance)>> bakedGeometry = new();

        private void AddBakedGeometry(Material material, SdfPath path, CombineInstance geometry)
        {
            if (!bakedGeometry.TryGetValue(material, out var list))
            {
                list = new List<(SdfPath, CombineInstance)>();
                bakedGeometry.Add(material, list);
            }
            list.Add((path, geometry));
        }

        private bool slowAndSafeConversion => basisTransform is BasisTransformation.SlowAndSafe;

        public USDExportContext(Scene usdScene)
        {
            this.usdScene = usdScene;
        }
        
        public void WriteXform(
            SdfPath path,
            Transform xform)
        {
            SdfPath parentPath = path.GetParentPath();
            if (usdScene.GetPrimAtPath(path.GetParentPath()) == null)
            {
                WriteXform(parentPath, xform.parent);
            }
            XformSample usdXform = (XformSample)XformSample.FromTransform(xform);
            usdXform.ConvertTransform();
            usdScene.Write(path, usdXform);
        }

        public void WriteRenderer(Renderer renderer)
        {
            SdfPath path = rootXform == null
                ? new SdfPath(UnityTypeConverter.GetPath(renderer.transform))
                : new SdfPath(UnityTypeConverter.GetPath(renderer.transform, rootXform));
            WriteXform(path, renderer.transform);
            if (renderer is MeshRenderer)
            {
                WriteMeshRenderer(path, renderer as MeshRenderer);
            }
            else
            {
                Debug.LogWarning($"Skipping {renderer.gameObject}: {renderer.GetType()} is not supported.");
            }
        }
        
        private void WriteMeshRenderer(SdfPath path, MeshRenderer renderer)
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null)
            {
                Debug.LogWarning($"Skipping {renderer.gameObject}: Mesh not found.");
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
            
            bool mirror = renderer.transform.IsOddNegativeScale();

            for (int si = 0; si < subMeshCount; ++si)
            {
                CombineInstance instance = new CombineInstance();
                instance.transform = Matrix4x4.identity;
                instance.mesh = mesh;
                instance.subMeshIndex = si;

                Material material = materials[si];

                if (material == null)
                {
                    Debug.LogWarning($"Skipping sub mesh {si} of {renderer.gameObject}: Material not found.");
                    continue;
                }
                TextureExportMode mode = textureExportMode;
                Purpose purpose = Purpose.Default; 
                if (mode != TextureExportMode.None && materialBaker?[material.shader] != null)
                {
                    mode = TextureExportMode.None;
                    purpose = Purpose.Proxy;
                    SdfPath renderMeshPath = path.AppendChild(new TfToken($"Mesh_{GetHash(mesh)}_Submesh{si}_Render"));
                    CombineInstance previewInstance = new CombineInstance();
                    previewInstance.transform = renderer.transform.localToWorldMatrix;
                    previewInstance.mesh = mesh;
                    previewInstance.subMeshIndex = si;

                    AddBakedGeometry(material, renderMeshPath, previewInstance);
                }

                if (!usdMaterials.TryGetValue(material, out SdfPath usdMaterialPath))
                {
                    usdMaterialPath = materialBasePath.AppendChild(new TfToken($"Mat_{GetHash(material)}"));
                    usdMaterials.Add(material, usdMaterialPath);
                    WriteMaterial(usdMaterialPath, material, mode);
                }
                
                Mesh usdMesh = new Mesh();
                usdMesh.indexFormat = IndexFormat.UInt32;
                usdMesh.CombineMeshes(new[] { instance });
                SdfPath meshPath = path.AppendChild(new TfToken($"Mesh_{GetHash(mesh)}_Submesh{si}"));
                WriteMesh(meshPath, usdMesh, new [] { usdMaterialPath }, mirror, purpose);
            }
        }

        public void FinalizeExport()
        {
            WriteBakeGeometry();
            usdScene.Save();
            usdScene.Close();
        }

        string GetHash(Object obj)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localID))
            {
                return $"{guid}_{localID.ToString("x")}";
            }
            else
            {
                return obj.GetInstanceID().ToString("x");
            }
        }

        private void WriteAtlasGroup(Material material, List<(SdfPath, CombineInstance)> instances, SdfPath usdMaterialPath, string atlasPath)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(instances.Select(instance => instance.Item2).ToArray(), false);
            mesh.indexFormat = IndexFormat.UInt32;

            BakeUSDMaterial(usdMaterialPath, mesh, Matrix4x4.identity, material, atlasPath, 1024);
            mesh.uv = mesh.uv2;
            for (int i = 0; i < instances.Count; ++i)
            {
                var (path, instance) = instances[i];
                CombineInstance inst = new CombineInstance()
                {
                    mesh = mesh,
                    subMeshIndex = i,
                    transform = instance.transform.inverse
                };
                Mesh instanceMesh = new Mesh();
                instanceMesh.indexFormat = IndexFormat.UInt32;
                instanceMesh.CombineMeshes(new [] {inst});
                WriteMesh(path, instanceMesh, new [] { usdMaterialPath }, instance.transform.IsOddNegativeScale(), Purpose.Render);
                Object.DestroyImmediate(instanceMesh);
            }
            Object.DestroyImmediate(mesh);
        }

        private void WriteBakeGeometry()
        {
            List<(SdfPath, CombineInstance)> buffer = new ();
            foreach (var (material, instances) in bakedGeometry)
            {
                string materialHash = GetHash(material);
                
                uint atlasID = 0;
                uint totalTriangleCount = 0;
                for (int i = 0; i < instances.Count; ++i)
                {
                    CombineInstance instance = instances[i].Item2;
                    uint triangleCount = instance.mesh.GetIndexCount(instance.subMeshIndex) / 3;
                    if (triangleCount + totalTriangleCount > 10000)
                    {
                        string atlasPath = Path.Combine(Path.GetDirectoryName(usdScene.FilePath),
                            $"Textures/Tex_{usdFileNameWithoutExtension}_{materialHash}_{atlasID}.png");
                        SdfPath usdMaterialPath = materialBasePath.AppendChild(new TfToken($"Mat_{usdFileNameWithoutExtension}_{materialHash}_{atlasID}_Preview"));
                        WriteAtlasGroup(material, buffer, usdMaterialPath, atlasPath);
                        buffer.Clear();
                        totalTriangleCount = 0;
                        ++atlasID;
                    }
                    buffer.Add(instances[i]);
                    totalTriangleCount += triangleCount;
                }

                if (buffer.Count != 0)
                {
                    string atlasPath = Path.Combine(Path.GetDirectoryName(usdScene.FilePath),
                        $"Textures/Tex_{usdFileNameWithoutExtension}_{materialHash}_{atlasID}.png");
                    SdfPath usdMaterialPath = materialBasePath.AppendChild(new TfToken($"Mat_{usdFileNameWithoutExtension}_{materialHash}_{atlasID}_Preview"));
                    WriteAtlasGroup(material, buffer, usdMaterialPath, atlasPath);
                    buffer.Clear();
                }
            }
        }

        private void BakeUSDMaterial(SdfPath usdMaterialPath, Mesh mesh, Matrix4x4 trs, Material material,
            string texturePath, int resolution = 512)
        {
            Shader shader = material.shader;

            UnityPreviewSurfaceSample usdShader = new UnityPreviewSurfaceSample();
            usdShader.unity.shaderName = shader.name;
            usdShader.unity.shaderKeywords = material.shaderKeywords;
            string shaderPath = usdMaterialPath + "/PreviewSurface";
            MaterialSample usdMaterial = new MaterialSample();
            usdMaterial.surface.SetConnectedPath(shaderPath, "outputs:surface");
            usdScene.Write(usdMaterialPath, usdMaterial);
            
            XAtlas.Unwrap(mesh, new UnwrapParam());
            Material bakerMaterial = Object.Instantiate(material);
            bakerMaterial.shader = materialBaker[material.shader];
            RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 0, GraphicsFormat.R8G8B8A8_SRGB);
            using (CommandBuffer cmd = new CommandBuffer())
            {
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.black);
                for (int i = 0; i < mesh.subMeshCount; ++i)
                {
                    cmd.DrawMesh(mesh, trs, bakerMaterial, i);
                }
                Graphics.ExecuteCommandBuffer(cmd);
            }
            string usdTexPath = WriteTexture(
                rt,
                "_MainTex",
                shaderPath,
                texturePath,
                USDTexOutput.RGBA, 
                out Hash128 textureHash);
            RenderTexture.ReleaseTemporary(rt);

            usdShader.diffuseColor.SetConnectedPath(usdTexPath);

            var matPrim = usdScene.GetPrimAtPath(usdMaterialPath);
            VtDictionary customData = new VtDictionary();
            customData.SetValueAtPath("contentHash", new VtValue(textureHash.GetHashCode()));
            customData.SetValueAtPath("materialPath", new VtValue(AssetDatabase.GetAssetPath(material)));
            matPrim.SetCustomData(customData);
            usdScene.Write(shaderPath, usdShader);
        }

        private void WriteMesh(SdfPath path, Mesh mesh, SdfPath[] materials, bool reverse = false, Purpose purpose = Purpose.Default)
        {
            MeshSample usdMesh = new MeshSample();
            Vector3[] points = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            int[] triangles = mesh.triangles;
            if (reverse)
            {
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    (triangles[i], triangles[i + 1]) = (triangles[i + 1], triangles[i]);
                }
            }
            mesh.triangles = triangles;
            if (slowAndSafeConversion)
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    points[i] = UnityTypeConverter.ChangeBasis(points[i]);
                    if (normals != null && normals.Length == points.Length)
                    {
                        normals[i] = UnityTypeConverter.ChangeBasis(normals[i]);
                    }

                    if (tangents != null && tangents.Length == points.Length)
                    {
                        float w = tangents[i].w;
                        Vector3 t = UnityTypeConverter.ChangeBasis(tangents[i]);
                        tangents[i] = new Vector4(t.x, t.y, t.z, w);
                    }
                }

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    (triangles[i], triangles[i + 1]) = (triangles[i + 1], triangles[i]);
                }
            }

            usdMesh.points = points;
            usdMesh.normals = normals;
            usdMesh.tangents.SetValue(tangents);
            usdMesh.purpose = purpose;
            /*meshSample.colors.SetValue(mesh.colors);
            if (meshSample.colors.value != null && meshSample.colors.Length == 0)
            {
                meshSample.colors.value = null;
            }*/
            usdMesh.AddPrimvars(new List<string>() { "st" });
            usdMesh.ArbitraryPrimvars["st"].SetValue(mesh.uv);
            mesh.RecalculateBounds();
            Bounds bounds = mesh.bounds;
            usdMesh.extent = new Bounds(
                UnityTypeConverter.ChangeBasis(bounds.center),
                bounds.extents);
            usdMesh.SetTriangles(triangles);
            usdScene.Write(path, usdMesh);
            if (materials.Length == 0)
                return;

            var faceTable = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (slowAndSafeConversion)
                {
                    faceTable.TryAdd(new Vector3Int(triangles[i + 1], triangles[i], triangles[i + 2]), i / 3);
                }
                else
                {
                    faceTable.TryAdd(new Vector3Int(triangles[i], triangles[i + 1], triangles[i + 2]), i / 3);
                }
            }

            var usdPrim = usdScene.GetPrimAtPath(path);
            var usdGeomMesh = new UsdGeomMesh(usdPrim);

            for (int si = 0; si < mesh.subMeshCount; ++si)
            {
                int[] indices = mesh.GetTriangles(si);
                int[] faceIndices = new int[indices.Length / 3];
                for (int i = 0; i < indices.Length; i += 3)
                {
                    faceIndices[i / 3] = faceTable[new Vector3Int(indices[i], indices[i + 1], indices[i + 2])];
                }

                var vtIndices = UnityTypeConverter.ToVtArray(faceIndices);
                var subset = UsdGeomSubset.CreateUniqueGeomSubset(
                    usdGeomMesh,
                    new TfToken($"Submesh{si}"),
                    UsdGeomTokens.face, vtIndices,
                    new TfToken("materialBind"));

                if (materials.Length > si && materials[si] != null)
                {
                    SdfPath usdMaterialPath = materials[si];
                    MaterialSample.Bind(usdScene, subset.GetPath(), usdMaterialPath);
                }
            }
        }

        public enum TextureExportMode
        {
            None,
            MainTex,
            All
        }

        public void WriteMaterial(
            string path,
            Material material, TextureExportMode textureExportMode)
        {
            MaterialSample usdMaterial = new MaterialSample();
            string shaderPath = path + "/PreviewSurface";
            usdMaterial.surface.SetConnectedPath(shaderPath, "outputs:surface");
            usdScene.Write(path, usdMaterial);

            Shader shader = material.shader;

            UnityPreviewSurfaceSample usdShader = new UnityPreviewSurfaceSample();
            usdShader.unity.shaderName = shader.name;
            usdShader.unity.shaderKeywords = material.shaderKeywords;

            material.GetProperties(
                out usdShader.unity.floatArgs,
                out var integers,
                out usdShader.unity.colorArgs,
                out usdShader.unity.vectorArgs,
                out var textures);
            foreach (var integer in integers)
            {
                usdShader.unity.floatArgs[integer.Key] = integer.Value;
            }

            Hash128 textureHash = new Hash128();

            if (textureExportMode == TextureExportMode.None)
            {
                textures.Clear();
            }
            
            if (textureExportMode == TextureExportMode.MainTex)
            {
                textures.Clear();
                if (material.HasProperty("_MainTex") && material.mainTexture != null)
                {
                    textures.Add("_MainTex", material.mainTexture);
                }
            }

            foreach (var texture in textures)
            {
                var (name, tex) = (texture.Key, texture.Value);
                if (tex != null && tex is Texture2D)
                {
                    textureHash.Append(tex.imageContentsHash.GetHashCode());
                    string texPath = WriteTexture(
                        tex as Texture2D,
                        name,
                        shaderPath,
                        Path.GetDirectoryName(usdScene.FilePath) + $"/Textures/Tex_{GetHash(tex)}.png",
                        USDTexOutput.RGBA, out Hash128 texHash);
                    if (tex == material.mainTexture)
                    {
                        usdShader.diffuseColor.SetConnectedPath(texPath);
                    }
                }
            }
            
            var matPrim = usdScene.GetPrimAtPath(path);
            VtDictionary customData = new VtDictionary();
            customData.SetValueAtPath("materialPath", new VtValue(AssetDatabase.GetAssetPath(material)));
            customData.SetValueAtPath("contentHash", new VtValue(textureHash.GetHashCode()));

            matPrim.SetCustomData(customData);
            usdScene.Write(shaderPath, usdShader);
        }
    }
}