using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using pxr;
using USD.NET;
using USD.NET.Unity;
using Unity.Formats.USD;
using UnityEditor;
using UnityEngine.Rendering;

namespace Harpoon.USD
{
    public partial class USDImportContext
    {
        public Scene usdScene;
        public string usdFileDir => Path.GetDirectoryName(usdScene.FilePath);
        public string usdFileName => Path.GetFileName(usdScene.FilePath);
        public string usdFileNameWithoutExtension  => Path.GetFileNameWithoutExtension(usdScene.FilePath);
        public Transform rootXform;
        public SdfPath usdRootPath;
        public string meshImportDir;
        public string materialImportDir;
        public string textureImportDir;
        public Shader defaultShader;
        public Dictionary<SdfPath, Material> materials = new();

        public USDImportContext(Scene usdScene, string usdRootPath = "/")
        {
            this.usdScene = usdScene;
            this.usdRootPath = new SdfPath(usdRootPath);
            usdScene.Time = 1.0f;
            usdScene.SetInterpolation(Scene.InterpolationMode.Linear);
        }
        
        public GameObject ImportGameObject(UsdPrim prim = null, Transform parent = null)
        {
            if (prim == null)
            {
                prim = usdScene.GetPrimAtPath("/");
            }

            if (parent == null)
                parent = new GameObject(usdFileNameWithoutExtension).transform;

            Transform xform = parent;

            switch (prim.GetTypeName())
            {
                case "Xform":
                {
                    XformSample usdXform = new XformSample();
                    usdScene.Read(prim.GetPath(), usdXform);
                    xform = ImportXform(prim, usdXform, parent);
                    break;
                }
                case "Mesh":
                {
                    MeshSample usdMesh = new MeshSample();
                    usdScene.Read(prim.GetPath(), usdMesh);
                    MeshFilter mf = ImportMesh(prim, usdMesh, parent);
                    xform = mf.transform;
                    break;
                }
                default:
                {
                    //Debug.LogWarning($"Skipping {prim.GetPath()}: Prim type {prim.GetTypeName()} not support.");
                    break;
                }
            }

            foreach (var child in prim.GetAllChildren())
            {
                ImportGameObject(child, xform);
            }
            
            return xform.gameObject;
        }

        public Transform ImportXform(UsdPrim prim, XformableSample usdXform, Transform parent)
        {
            GameObject go  = new GameObject(prim.GetName());
            Transform xform = go.transform;
            if (parent != null)
            {
                xform.SetParent(parent);
            }
            usdXform.ConvertTransform();
            Vector3 localPosition = Vector3.zero;
            Quaternion localRotation = Quaternion.identity;
            Vector3 localScale = Vector3.one;
            UnityTypeConverter.ExtractTrs(usdXform.transform, ref localPosition, ref localRotation, ref localScale);
            xform.localPosition = localPosition;
            xform.localRotation = localRotation;
            xform.localScale = localScale;
            return xform;
        }

        public MeshFilter ImportMesh(UsdPrim prim, MeshSample usdMesh, Transform parent)
        {
            Transform xform = ImportXform(prim, usdMesh, parent);
            GameObject go = xform.gameObject;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            var geomSubsets = MeshImporter.ReadGeomSubsets(usdScene, prim.GetPath());
            Mesh mesh = USDMesh.BuildMesh(usdMesh, geomSubsets);
            mesh.name = Path.GetFileName(prim.GetPath());
            if (!string.IsNullOrEmpty(meshImportDir))
            {
                 string meshDirFS = Path.Combine(Path.GetDirectoryName(Application.dataPath), meshImportDir);
                 string meshPath = Path.Combine(meshImportDir, $"{mesh.name}.asset");
                 if (!Directory.Exists(meshDirFS))
                 {
                     Directory.CreateDirectory(meshDirFS);
                 }
                 AssetDatabase.Refresh();
                 AssetDatabase.CreateAsset(mesh, meshPath); 
                 mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            }
            mf.sharedMesh = mesh;
            int si = 0;
            List<Material> materials = new ();
            if (geomSubsets == null || geomSubsets.Subsets.Count == 0)
            {
                Material mat = null;
                MaterialBindingSample materialBinding = new MaterialBindingSample();
                usdScene.Read(prim.GetPath(), materialBinding);
                string[] materialPaths = materialBinding.binding.targetPaths;
                if (materialPaths != null && materialPaths.Length >= 1)
                {
                    mat = ImportMaterial(new SdfPath(materialPaths[0]));
                }
                materials.Add(mat);
            }
            else
            {
                foreach (var subset in geomSubsets.Subsets)
                {
                    Material mat = null;
                    MaterialBindingSample materialBinding = new MaterialBindingSample();
                    usdScene.Read(subset.Key, materialBinding);
                    string[] materialPaths = materialBinding.binding.targetPaths;
                    if (materialPaths != null && materialPaths.Length >= 1)
                    {
                        mat = ImportMaterial(new SdfPath(materialPaths[0]));
                    }
                    materials.Add(mat);
                }
            }

            mr.sharedMaterials = materials.ToArray();
            
            return mf;
        }
    }
}