using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using pxr;
using USD.NET;
using Unity.Formats.USD;
using UnityEngine.TestTools.Constraints;

namespace Harpoon.USD
{
    public static class USDUtils
    {
        static USDUtils()
        {
            InitUsd.Initialize();
        }

        public struct SceneObject
        {
            private Mesh mesh;
            private Material[] materials;
        }

        public static IEnumerable<Renderer> RendererIterator(GameObject go, LODExportMode lodExportMode = LODExportMode.All)
        {
            LODGroup lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                if (go.GetComponentsInChildren<Renderer>().Length == 0)
                {
                    yield break;
                }

                Transform xform = go.transform;
                for (int i = 0; i < xform.childCount; ++i)
                {
                    var child = xform.GetChild(i);
                    foreach (var renderer in RendererIterator(child.gameObject, lodExportMode))
                    {
                        yield return renderer;
                    }
                }
            }
            else
            {
                LOD[] lods = lodGroup.GetLODs();
                IEnumerable<Renderer> renderers = null;
                switch (lodExportMode)
                {
                    case LODExportMode.All:
                    {
                        renderers = lods.SelectMany(lod => lod.renderers);
                        break;
                    }
                    case LODExportMode.First:
                    {
                        renderers = lods[0].renderers;
                        break;
                    }
                    case LODExportMode.Last:
                    {
                        renderers = lods[lods.Length - 1].renderers;
                        break;
                    }
                }

                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                        yield return renderer;
                }
            }
        }

        private static string m_importDir;

        static string importDir
        {
            get
            {
                if (string.IsNullOrEmpty(m_importDir))
                {
                    m_importDir = Application.dataPath;
                }

                return m_importDir;
            }
            set => m_importDir = value;
        }

        [MenuItem("Harpoon/ImportUSD")]
        public static void ImortUSD()
        {
            string importPath = EditorUtility.OpenFilePanel("Import USD", importDir, "usd,usda,usdz");
            if (string.IsNullOrEmpty(importPath))
                return;
            importDir = Path.GetDirectoryName(importPath);
            ImportUSD(importPath);
        }

        public static GameObject ImportUSD(
            string usdPath, 
            string textureImportDir = "", 
            string materialImportDir = "", 
            string meshImportDir = "", 
            Shader defaultShader = null)
        {
            GameObject go = null;
            using (UsdStage stage = UsdStage.Open(usdPath, UsdStage.InitialLoadSet.LoadAll))
            {
                Scene usdScene = Scene.Open(stage);
                USDImportContext context = new USDImportContext(usdScene);
                context.materialImportDir = materialImportDir;
                context.textureImportDir = textureImportDir;
                context.meshImportDir = meshImportDir;
                context.defaultShader = defaultShader;
                go = context.ImportGameObject(null);
                usdScene.Close();
            }
            return go;
        }

        public static void ExportUSD(USDExportOption options, params string[] primitives)
        {
            GameObject[] gameObjects = primitives
                .Select(prim => GameObject.Find(prim))
                .Where(go => go != null).ToArray();
            ExportUSD(options, gameObjects);
        }

        public static void ExportUSD(USDExportOption options, params GameObject[] gameObjects)
        {
            Scene usdScene = Scene.Create(options.usdFilePath);
            usdScene.UpAxis = Scene.UpAxes.Y;
            USDExportContext context = new USDExportContext(usdScene);
            context.materialBaker = options.materialBaker;
            context.rootXform = options.rootXform;

            usdScene.WriteMode = Scene.WriteModes.Define;
            Vector3 regionMin = options.regionMin;
            Vector3 regionMax = options.regionMax;
            foreach (var go in gameObjects)
            {
                if (go == null)
                    continue;
                foreach (var renderer in RendererIterator(go, options.lodExportMode))
                {
                    if (renderer == null)
                        continue;
                    Vector3 center = renderer.bounds.center;
                    if (center.x > regionMin.x && center.x < regionMax.x
                     && center.y > regionMin.y && center.y < regionMax.y
                     && center.z > regionMin.z && center.z < regionMax.z)
                        context.WriteRenderer(renderer);
                }
            }
            context.FinalizeExport();
        }
    }

    public enum LODExportMode
    {
        All,
        First,
        Last
    }
    
    public struct USDExportOption
    {
        public string usdFilePath;
        public Vector3 regionMin;
        public Vector3 regionMax;
        public USDMaterialBaker materialBaker;
        public Transform rootXform;
        public LODExportMode lodExportMode;

        public USDExportOption(string usdFilePath)
        {
            this.usdFilePath = usdFilePath;
            regionMin = Vector3.negativeInfinity;
            regionMax = Vector3.positiveInfinity;
            materialBaker = null;
            rootXform = null;
            lodExportMode = LODExportMode.All;
        }
    }
}
